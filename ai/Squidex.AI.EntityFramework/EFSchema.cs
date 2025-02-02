// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

using Squidex.AI.Mongo;

namespace Microsoft.EntityFrameworkCore;

public static class EFSchema
{
    public static ModelBuilder UseChatStore(this ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<EFChatEntity>(b =>
        {
            b.ToTable("Chats");

            b.HasIndex(x => x.LastUpdated);
        });

        return modelBuilder;
    }
}
