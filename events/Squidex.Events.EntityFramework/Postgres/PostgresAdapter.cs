// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

using Microsoft.EntityFrameworkCore;

namespace Squidex.Events.EntityFramework.Postgres;

public sealed class PostgresAdapter : IProviderAdapter
{
    public async Task<long> GetPositionAsync(DbContext dbContext,
        CancellationToken ct)
    {
        // Autoincremented positions are not necessarily in the correct order.
        // Therefore we have to create a positions table by ourself and create the next position in the same transaction.
        // Read comments from the following article: https://dev.to/kspeakman/event-storage-in-postgres-4dk2
        var query = dbContext.Database.SqlQuery<long>($"SELECT * FROM NextPosition()");

        return (await query.ToListAsync(ct)).Single();
    }

    public async Task InitializeAsync(DbContext dbContext,
        CancellationToken ct)
    {
        var storedProdecure = Format($@"
CREATE OR REPLACE FUNCTION NextPosition() RETURNS BIGINT AS $$
    DECLARE
        nextPosition BIGINT;
    BEGIN
        UPDATE public.'EventPosition'
        SET 'Position' = 'Position' + 1
        WHERE 'Id' = 1
        RETURNING 'Position' INTO nextPosition;

        RETURN nextPosition;
    END;
$$ LANGUAGE plpgsql;");
        await dbContext.Database.ExecuteSqlRawAsync(storedProdecure, ct);

        var initialPosition = Format($@"
INSERT INTO public.'EventPosition' ('Id', 'Position') 
VALUES (1, 1)
ON CONFLICT DO NOTHING;");
        await dbContext.Database.ExecuteSqlRawAsync(initialPosition, ct);
    }

    public bool IsDuplicateException(Exception exception)
    {
        Exception? ex = exception;

        while (ex != null)
        {
            // Primary Key and Unique Index constraint
            if (ex.Message.Contains("23505", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            ex = ex.InnerException;
        }

        return false;
    }

    private static string Format(string source)
    {
        return source.Replace('\'', '"');
    }
}
