// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

using MongoDB.Driver.GridFS;
using Squidex.Assets;
using Squidex.Assets.Mongo;

namespace Microsoft.Extensions.DependencyInjection;

public static class AssetsServiceExtensions
{
    public static IServiceCollection AddMongoAssetStore(this IServiceCollection services, Func<IServiceProvider, IGridFSBucket<string>> bucketProvider)
    {
        services.AddSingletonAs(c => new MongoGridFsAssetStore(bucketProvider(c)))
            .As<IAssetStore>().AsSelf();

        return services;
    }

    public static IServiceCollection AddMongoAssetKeyValueStore(this IServiceCollection services)
    {
        services.AddSingleton(typeof(IAssetKeyValueStore<>), typeof(MongoAssetKeyValueStore<>));

        return services;
    }
}
