// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Squidex.Flows.Execution;
using TestHelpers;
using TestHelpers.EntityFramework;

#pragma warning disable MA0048 // File name must match type name

namespace Squidex.Flows;

public sealed class EFFlowStateStoreDbContext(DbContextOptions options) : DbContext(options)
{
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.UseFlows();
        base.OnModelCreating(modelBuilder);
    }
}

public sealed class EFFlowStateStoreFixture() : PostgresFixture<EFFlowStateStoreDbContext>("flows-postgres")
{
    protected override void AddServices(IServiceCollection services)
    {
        services.AddFlows<TestFlowContext>(TestUtils.Configuration)
            .AddEntityFrameworkStore<EFFlowStateStoreDbContext, TestFlowContext>();
    }
}

public class EFFlowStateStoreTests(EFFlowStateStoreFixture fixture) :
    FlowStateStoreTests, IClassFixture<EFFlowStateStoreFixture>
{
    protected override Task<IFlowStateStore<TestFlowContext>> CreateSutAsync()
    {
        var store = fixture.Services.GetRequiredService<IFlowStateStore<TestFlowContext>>();
        return Task.FromResult(store);
    }
}
