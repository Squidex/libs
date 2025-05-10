// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using TestHelpers;
using TestHelpers.EntityFramework;

#pragma warning disable MA0048 // File name must match type name

namespace Squidex.Flows;

public sealed class EFFlowsDbContext(DbContextOptions options) : DbContext(options)
{
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
    }
}
