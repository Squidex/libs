// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

#pragma warning disable MA0048 // File name must match type name

using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Squidex.Events;

public class TestContext(DbContextOptions options) : DbContext(options)
{
    public DbSet<TestEntity> Tests { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.UseEventStore();
        base.OnModelCreating(modelBuilder);
    }
}

[Index(nameof(UniqueValue), IsUnique = true)]
public class TestEntity
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.None)]
    public long Id { get; set; }

    public long UniqueValue { get; set; }
}
