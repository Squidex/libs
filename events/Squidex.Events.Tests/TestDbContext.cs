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
using PhenX.EntityFrameworkCore.BulkInsert.Extensions;
using Squidex.Events.EntityFramework;

namespace Squidex.Events;

public class TestDbContext(DbContextOptions options) : DbContext(options)
{
    public DbSet<TestEntity> Tests { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.UseEventStore();
        base.OnModelCreating(modelBuilder);
    }
}

public sealed class BulkInserter : IDbEventStoreBulkInserter
{
    public Task BulkInsertAsync<T>(DbContext dbContext, IEnumerable<T> entities,
        CancellationToken ct = default) where T : class
    {
        return dbContext.ExecuteBulkInsertAsync(entities, null, null, ct);
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
