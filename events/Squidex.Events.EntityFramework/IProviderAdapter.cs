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

    Task<long> GetPositionAsync(DbContext dbContext,
        CancellationToken ct);

    bool IsDuplicateException(Exception exception);
}
