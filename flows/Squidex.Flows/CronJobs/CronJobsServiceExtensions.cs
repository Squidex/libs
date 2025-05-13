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
using Squidex.Flows.CronJobs;
using Squidex.Flows.CronJobs.Internal;

namespace Microsoft.Extensions.DependencyInjection;

public static class CronJobsServiceExtensions
{
    public static CronJobsBuilder AddCronJobsCore(this IServiceCollection services)
    {
        return new CronJobsBuilder(services);
    }

    public static CronJobsBuilder AddCronJobs<TContext>(this IServiceCollection services, IConfiguration config,
        Action<CronJobsOptions>? configure = null, string configPath = "cronJobs")
    {
        services.Configure(config, configPath, configure);

        services.AddSingletonAs<DefaultCronJobManager<TContext>>()
            .As<ICronJobManager<TContext>>();

        services.AddSingletonAs<NodaCronTimezoneProvider>()
            .As<ICronTimezoneProvider>();

        services.TryAddSingleton(
            new JsonSerializerOptions(JsonSerializerOptions.Default)
                .ConfigureForNodaTime(DateTimeZoneProviders.Tzdb));

        return new CronJobsBuilder(services);
    }

    public static CronJobsBuilder AddWorker<TContext>(this CronJobsBuilder builder)
    {
        builder.Services.AddSingletonAs<CronJobWorker<TContext>>()
            .AsSelf();

        return builder;
    }
}
