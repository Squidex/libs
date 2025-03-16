// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage;
using Squidex.Hosting;

namespace TestHelpers.EntityFramework;

public sealed class DbContextInitializer<TContext>(IDbContextFactory<TContext> dbContextFactory)
    : IInitializable where TContext : DbContext
{
    public int Order => int.MinValue;

    public async Task InitializeAsync(
        CancellationToken ct)
    {
        await using var context = await dbContextFactory.CreateDbContextAsync(ct);

        var creator = (RelationalDatabaseCreator)context.Database.GetService<IDatabaseCreator>();

        await creator.EnsureCreatedAsync(ct);
    }
}
