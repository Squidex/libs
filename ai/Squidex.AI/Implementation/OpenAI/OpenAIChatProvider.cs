// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

using System.Globalization;
using System.Reactive.Linq;
using Microsoft.Extensions.Options;
using OpenAI.Managers;
using OpenAI.ObjectModels.RequestModels;
using OpenAIMessage = OpenAI.ObjectModels.RequestModels.ChatMessage;

namespace Squidex.AI.Implementation.OpenAI;

public sealed class OpenAIChatProvider : IChatProvider
{
    private readonly StreamOptions streamOptions = new StreamOptions { IncludeUsage = true };
    private readonly OpenAIOptions options;
    private readonly OpenAIService service;

    public OpenAIChatProvider(IOptions<OpenAIOptions> options)
    {
        service = new OpenAIService(options.Value);

        this.options = options.Value;
    }

    public IAsyncEnumerable<InternalChatEvent> StreamAsync(ChatProviderRequest request,
        CancellationToken ct = default)
    {
        var internalRequest = new ChatCompletionCreateRequest
        {
            MaxTokens = options.MaxTokens,
            Messages = ConvertHistory(request),
            Model = options.Model,
            N = 1,
            Seed = options.Seed,
            Stream = true,
            StreamOptions = streamOptions,
            Temperature = options.Temperature,
        };

        var index = 0;
        foreach (var tool in request.Tools)
        {
            var toolName = index.ToString(CultureInfo.InvariantCulture);

            internalRequest.Tools ??= [];
            internalRequest.Tools.Add(tool.Spec.ToOpenAITool(toolName));
            index++;
        }

        if (internalRequest.Tools?.Count > 0)
        {
            internalRequest.ToolChoice = ConvertTool(request);
        }

        var stream = RequestCoreAsync(request, internalRequest, ct);

        return stream.ToAsyncEnumerable();
    }

    private IObservable<InternalChatEvent> RequestCoreAsync(ChatProviderRequest request, ChatCompletionCreateRequest internalRequest,
        CancellationToken ct)
    {
        return Observable.Create<InternalChatEvent>(async observer =>
        {
            await RequestCoreAsync(request, internalRequest, observer, ct);
        });
    }

    private async Task RequestCoreAsync(ChatProviderRequest request, ChatCompletionCreateRequest internalRequest, IObserver<InternalChatEvent> observer,
        CancellationToken ct)
    {
        var numInputTokens = 0;
        var numOutputTokens = 0;

        void EmitMetadata()
        {
            observer.OnNext(new ChatFinishEvent
            {
                NumInputTokens = numInputTokens,
                NumOutputTokens = numOutputTokens
            });
        }

        var maxIterations = options.MaxIterations;

        try
        {
            for (var run = 1; run <= maxIterations; run++)
            {
                var stream = service.ChatCompletion.CreateCompletionAsStream(internalRequest, cancellationToken: ct);

                var isToolCall = false;
                await foreach (var response in stream.WithCancellation(ct))
                {
                    if (response.Error != null)
                    {
                        throw new ChatException($"Request failed with internal error: {response.Error.Message}. HTTP {response.HttpStatusCode}");
                    }

                    if (!response.Successful)
                    {
                        throw new ChatException($"Request failed with unknown error. HTTP {response.HttpStatusCode}");
                    }

                    if (response.Usage != null)
                    {
                        numInputTokens += response.Usage.PromptTokens;
                        numOutputTokens += response.Usage.CompletionTokens ?? 0;
                    }

                    var choice = response.Choices.FirstOrDefault()?.Message;

                    if (choice == null)
                    {
                        continue;
                    }

                    if (choice.ToolCalls is not { Count: > 0 })
                    {
                        if (!string.IsNullOrEmpty(choice.Content))
                        {
                            observer.OnNext(new ChunkEvent { Content = choice.Content });
                        }
                    }
                    else if (run == maxIterations)
                    {
                        // This should actually never happen.
                        throw new ChatException($"Exceeded max tool runs.");
                    }
                    else
                    {
                        // Only continue with the outer loop if we have a tool call.
                        isToolCall = true;

                        var toolsResults = await ExecuteToolsAsync(request, observer, choice, ct);

                        internalRequest.Messages.Add(choice);
                        internalRequest.Messages.AddRange(toolsResults);

                        if (run == maxIterations - 1)
                        {
                            // Prevent the tool call for the last result, this should actually never happen.
                            internalRequest.ToolChoice = ToolChoice.None;
                        }
                    }
                }

                if (!isToolCall)
                {
                    break;
                }
            }

            EmitMetadata();
            observer.OnCompleted();
        }
        catch (Exception ex)
        {
            EmitMetadata();
            observer.OnError(ex);
        }
    }

    private static async Task<OpenAIMessage[]> ExecuteToolsAsync(ChatProviderRequest request, IObserver<InternalChatEvent> observer, OpenAIMessage choice, CancellationToken ct)
    {
        var validCalls = new List<(IChatTool Tool, int Index, string Id, FunctionCall Call)>();

        var i = 0;
        foreach (var call in choice.ToolCalls!)
        {
            var toolName = call.FunctionCall?.Name;

            if (string.IsNullOrWhiteSpace(call.FunctionCall?.Name))
            {
                throw new ChatException($"Undefined tool name '{toolName}'.");
            }

            if (!int.TryParse(toolName, CultureInfo.InvariantCulture, out var toolIndex))
            {
                throw new ChatException($"Invalid tool name '{toolName}'.");
            }

            var tool = request.Tools.ElementAtOrDefault(toolIndex)
                ?? throw new ChatException($"Unknown tool name '{toolName}'.");

            validCalls.Add((tool, i++, call.Id!, call.FunctionCall));
        }

        var results = new OpenAIMessage[validCalls.Count];

        // Run all the tools in parallel, because they could take long time potentially.
        await Parallel.ForEachAsync(validCalls, ct, async (job, ct) =>
        {
            observer.OnNext(new ToolStartEvent { Tool = job.Tool });
            try
            {
                var args = job.Call.ParseArguments(job.Tool.Spec);

                var toolContext = new ToolContext
                {
                    Arguments = args,
                    ChatAgent = request.ChatAgent,
                    Context = request.Context,
                    ToolData = request.ToolData
                };

                var result = await job.Tool.ExecuteAsync(toolContext, ct);

                results[job.Index] = OpenAIMessage.FromTool(result, job.Id);
            }
            finally
            {
                observer.OnNext(new ToolEndEvent { Tool = job.Tool });
            }
        });

        return results;
    }

    private static ToolChoice ConvertTool(ChatProviderRequest request)
    {
        return request.Tool != null ?
            ToolChoice.FunctionChoice(request.Tool) :
            ToolChoice.Auto;
    }

    private static List<OpenAIMessage> ConvertHistory(ChatProviderRequest request)
    {
        return request.History.Select(x => x.ToOpenAIMessage()).ToList();
    }
}
