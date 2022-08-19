// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

namespace Squidex.Messaging.Implementation.Scheduler
{
    public sealed class InlineScheduler : IScheduler
    {
        public static readonly InlineScheduler Instance = new InlineScheduler();

        private InlineScheduler()
        {
        }

        public Task CompleteAsync()
        {
            return Task.CompletedTask;
        }

        public Task ExecuteAsync<TArgs>(TArgs args, Func<TArgs, CancellationToken, Task> action,
            CancellationToken ct)
        {
            return action(args, ct);
        }
    }
}
