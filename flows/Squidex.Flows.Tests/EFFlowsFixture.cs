// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

using Google.Api;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using PhenX.EntityFrameworkCore.BulkInsert.Extensions;
using PhenX.EntityFrameworkCore.BulkInsert.Options;
using PhenX.EntityFrameworkCore.BulkInsert.PostgreSql;
using Squidex.Flows.EntityFramework;
using TestHelpers;
using TestHelpers.EntityFramework;

#pragma warning disable MA0048 // File name must match type name

namespace Squidex.Flows;

public sealed class EFFlowsDbContext(DbContextOptions options) : DbContext(options)
{
    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        optionsBuilder.UseBulkInsertPostgreSql();
        base.OnConfiguring(optionsBuilder);
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.UseFlows();
        modelBuilder.UseCronJobs();
        base.OnModelCreating(modelBuilder);
    }
}

public sealed class EFFlowsFixture() : PostgresFixture<EFFlowsDbContext>("flows-postgres")
{
    protected override void AddServices(IServiceCollection services)
    {
        services.AddFlows<TestFlowContext>(TestUtils.Configuration)
            .AddEntityFrameworkStore<EFFlowsDbContext, TestFlowContext>();

        services.AddCronJobs<TestFlowContext>(TestUtils.Configuration)
            .AddEntityFrameworkStore<EFFlowsDbContext, TestFlowContext>();

        services.AddSingleton<IDbFlowsBulkInserter, BulkInserter>();
    }
}

public sealed class BulkInserter : IDbFlowsBulkInserter
{
    public Task BulkUpsertAsync<T>(DbContext dbContext, IEnumerable<T> entities,
        CancellationToken ct = default) where T : class
    {
        return dbContext.ExecuteBulkInsertAsync(
            entities,
            null,
            new OnConflictOptions<T>
            {
                Update = e => e,
            },
            ct);
    }
}

[CollectionDefinition(Name)]
public class EFFlowsCollection : ICollectionFixture<EFFlowsFixture>
{
    public const string Name = "flows-postgres";
}
