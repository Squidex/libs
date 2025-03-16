// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Squidex.AI.Implementation;
using TestHelpers.EntityFramework;

#pragma warning disable MA0048 // File name must match type name

namespace Squidex.AI;

public sealed class EFChatStoreDbContext(DbContextOptions options) : DbContext(options)
{
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.UseChatStore();
        base.OnModelCreating(modelBuilder);
    }
}

public class EFChatStoreFixture() : PostgresFixture<EFChatStoreDbContext>("chat-postgres")
{
    protected override void AddServices(IServiceCollection services)
    {
        services.AddAI()
            .AddEntityFrameworkChatStore<EFChatStoreDbContext>();
    }
}

public sealed class EFChatStoreTests(EFChatStoreFixture fixture)
    : ChatStoreTests, IClassFixture<EFChatStoreFixture>
{
    public override Task<IChatStore> CreateSutAsync()
    {
        var store = fixture.Services.GetRequiredService<IChatStore>();
        return Task.FromResult(store);
    }
}
