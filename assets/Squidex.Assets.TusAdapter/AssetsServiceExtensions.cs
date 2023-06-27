// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

using Squidex.Assets;
using Squidex.Assets.Internal;
using tusdotnet.Interfaces;

namespace Microsoft.Extensions.DependencyInjection;

public static class AssetsServiceExtensions
{
    public static IServiceCollection AddAssetTus(this IServiceCollection services)
    {
        services.AddSingletonAs<AssetTusStore>()
            .As<ITusExpirationStore>()
            .As<ITusCreationDeferLengthStore>()
            .As<ITusCreationStore>()
            .As<ITusReadableStore>()
            .As<ITusTerminationStore>()
            .As<ITusStore>();

        services.AddSingletonAs<AssetFileLockProvider>()
            .As<ITusFileLockProvider>();

        services.AddSingletonAs<AssetTusRunner>()
            .AsSelf();

        return services;
    }
}
