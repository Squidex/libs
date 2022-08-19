// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

using System.Threading.Tasks.Dataflow;

namespace Squidex.Messaging.Implementation.Scheduler
{
    public sealed class ParallelScheduler : IScheduler
    {
        private readonly ActionBlock<Func<Task>> actionBlock;

        public ParallelScheduler(int maxDegreeOfParallelism)
        {
            actionBlock = new ActionBlock<Func<Task>>(OnScheduledMessage, new ExecutionDataflowBlockOptions
            {
                MaxMessagesPerTask = 1,
                MaxDegreeOfParallelism = maxDegreeOfParallelism,
                BoundedCapacity = 1
            });
        }

        private async Task OnScheduledMessage(Func<Task> action)
        {
            try
            {
                await action();
            }
            catch
            {
                // We just assume that the exception is handled outside.
                return;
            }
        }

        public Task CompleteAsync()
        {
            actionBlock.Complete();

            return actionBlock.Completion;
        }

        public Task ExecuteAsync<TArgs>(TArgs args, Func<TArgs, CancellationToken, Task> action,
            CancellationToken ct)
        {
            return actionBlock.SendAsync(() => action(args, ct));
        }
    }
}
