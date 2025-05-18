// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using NpgsqlTypes;

namespace Squidex.Events.EntityFramework.Postgres;

public sealed class PostgresAdapter : IProviderAdapter
{
    public async Task InitializeAsync(DbContext dbContext,
        CancellationToken ct)
    {
        await CreateUpdatePositionAsync(dbContext, ct);
        await CreateUpdatePositionsAsync(dbContext, ct);
        await InitialPositionAsync(dbContext, ct);
    }

    private static async Task CreateUpdatePositionAsync(DbContext dbContext,
        CancellationToken ct)
    {
        var sql = Format($@"
CREATE OR REPLACE FUNCTION UpdatePosition(id UUID) RETURNS BIGINT AS $$
    DECLARE
        newPosition BIGINT;
    BEGIN
        -- Reserve a new position
        UPDATE public.'EventPosition'
        SET 'Position' = 'Position' + 1
        WHERE 'Id' = 1
        RETURNING 'Position' INTO newPosition;

        -- Update Event with calculated position
        UPDATE public.""Events""
        SET ""Position"" = newPosition
        WHERE ""Id"" = id;

        RETURN newPosition;
    END;
$$ LANGUAGE plpgsql;");
        await dbContext.Database.ExecuteSqlRawAsync(sql, ct);
    }

    private static async Task CreateUpdatePositionsAsync(DbContext dbContext,
        CancellationToken ct)
    {
        var sql = Format($@"
CREATE OR REPLACE FUNCTION UpdatePositions(IN ids JSON) RETURNS BIGINT AS $$
    DECLARE
        newPosition BIGINT;
        currentId UUID;
        currentIndex INT DEFAULT 0;
        oldPosition BIGINT;
        total INT;
    BEGIN
        -- Get the number of IDs to process
        total = json_array_length(ids);

        -- Reserve a new positions
        UPDATE public.""EventPosition""
		SET ""Position"" = ""Position"" + 1
		WHERE ""Id"" = 1
		RETURNING ""Position"" INTO newPosition;

        oldPosition = newPosition - total + 1;

        -- Update Events with calculated position
        WHILE currentIndex < total LOOP
            -- Extract ID as string
            currentId = (ids ->> currentIndex)::uuid;

            -- Update Entity with calculated position
            UPDATE public.""Events""
            SET ""Position"" = oldPosition + currentIndex
            WHERE ""Id"" = currentId;

            currentIndex = currentIndex + 1;
        END LOOP;

        RETURN newPosition;
    END;
$$ LANGUAGE plpgsql;");
        await dbContext.Database.ExecuteSqlRawAsync(sql, ct);
    }

    private static async Task InitialPositionAsync(DbContext dbContext,
        CancellationToken ct)
    {
        var sql = Format($@"
INSERT INTO public.'EventPosition' ('Id', 'Position') 
VALUES (1, 1)
ON CONFLICT DO NOTHING;");
        await dbContext.Database.ExecuteSqlRawAsync(sql, ct);
    }

    public async Task<long> UpdatePositionAsync(DbContext dbContext, Guid id,
        CancellationToken ct)
    {
        var parameter = new NpgsqlParameter("id", NpgsqlDbType.Uuid)
        {
            Value = id,
        };

        // Autoincremented positions are not necessarily in the correct order.
        // Therefore we have to create a positions table by ourself and create the next position in the same transaction.
        // Read comments from the following article: https://dev.to/kspeakman/event-storage-in-postgres-4dk2
        var query = dbContext.Database.SqlQueryRaw<long>("SELECT * FROM UpdatePosition(@id)", parameter);

        return (await query.ToListAsync(ct)).Single();
    }

    public async Task<long> UpdatePositionsAsync(DbContext dbContext, Guid[] ids,
        CancellationToken ct)
    {
        var parameter = new NpgsqlParameter("ids", NpgsqlDbType.Json)
        {
            Value = JsonSerializer.Serialize(ids),
        };

        // Autoincremented positions are not necessarily in the correct order.
        // Therefore we have to create a positions table by ourself and create the next position in the same transaction.
        // Read comments from the following article: https://dev.to/kspeakman/event-storage-in-postgres-4dk2
        var query = dbContext.Database.SqlQueryRaw<long>("SELECT * FROM UpdatePositions(@ids)", parameter);

        return (await query.ToListAsync(ct)).Single();
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
