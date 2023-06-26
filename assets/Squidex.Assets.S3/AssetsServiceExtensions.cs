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
    public static IServiceCollection AddAmazonS3AssetStore(this IServiceCollection services, IConfiguration config, Action<AmazonS3AssetOptions>? configure = null,
        string configPath = "assetStore:amazonS3")
    {
        services.ConfigureAndValidate(config, configPath, configure);

        services.AddSingletonAs<AmazonS3AssetStore>()
            .As<IAssetStore>().AsSelf();

        return services;
    }
}
