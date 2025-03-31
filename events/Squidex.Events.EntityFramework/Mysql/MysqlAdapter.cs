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
    public async Task<long> GetPositionAsync(DbContext dbContext,
        CancellationToken ct)
    {
        // Autoincremented positions are not necessarily in the correct order.
        // Therefore we have to create a positions table by ourself and create the next position in the same transaction.
        // Read comments from the following article: https://dev.to/kspeakman/event-storage-in-postgres-4dk2
        var query = dbContext.Database.SqlQuery<long>($"SELECT NextPosition()");

        return (await query.ToListAsync(ct)).Single();
    }

    public async Task InitializeAsync(DbContext dbContext,
        CancellationToken ct)
    {
        try
        {
            var storedProdecure = $@"
CREATE FUNCTION NextPosition() RETURNS BIGINT
READS SQL DATA
DETERMINISTIC
BEGIN
    DECLARE nextPosition bigint;

    UPDATE EventPosition
    SET Position = Position + 1
    WHERE Id = 1;
    
    SELECT Position INTO nextPosition FROM EventPosition WHERE Id = 1;

    RETURN nextPosition;
END;";
            await dbContext.Database.ExecuteSqlRawAsync(storedProdecure, ct);
        }
        catch (Exception ex) when (ex.Message.Contains("already exists", StringComparison.OrdinalIgnoreCase))
        {
            // MySQL has not simple check function.
        }

        var initialPosition = $@"
INSERT INTO EventPosition (Id, Position) 
VALUES (1, 1) 
ON DUPLICATE KEY UPDATE Id = Id;";
        await dbContext.Database.ExecuteSqlRawAsync(initialPosition, ct);
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
