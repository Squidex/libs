// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

#pragma warning disable SA1313 // Parameter names should begin with lower-case letter
#pragma warning disable MA0048 // File name must match type name

namespace Squidex.AI;

public sealed record ChatBotResponse(string Text, ChatBotResult Result)
{
    public decimal EstimatedCostsInEUR { get; init; }

    public static ChatBotResponse Success(string text)
    {
        return new ChatBotResponse(text, ChatBotResult.Success);
    }

    public static ChatBotResponse Failed(string text)
    {
        return new ChatBotResponse(text, ChatBotResult.Failed);
    }
}

public enum ChatBotResult
{
    Success,
    Failed,
}
