// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

using Squidex.Flows;
using Squidex.Flows.Execution;
using Squidex.Flows.Mongo;

namespace Microsoft.Extensions.DependencyInjection;

public static class FlowServiceExtensions
{
    public static FlowsBuilder AddMongoFlowStore<TContext>(this FlowsBuilder builder) where TContext : FlowContext
    {
        builder.Services.AddSingletonAs<MongoFlowStateStore<TContext>>()
            .As<IFlowStateStore<TContext>>();

        return builder;
    }
}
