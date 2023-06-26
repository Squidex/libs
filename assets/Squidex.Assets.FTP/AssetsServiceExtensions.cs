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
    public static IServiceCollection AddFTPAssetStore(this IServiceCollection services, IConfiguration config, Action<FTPAssetOptions>? configure = null,
        string configPath = "assetStore:ftp")
    {
        services.ConfigureAndValidate(config, configPath, configure);

        services.AddSingletonAs<FTPAssetStore>()
            .As<IAssetStore>().AsSelf();

        return services;
    }
}
