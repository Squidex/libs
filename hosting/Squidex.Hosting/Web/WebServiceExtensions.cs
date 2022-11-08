// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Squidex.Hosting;
using Squidex.Hosting.Web;

namespace Microsoft.Extensions.DependencyInjection;

public static class WebServiceExtensions
{
    public static IServiceCollection AddDefaultWebServices(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<UrlOptions>(configuration, "urls");

        services.AddSingleton<IUrlGenerator, UrlGenerator>();

        return services;
    }

    public static IServiceCollection AddDefaultForwardRules(this IServiceCollection services)
    {
        services.AddHostFiltering(options => { })
            .ConfigureOptions<ConfigureForwardedHeaders>();

        services.AddHttpsRedirection(options => { })
            .ConfigureOptions<ConfigureHttpsRedirection>();

        return services;
    }
}
