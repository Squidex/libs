// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Squidex.Hosting.Configuration;
using Squidex.Hosting.Ssrf;

namespace Microsoft.Extensions.DependencyInjection;

public static class SsrfServiceExtensions
{
    public static IServiceCollection AddSsrfProtectedHttpClient(this IServiceCollection services, IConfiguration config, string path = "ssrf")
    {
        return AddSsrfProtectedHttpClient(services, SsrfClient.Name, config, path);
    }

    public static IServiceCollection AddSsrfProtectedHttpClient(this IServiceCollection services, string name, IConfiguration config, string path = "ssrf")
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        ArgumentNullException.ThrowIfNull(config);

        services.AddHttpClient(name)
            .EnableSsrfProtection();

        services.Configure<SsrfOptions>(config, path);
        return services;
    }
}
