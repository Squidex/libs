// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

namespace Squidex.AI;

public sealed class ChatOptions
{
    public ChatConfiguration? Defaults { get; set; }

    public Dictionary<string, ChatConfiguration> Configurations { get; set; } = [];

    public decimal PricePerInputTokenInEUR { get; set; } = 0.003m / 1000;

    public decimal PricePerOutputTokenInEUR { get; set; } = 0.004m / 1000;

    public Dictionary<string, decimal> ToolCostsInEur { get; set; } = [];
}
