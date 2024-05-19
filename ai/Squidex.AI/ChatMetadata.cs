// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

namespace Squidex.AI;

public sealed record ChatMetadata
{
    public decimal CostsInEUR { get; init; }

    public int NumInputTokens { get; init; }

    public int NumOutputTokens { get; init; }
}
