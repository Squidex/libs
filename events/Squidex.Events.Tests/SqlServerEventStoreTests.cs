// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

using Microsoft.Extensions.DependencyInjection;
using TestHelpers;
using TestHelpers.EntityFramework;

#pragma warning disable MA0048 // File name must match type name

namespace Squidex.Events;

public sealed class SqlServerEventStoreFixture() : SqlServerFixture<TestDbContext>("eventstore-sqlserver")
{
    protected override void AddServices(IServiceCollection services)
    {
        services.AddEntityFrameworkEventStore<TestDbContext>(TestUtils.Configuration, options =>
            {
                options.PollingInterval = TimeSpan.FromSeconds(0.1);
            })
            .AddSqlServerAdapter();
    }
}

public class SqlServerEventStoreTests(SqlServerEventStoreFixture fixture)
    : EFEventStoreTests, IClassFixture<SqlServerEventStoreFixture>
{
    public override IServiceProvider Services => fixture.Services;

    protected override Task<IEventStore> CreateSutAsync()
    {
        var store = fixture.Services.GetRequiredService<IEventStore>();
        return Task.FromResult(store);
    }
}
