// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using PhenX.EntityFrameworkCore.BulkInsert.MySql;
using Squidex.Events.EntityFramework;
using TestHelpers;
using TestHelpers.EntityFramework;

#pragma warning disable MA0048 // File name must match type name

namespace Squidex.Events;

public sealed class MariaDbContext(DbContextOptions options) : TestDbContext(options)
{
    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        optionsBuilder.UseBulkInsertMySql();
    }
}

public sealed class MariaDbEventStoreFixture() : MariaDbFixture<MariaDbContext>("eventstore-mariadb")
{
    protected override void AddServices(IServiceCollection services)
    {
        services.AddSingleton<IDbEventStoreBulkInserter, BulkInserter>();
        services.AddEntityFrameworkEventStore<MariaDbContext>(TestUtils.Configuration, options =>
        {
            options.PollingInterval = TimeSpan.FromSeconds(0.1);
        })
        .AddMysqlAdapter();
    }
}

public class MariaDbEventStoreTests(MariaDbEventStoreFixture fixture)
    : EFEventStoreTests<MariaDbContext>, IClassFixture<MariaDbEventStoreFixture>
{
    public override IServiceProvider Services => fixture.Services;

    protected override Task<IEventStore> CreateSutAsync()
    {
        var store = fixture.Services.GetRequiredService<IEventStore>();
        return Task.FromResult(store);
    }
}
