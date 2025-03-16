// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection.Extensions;
using NodaTime;
using NodaTime.Serialization.SystemTextJson;
using Squidex.Flows;
using Squidex.Flows.Execution;
using Squidex.Flows.Internal;
using Squidex.Flows.Steps;

namespace Microsoft.Extensions.DependencyInjection;

public static class FlowsServiceExtensions
{
    public static FlowsBuilder AddFlows<TContext>(this IServiceCollection services, IConfiguration config,
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

        services.TryAddSingleton(
            new JsonSerializerOptions(JsonSerializerOptions.Default)
                .ConfigureForNodaTime(DateTimeZoneProviders.Tzdb));

        services.Configure<FlowOptions>(options =>
        {
            options.AddStepIfNotExist(typeof(DelayStep));
            options.AddStepIfNotExist(typeof(IfStep));
            options.AddStepIfNotExist(typeof(ScriptStep));
            options.AddStepIfNotExist(typeof(WebhookStep));
        });

        return new FlowsBuilder(services);
    }

    public static FlowsBuilder AddWorker<TContext>(this FlowsBuilder builder)
        where TContext : FlowContext
    {
        builder.Services.AddSingletonAs<FlowExecutionWorker<TContext>>()
            .AsSelf();

        return builder;
    }
}
