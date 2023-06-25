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
    public static IServiceCollection AddAzureBlobAssetStore(this IServiceCollection services, IConfiguration config, Action<AzureBlobAssetOptions>? configure = null,
        string configPath = "assetStore:azureBlob")
    {
        services.ConfigureAndValidate(config, configPath, configure);

        services.AddSingletonAs<AzureBlobAssetStore>()
            .As<IAssetStore>().AsSelf();

        return services;
    }
}
