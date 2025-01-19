// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

using Squidex.Assets.EntityFramework;

namespace Microsoft.EntityFrameworkCore;

public static class EFSchema
{
    public static ModelBuilder AddAssetKeyValueStore<T>(this ModelBuilder modelBuilder) where T : class
    {
        modelBuilder.Entity<EFAssetKeyValueEntity<T>>(b =>
        {
            b.ToTable($"AssetKeyValueStore_{typeof(T).Name}");

            b.HasIndex(x => x.Expires);
        });

        return modelBuilder;
    }
}
