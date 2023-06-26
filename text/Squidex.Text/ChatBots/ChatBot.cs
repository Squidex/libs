// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

using Microsoft.Extensions.Logging;

namespace Squidex.Text.ChatBots;

public sealed class ChatBot : IChatBot
{
    private readonly IEnumerable<IChatBotService> services;
    private readonly ILogger<ChatBot> log;

    public ChatBot(IEnumerable<IChatBotService> services, ILogger<ChatBot> log)
    {
        this.services = services;

        this.log = log;
    }

    public async Task<IReadOnlyList<string>> AskQuestionAsync(string prompt,
        CancellationToken ct = default)
    {
        var results = await Task.WhenAll(services.Select(x => AskQuestionSafeAsync(x, prompt, ct)));

        return results.SelectMany(x => x).Distinct().ToList();
    }

    private async Task<IReadOnlyList<string>> AskQuestionSafeAsync(IChatBotService service, string prompt,
        CancellationToken ct)
    {
        try
        {
            return await service.AskQuestionAsync(prompt, ct);
        }
        catch (Exception ex)
        {
            log.LogWarning(ex, "Failed to call chatbot service {serivce}.", service);

            return new List<string>();
        }
    }
}
