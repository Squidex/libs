// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

using System.Text.Json;
using Microsoft.Extensions.Options;
using Pinecone;
using Pinecone.Grpc;
using Squidex.Hosting;

namespace Squidex.AI.Implementation.Pinecone;

public sealed class PineconeTool : IChatTool, IInitializable
{
    private readonly JsonSerializerOptions serializerOptions = new JsonSerializerOptions
    {
        WriteIndented = true
    };

    private readonly PineconeClient client;
    private readonly PineconeOptions options;
    private readonly IEmbeddings embeddings;
    private Index<GrpcTransport> index;

    public ToolSpec Spec { get; }

    public PineconeTool(IEmbeddings embeddings, IOptions<PineconeOptions> options)
    {
        client = new PineconeClient(options.Value.ApiKey);

        this.embeddings = embeddings;
        this.options = options.Value;

        Spec = new ToolSpec(options.Value.ToolName, options.Value.ToolName, options.Value.ToolDescription)
        {
            Arguments =
            {
                ["query"] = new ToolStringArgumentSpec("The request query.")
                {
                    IsRequired = true
                }
            }
        };
    }

    public async Task InitializeAsync(
        CancellationToken ct)
    {
        index = await client.GetIndex<GrpcTransport>(options.IndexName, ct);
    }

    public async Task<string> ExecuteAsync(ToolContext toolContext,
        CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(toolContext);

        if (index == null)
        {
            throw new InvalidOperationException("Not initialized yet.");
        }

        if (!toolContext.Arguments.TryGetValue("query", out var queryArg))
        {
            throw new ChatException("Missing argument 'query'.");
        }

        var query = queryArg.ToString();

        var embeddingsMemory = await embeddings.CalculateEmbeddingsAsync(query, ct);
        var embeddingsArray = embeddingsMemory.ToArray().Select(x => (float)x).ToArray();

        var result = await index.Query(embeddingsArray, (uint)options.TopK, includeMetadata: true, ct: ct);

        var parsed = new List<object>();

        foreach (var item in result)
        {
            if (item.Metadata != null && item.Metadata.TryGetValue("text", out var metadata))
            {
                var text = metadata.Inner?.ToString();

                if (!string.IsNullOrWhiteSpace(text))
                {
                    parsed.Add(new { text });
                }
            }
        }

        return JsonSerializer.Serialize(parsed, serializerOptions);
    }
}
