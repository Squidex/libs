// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

namespace Squidex.Text.ChatBots;

public sealed class ChatBotAnswer
{
    required public IReadOnlyList<string> Alternatives { get; init; }

    public decimal EstimatedCostsInEUR { get; init; }
}
