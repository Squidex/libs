// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using PhenX.EntityFrameworkCore.BulkInsert.SqlServer;
using TestHelpers;
using TestHelpers.EntityFramework;

#pragma warning disable MA0048 // File name must match type name

namespace Squidex.Events;

public sealed class SqlServerDbContext(DbContextOptions options) : TestDbContext(options)
{
    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        optionsBuilder.UseBulkInsertSqlServer();
    }
}

public sealed class SqlServerEventStoreFixture() : SqlServerFixture<SqlServerDbContext>("eventstore-sqlserver")
{
    protected override void AddServices(IServiceCollection services)
    {
        services.AddEntityFrameworkEventStore<SqlServerDbContext>(TestUtils.Configuration, options =>
            {
                options.PollingInterval = TimeSpan.FromSeconds(0.1);
            })
            .AddSqlServerAdapter();
    }
}

public class SqlServerEventStoreTests(SqlServerEventStoreFixture fixture)
    : EFEventStoreTests<SqlServerDbContext>, IClassFixture<SqlServerEventStoreFixture>
{
    public override IServiceProvider Services => fixture.Services;

    protected override Task<IEventStore> CreateSutAsync()
    {
        var store = fixture.Services.GetRequiredService<IEventStore>();
        return Task.FromResult(store);
    }
}
