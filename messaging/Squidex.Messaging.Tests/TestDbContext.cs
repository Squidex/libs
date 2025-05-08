// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

using Microsoft.EntityFrameworkCore;

#pragma warning disable MA0048 // File name must match type name

namespace Squidex.Messaging;

public sealed class TestDbContext(DbContextOptions options) : DbContext(options)
{
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.UseMessagingDataStore();
        modelBuilder.UseMessagingTransport();
        base.OnModelCreating(modelBuilder);
    }
}
