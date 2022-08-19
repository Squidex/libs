// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Squidex.Hosting;

namespace Squidex.Messaging.Implementation
{
    internal sealed class SubscriptionCleaner : IBackgroundProcess
    {
        private readonly Func<IAsyncDisposable> timerFactory;
        private IAsyncDisposable? timer;

        public SubscriptionCleaner(ISubscriptionStore subscriptions,
            IOptions<MessagingOptions> messagingOptions, IClock clock, ILogger<SubscriptionCleaner> log)
        {
            var interval = messagingOptions.Value.SubscriptionUpdateInterval;

            timerFactory = () =>
                new SimpleTimer(async ct =>
                {
                    // The timer will do the logging anyway, so there is no need to handle exceptions here.
                    await subscriptions.CleanupAsync(clock.UtcNow, ct);
                }, interval, log);
        }

        public Task StartAsync(
            CancellationToken ct)
        {
            timer ??= timerFactory();

            return Task.CompletedTask;
        }

        public async Task StopAsync(
            CancellationToken ct)
        {
            if (timer != null)
            {
                await timer.DisposeAsync();
            }
        }
    }
}
