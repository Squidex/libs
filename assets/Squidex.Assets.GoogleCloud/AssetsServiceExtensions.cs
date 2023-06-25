// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

using Microsoft.Extensions.Configuration;
using Squidex.Assets;

namespace Microsoft.Extensions.DependencyInjection;

public static class AssetsServiceExtensions
{
    public static IServiceCollection AddGoogleCloudAssetStore(this IServiceCollection services, IConfiguration config, Action<GoogleCloudAssetOptions>? configure = null,
        string configPath = "assetStore:googleCloud")
    {
        services.ConfigureAndValidate(config, configPath, configure);

        services.AddSingletonAs<GoogleCloudAssetStore>()
            .As<IAssetStore>().AsSelf();

        return services;
    }
}
