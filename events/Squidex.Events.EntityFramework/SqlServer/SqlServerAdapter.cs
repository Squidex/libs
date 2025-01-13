// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

using Microsoft.EntityFrameworkCore;

namespace Squidex.Events.EntityFramework.SqlServer;

public sealed class SqlServerAdapter : IProviderAdapter
{
    public async Task<long> GetPositionAsync(DbContext dbContext,
        CancellationToken ct)
    {
        // await using var transaction = await dbContext.Database.BeginTransactionAsync(ct);

        // Autoincremented positions are not necessarily in the correct order.
        // Therefore we have to create a positions table by ourself and create the next position in the same transaction.
        var query = dbContext.Database.SqlQuery<long>($"EXEC NextPosition");

        long result;
        try
        {
            result = (await query.ToListAsync(ct)).Single();
            // await transaction.CommitAsync(ct);
        }
        catch (Exception)
        {
            // await transaction.RollbackAsync(ct);
            throw;
        }

        return result;
    }

    public async Task InitializeAsync(DbContext dbContext,
        CancellationToken ct)
    {
        var storedProdecure = $@"
CREATE OR ALTER PROCEDURE NextPosition
AS
BEGIN
	-- Increment the position
	UPDATE EventPosition
	SET Position = Position + 1
	WHERE Id = 1;

	SELECT Position FROM EventPosition WHERE Id = 1;
END;";
        await dbContext.Database.ExecuteSqlRawAsync(storedProdecure, ct);

        try
        {
            var initialPosition = $@"
IF NOT EXISTS(
    SELECT 1
    FROM EventPosition
    WHERE Position = 1
)
BEGIN
    INSERT INTO EventPosition(Id, Position)
    VALUES(1, 1);
END;";
            await dbContext.Database.ExecuteSqlRawAsync(initialPosition, ct);
        }
        catch (Exception ex) when (IsDuplicateException(ex))
        {
            // Somehow the check above does not work reliably.
        }
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

            ex = ex.InnerException;
        }

        return false;
    }
}
