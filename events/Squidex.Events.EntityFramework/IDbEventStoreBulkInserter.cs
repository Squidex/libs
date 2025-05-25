// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

using Microsoft.EntityFrameworkCore;

#pragma warning disable MA0048 // File name must match type name

namespace Squidex.Events.EntityFramework;

public interface IDbEventStoreBulkInserter
{
    Task BulkInsertAsync<T>(DbContext dbContext, IEnumerable<T> entities,
        CancellationToken ct = default) where T : class;
}

public sealed class NullDbEventStoreBulkInserter : IDbEventStoreBulkInserter
{
    public Task BulkInsertAsync<T>(DbContext dbContext, IEnumerable<T> entities,
        CancellationToken ct = default) where T : class
    {
        throw new NotSupportedException("Register a bulk inserter.");
    }
}
