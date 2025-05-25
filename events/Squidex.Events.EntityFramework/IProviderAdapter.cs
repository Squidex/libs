// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

using Microsoft.EntityFrameworkCore;

namespace Squidex.Events.EntityFramework;

public interface IProviderAdapter
{
    Task InitializeAsync(DbContext dbContext,
        CancellationToken ct);

    Task<long> UpdatePositionAsync(DbContext dbContext, Guid id,
        CancellationToken ct);

    Task<long> UpdatePositionsAsync(DbContext dbContext, Guid[] ids,
        CancellationToken ct);

    bool IsDuplicateException(Exception exception);
}
