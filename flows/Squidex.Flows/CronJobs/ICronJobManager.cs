// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

namespace Squidex.Flows.CronJobs;

public interface ICronJobManager<TContext>
{
    void Subscribe(Func<CronJob<TContext>, CancellationToken, Task> handler);

    Task AddAsync(CronJob<TContext> cronJob,
        CancellationToken ct = default);

    Task RemoveAsync(string id,
        CancellationToken ct = default);
}
