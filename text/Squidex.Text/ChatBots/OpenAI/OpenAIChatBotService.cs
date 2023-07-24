// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

using Microsoft.Extensions.Options;
using OpenAI;
using OpenAI.Managers;
using OpenAI.ObjectModels.RequestModels;

namespace Squidex.Text.ChatBots.OpenAI;

public sealed class OpenAIChatBotService : IChatBotService
{
    private readonly OpenAIChatBotOptions options;
    private OpenAIService? service;

    public OpenAIChatBotService(IOptions<OpenAIChatBotOptions> options)
    {
        this.options = options.Value;
    }

    public async Task<ChatBotResult> AskQuestionAsync(string prompt,
        CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(options.ApiKey))
        {
            return new ChatBotResult
            {
                Choices = new List<string>()
            };
        }

        service ??= new OpenAIService(new OpenAiOptions
        {
            ApiKey = options.ApiKey
        });

        var response = await service.ChatCompletion.CreateCompletion(new ChatCompletionCreateRequest
        {
            Model = options.Model,
            Messages = new List<ChatMessage>
            {
                ChatMessage.FromSystem(prompt)
            },
            MaxTokens = options.MaxTokens
        }, cancellationToken: ct);

        if (response.Error != null)
        {
            throw new InvalidOperationException(response.Error.Message);
        }

        var numTokensInput = response.Usage?.PromptTokens ?? 0;
        var numTokensOutput = response.Usage?.CompletionTokens ?? 0;

        return new ChatBotResult
        {
            Choices = response.Choices.Select(x => x.Message.Content).ToList(),
            EstimatedCostsInEUR =
                (numTokensInput * options.PricePerInputTokenInEUR) +
                (numTokensOutput * options.PricePerOutputTokenInEUR)
        };
    }
}
