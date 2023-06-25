﻿// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

namespace Squidex.Text.ChatBots;

public interface IChatBot
{
    Task<IReadOnlyList<string>> AskQuestionAsync(string prompt,
        CancellationToken ct = default);
}