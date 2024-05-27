// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

namespace Squidex.AI.Implementation.Pinecone;

public sealed class PineconeOptions
{
    public string ToolName { get; set; } = "pinecone";

    public string ToolDescription { get; set; } = "Provides documents from pinecone.";

    public string ApiKey { get; set; }

    public string IndexName { get; set; }

    public int TopK { get; set; } = 3;

    public float MinimumDistance { get; set; } = 0.5f;
}
