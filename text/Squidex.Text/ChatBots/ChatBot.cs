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

    public async Task<ChatBotResult> AskQuestionAsync(string prompt,
        CancellationToken ct = default)
    {
        var results = await Task.WhenAll(services.Select(x => AskQuestionSafeAsync(x, prompt, ct)));

        return new ChatBotResult
        {
            Choices = results.SelectMany(x => x.Choices).Distinct().ToList(),
            // We just sum up all costs.
            EstimatedCostsInEUR = results.Sum(x => x.EstimatedCostsInEUR)
        };
    }

    private async Task<ChatBotResult> AskQuestionSafeAsync(IChatBotService service, string prompt,
        CancellationToken ct)
    {
        try
        {
            return await service.AskQuestionAsync(prompt, ct);
        }
        catch (Exception ex)
        {
            log.LogWarning(ex, "Failed to call chatbot service {serivce}.", service);

            return new ChatBotResult
            {
                Choices = new List<string>()
            };
        }
    }
}
