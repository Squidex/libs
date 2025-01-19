// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Squidex.Assets;
using Squidex.Assets.EntityFramework;

namespace Microsoft.Extensions.DependencyInjection;

public static class AssetsServiceExtensions
{
    public static IServiceCollection AddEntityFrameworkAssetKeyValueStore<TContext, TEntity>(this IServiceCollection services)
        where TContext : DbContext
        where TEntity : class
    {
        services.AddSingletonAs<EFAssetKeyValueStore<TContext, TEntity>>()
            .As<IAssetKeyValueStore<TEntity>>().AsSelf();

        services.AddDbContextFactory<TContext>();
        services.TryAddSingleton(JsonSerializerOptions.Default);

        return services;
    }
}
