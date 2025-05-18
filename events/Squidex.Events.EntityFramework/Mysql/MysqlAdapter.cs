// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

using Microsoft.EntityFrameworkCore;

namespace Squidex.Events.EntityFramework.Mysql;

public sealed class MysqlAdapter : IProviderAdapter
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
        try
        {
            var sql = $@"
CREATE FUNCTION UpdatePositionV13(eventId CHAR(36)) RETURNS BIGINT
READS SQL DATA
DETERMINISTIC
BEGIN
    DECLARE newPosition BIGINT;

    -- Update and fetch new position
    UPDATE EventPosition
    SET Position = Position + 1
    WHERE Id = 1;

    SELECT Position INTO newPosition
    FROM EventPosition
    WHERE Id = 1;

    UPDATE Events
    SET Position = newPosition
    WHERE Id = eventId;

    RETURN newPosition;
END;";
            await dbContext.Database.ExecuteSqlRawAsync(sql, ct);
        }
        catch (Exception ex) when (ex.Message.Contains("already exists", StringComparison.OrdinalIgnoreCase))
        {
            // MySQL has not simple check function.
        }
    }

    private static async Task CreateUpdatePositionsAsync(DbContext dbContext,
        CancellationToken ct)
    {
        try
        {
            var sql = $@"
CREATE FUNCTION UpdatePositions8(eventIds CHAR(36)) RETURNS BIGINT
READS SQL DATA
DETERMINISTIC
BEGIN
    DECLARE eventIdsOffset INT DEFAULT 1;
    DECLARE currentId CHAR(36);
    DECLARE currentPosition BIGINT;
    DECLARE total INT;

    SELECT Position INTO currentPosition
    FROM EventPosition
    WHERE Id = 1
    FOR UPDATE;

    WHILE LENGTH(eventIds) > 0 DO
        SET eventIdsOffset = LOCATE(',', eventIds);

        IF eventIdsOffset > 0 THEN
            SET currentId = TRIM(SUBSTRING(eventIds, 1, eventIdsOffset - 1));
            SET eventIds = SUBSTRING(eventIds, eventIdsOffset + 1);
        ELSE
            SET currentId = TRIM(eventIds);
            SET eventIds = ''; -- Done
        END IF;

        SET currentPosition = currentPosition + 1;

        UPDATE Events
        SET Position = currentPosition
        WHERE Id = currentId;
    END WHILE;

    -- Update new position
    UPDATE EventPosition
    SET Position = currentPosition
    WHERE Id = 1;

    RETURN currentPosition;
END;";
            await dbContext.Database.ExecuteSqlRawAsync(sql, ct);
        }
        catch (Exception ex) when (ex.Message.Contains("already exists", StringComparison.OrdinalIgnoreCase))
        {
            // MySQL has not simple check function.
        }
    }

    private static async Task InitialPositionAsync(DbContext dbContext,
        CancellationToken ct)
    {
        var sql = $@"
INSERT INTO EventPosition (Id, Position) 
VALUES (1, 1) 
ON DUPLICATE KEY UPDATE Id = Id;";
        await dbContext.Database.ExecuteSqlRawAsync(sql, ct);
    }

    public async Task<long> UpdatePositionAsync(DbContext dbContext, Guid id,
        CancellationToken ct)
    {
        var parameter = id.ToString();

        // Autoincremented positions are not necessarily in the correct order.
        // Therefore we have to create a positions table by ourself and create the next position in the same transaction.
        // Read comments from the following article: https://dev.to/kspeakman/event-storage-in-postgres-4dk2
        var query = dbContext.Database.SqlQuery<long>($"SELECT UpdatePositionV13({parameter})");

        return (await query.ToListAsync(ct)).Single();
    }

    public async Task<long> UpdatePositionsAsync(DbContext dbContext, Guid[] ids,
        CancellationToken ct)
    {
        var parameter = string.Join(',', ids);

        // Autoincremented positions are not necessarily in the correct order.
        // Therefore we have to create a positions table by ourself and create the next position in the same transaction.
        // Read comments from the following article: https://dev.to/kspeakman/event-storage-in-postgres-4dk2
        var query = dbContext.Database.SqlQuery<long>($"SELECT UpdatePositions8({parameter})");

        return (await query.ToListAsync(ct)).Single();
    }

    public bool IsDuplicateException(Exception exception)
    {
        Exception? ex = exception;

        while (ex != null)
        {
            // Primary Key and Unique Index constraint
            if (ex.Message.Contains("Duplicate entry", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            ex = ex.InnerException;
        }

        return false;
    }
}
