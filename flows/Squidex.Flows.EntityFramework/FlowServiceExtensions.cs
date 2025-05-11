// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

using Microsoft.EntityFrameworkCore;
using Squidex.Flows;
using Squidex.Flows.CronJobs;
using Squidex.Flows.CronJobs.Internal;
using Squidex.Flows.EntityFramework;
using Squidex.Flows.Internal.Execution;

namespace Microsoft.Extensions.DependencyInjection;

public static class FlowServiceExtensions
{
    public static CronJobsBuilder AddEntityFrameworkStore<TDbContext, TContext>(this CronJobsBuilder builder)
        where TDbContext : DbContext
    {
        builder.Services.AddSingletonAs<EFCronJobStore<TDbContext, TContext>>()
            .As<ICronJobStore<TContext>>();

        return builder;
    }

    public static FlowsBuilder AddEntityFrameworkStore<TDbContext, TContext>(this FlowsBuilder builder)
        where TContext : FlowContext
        where TDbContext : DbContext
    {
        builder.Services.AddSingletonAs<EFFlowStateStore<TDbContext, TContext>>()
            .As<IFlowStateStore<TContext>>();

        return builder;
    }
}
