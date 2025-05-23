// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

using Squidex.Flows;
using Squidex.Flows.CronJobs;
using Squidex.Flows.CronJobs.Internal;
using Squidex.Flows.Internal.Execution;
using Squidex.Flows.Mongo;

namespace Microsoft.Extensions.DependencyInjection;

public static class FlowServiceExtensions
{
    public static CronJobsBuilder AddMongoStore<TContext>(this CronJobsBuilder builder)
    {
        builder.Services.AddSingletonAs<MongoCronJobStore<TContext>>()
            .As<ICronJobStore<TContext>>();

        return builder;
    }

    public static FlowsBuilder AddMongoStore<TContext>(this FlowsBuilder builder) where TContext : FlowContext
    {
        builder.Services.AddSingletonAs<MongoFlowStateStore<TContext>>()
            .As<IFlowStateStore<TContext>>();

        return builder;
    }
}
