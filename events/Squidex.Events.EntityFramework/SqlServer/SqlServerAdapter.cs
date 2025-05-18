// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

using System.Data;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace Squidex.Events.EntityFramework.SqlServer;

public sealed class SqlServerAdapter : IProviderAdapter
{
    public async Task InitializeAsync(DbContext dbContext,
        CancellationToken ct)
    {
        await CreateTemporaryTableAsync(dbContext, ct);
        await CreateUpdatePositionAsync(dbContext, ct);
        await CreateUpdatePositionsAsync(dbContext, ct);
        await InitialPositionAsync(dbContext, ct);
    }

    private static async Task CreateUpdatePositionAsync(DbContext dbContext,
        CancellationToken ct)
    {
        var sql = $@"
CREATE OR ALTER PROCEDURE UpdatePositionV2
    @eventId UNIQUEIDENTIFIER
AS
BEGIN
	SET NOCOUNT ON;

    DECLARE @newPosition BIGINT;

    -- Update position
    UPDATE EventPosition
    SET Position = Position + 1
    WHERE Id = 1;

    -- Fetch new position
    SELECT @newPosition = Position
    FROM EventPosition
    WHERE Id = 1;

    -- Update Events table
    UPDATE Events
    SET Position = @newPosition
    WHERE Id = @eventId;

    SELECT @newPosition AS NewPosition;
END;";
        await dbContext.Database.ExecuteSqlRawAsync(sql, ct);
    }

    private static async Task CreateUpdatePositionsAsync(DbContext dbContext,
        CancellationToken ct)
    {
        var sql2 = $@"
CREATE OR ALTER PROCEDURE UpdatePositionsV2
    @eventIds EventIdTableType READONLY
AS
BEGIN
	SET NOCOUNT ON;

    DECLARE @newPosition BIGINT;
    DECLARE @count INT;

    SELECT @count = COUNT(*) FROM @eventIds;

    -- Update position
    UPDATE EventPosition
    SET Position = Position +  @count
    WHERE Id = 1;

    -- Fetch new position
    SELECT @newPosition = Position
    FROM EventPosition
    WHERE Id = 1;

    -- Update Events table
    UPDATE Events
    SET Position = @newPosition - @count + eventId.Idx
    FROM Events targetEvent
    JOIN @eventIds eventId ON targetEvent.Id = eventId.Id;

    SELECT @newPosition AS NewPosition;
END;";
        await dbContext.Database.ExecuteSqlRawAsync(sql2, ct);
    }

    private async Task InitialPositionAsync(DbContext dbContext,
        CancellationToken ct)
    {
        try
        {
            var sql = $@"
IF NOT EXISTS(
    SELECT 1
    FROM EventPosition
    WHERE Position = 1
)
BEGIN
    INSERT INTO EventPosition(Id, Position)
    VALUES(1, 1);
END;";
            await dbContext.Database.ExecuteSqlRawAsync(sql, ct);
        }
        catch (Exception ex) when (IsDuplicateException(ex))
        {
            // Somehow the check above does not work reliably.
        }
    }

    private async Task CreateTemporaryTableAsync(DbContext dbContext,
        CancellationToken ct)
    {
        try
        {
            var sql1 = $@"
CREATE TYPE EventIdTableType AS TABLE
(
    Id  UNIQUEIDENTIFIER,
    Idx INT
);";
            await dbContext.Database.ExecuteSqlRawAsync(sql1, ct);
        }
        catch (Exception ex) when (IsDuplicateException(ex))
        {
            // Somehow the check above does not work reliably.
        }
    }

    public async Task<long> UpdatePositionAsync(DbContext dbContext, Guid id,
        CancellationToken ct)
    {
        // Autoincremented positions are not necessarily in the correct order.
        // Therefore we have to create a positions table by ourself and create the next position in the same transaction.
        // Read comments from the following article: https://dev.to/kspeakman/event-storage-in-postgres-4dk2
        var query = dbContext.Database.SqlQuery<long>($"EXEC UpdatePositionsV2 {id}");

        return (await query.ToListAsync(ct)).Single();
    }

    public async Task<long> UpdatePositionsAsync(DbContext dbContext, Guid[] ids,
        CancellationToken ct)
    {
        var dataTable = new DataTable();
        dataTable.Columns.Add("Id", typeof(Guid));
        dataTable.Columns.Add("Index", typeof(int));

        var i = 1;
        foreach (var id in ids)
        {
            dataTable.Rows.Add(id, i);
            i++;
        }

        var parameter = new SqlParameter("@eventIds", SqlDbType.Structured)
        {
            Value = dataTable,
            // The table structure does not really matter.
            TypeName = "EventIdTableType",
        };

        // Autoincremented positions are not necessarily in the correct order.
        // Therefore we have to create a positions table by ourself and create the next position in the same transaction.
        // Read comments from the following article: https://dev.to/kspeakman/event-storage-in-postgres-4dk2
        var query = dbContext.Database.SqlQueryRaw<long>($"EXEC UpdatePositionsV2 @eventIds", parameter);

        return (await query.ToListAsync(ct)).Single();
    }

    public bool IsDuplicateException(Exception exception)
    {
        Exception? ex = exception;

        while (ex != null)
        {
            // Primary Key constraint
            if (ex.Message.Contains("PRIMARY KEY constraint", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            // Unique Index constraint.
            if (ex.Message.Contains("unique index", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            // Table already exists.
            if (ex.Message.Contains("already exists", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            ex = ex.InnerException;
        }

        return false;
    }
}
