// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

using Microsoft.Extensions.Configuration;
using Squidex.Flows;
using Squidex.Flows.Execution;
using Squidex.Flows.Internal;
using Squidex.Flows.Steps;

namespace Microsoft.Extensions.DependencyInjection;

public static class FlowsServiceExtensions
{
    public static IServiceCollection AddFlows<TContext>(this IServiceCollection services, IConfiguration config,
        Action<FlowOptions>? configure = null, string configPath = "flows")
        where TContext : FlowContext
    {
        services.Configure(config, configPath, configure);

        services.AddSingletonAs<DefaultFlowExecutor<TContext>>()
            .As<IFlowExecutor<TContext>>();

        services.AddSingletonAs<DefaultRetryErrorPolicy<TContext>>()
            .As<IErrorPolicy<TContext>>();

        services.AddSingletonAs<FlowConsoleMiddleware>()
            .As<IFlowMiddleware>();

        services.AddSingletonAs<FlowStepRegistry>()
            .As<IFlowStepRegistry>();

        services.Configure<FlowOptions>(options =>
        {
            options.Steps.Add(typeof(DelayStep));
            options.Steps.Add(typeof(IfStep));
            options.Steps.Add(typeof(ScriptStep));
            options.Steps.Add(typeof(WebhookStep));
        });

        return services;
    }

    public static IServiceCollection AddFlowWorker<TContext>(this IServiceCollection services)
        where TContext : FlowContext
    {
        services.AddSingletonAs<FlowExecutionWorker<TContext>>()
            .AsSelf();

        return services;
    }
}
