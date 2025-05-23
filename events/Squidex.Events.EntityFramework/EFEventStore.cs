// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Squidex.Hosting;

namespace Squidex.Events.EntityFramework;

public sealed partial class EFEventStore<T>(
    IDbContextFactory<T> dbContextFactory,
    IProviderAdapter adapter,
    TimeProvider timeProvider,
    IOptions<EFEventStoreOptions> options)
    : IEventStore, IInitializable where T : DbContext
{
    public async Task InitializeAsync(
        CancellationToken ct)
    {
        await using var dbContext = await dbContextFactory.CreateDbContextAsync(ct);
        await adapter.InitializeAsync(dbContext, ct);
    }
}
