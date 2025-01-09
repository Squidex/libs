// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

using Microsoft.EntityFrameworkCore;
using Squidex.AI.Implementation;
using Squidex.AI.Mongo;

namespace Microsoft.Extensions.DependencyInjection;

public static class EFChatServiceExtensions
{
    public static IServiceCollection AddEntityFrameworkChatStore<T>(this IServiceCollection services)
        where T : DbContext
    {
        services.AddSingletonAs<EFChatStore<T>>()
            .As<IChatStore>();

        services.AddDbContextFactory<T>();

        return services;
    }

    public static ModelBuilder AddChatStore(this ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<EFChatEntity>(b =>
        {
            b.HasIndex(x => x.LastUpdated);
        });

        return modelBuilder;
    }
}
