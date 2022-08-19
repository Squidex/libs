// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

namespace Squidex.Messaging.Implementation.Scheduler
{
    public interface IScheduler
    {
        Task CompleteAsync();

        Task ExecuteAsync<TArgs>(TArgs args, Func<TArgs, CancellationToken, Task> action,
            CancellationToken ct);
    }
}
