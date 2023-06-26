// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using Squidex.Hosting;
using Squidex.Hosting.Configuration;

namespace Microsoft.Extensions.DependencyInjection;

public static class ConfigurationServiceExtensions
{
    public static IServiceCollection AddOptionValidation(this IServiceCollection services)
    {
        services.AddSingleton<IInitializable, ValidationInitializer>();

        return services;
    }

    public static IServiceCollection Configure<T>(this IServiceCollection services, Action<IServiceProvider, T> configure) where T : class
    {
        services.AddSingleton<IConfigureOptions<T>>(c => new ConfigureOptions<T>(o => configure(c, o)));

        return services;
    }

    public static IServiceCollection Configure<T>(this IServiceCollection services, string name, Action<IServiceProvider, T> configure) where T : class
    {
        services.AddSingleton<IConfigureNamedOptions<T>>(c => new ConfigureNamedOptions<T>(name, o => configure(c, o)));

        return services;
    }

    public static IServiceCollection ConfigureOptional<T>(this IServiceCollection services, Action<T>? configure = null) where T : class
    {
        services.Configure(configure ?? (_ => { }));

        return services;
    }

    public static IServiceCollection Configure<T>(this IServiceCollection services, IConfiguration config, string path, Action<T>? configure = null) where T : class
    {
        services.AddOptions<T>().Bind(config.GetSection(path));
        services.ConfigureOptional(configure);

        return services;
    }

    public static IServiceCollection ConfigureAndValidate<T>(this IServiceCollection services, IConfiguration config, string path, Action<T>? configure = null) where T : class, IValidatableOptions
    {
        services.AddOptions<T>().Bind(config.GetSection(path));
        services.AddSingleton<IErrorProvider>(c => ActivatorUtilities.CreateInstance<OptionsErrorProvider<T>>(c, path));
        services.ConfigureOptional(configure);

        return services;
    }
}
