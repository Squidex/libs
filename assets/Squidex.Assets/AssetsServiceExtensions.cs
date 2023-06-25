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
    public static IServiceCollection AddFolderAssetStore(this IServiceCollection services, IConfiguration config, Action<FolderAssetOptions>? configure = null,
        string configPath = "assetStore:folder")
    {
        services.ConfigureAndValidate(config, configPath, configure);

        services.AddSingletonAs<FolderAssetStore>()
            .As<IAssetStore>().AsSelf();

        return services;
    }
}
