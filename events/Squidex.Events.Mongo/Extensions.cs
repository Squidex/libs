﻿// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

using System.Globalization;
using System.Runtime.CompilerServices;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Squidex.Events.Mongo;

internal static class Extensions
{
    public static async Task<int> GetMajorVersionAsync(this IMongoDatabase database,
        CancellationToken ct = default)
    {
        var command =
            new BsonDocumentCommand<BsonDocument>(new BsonDocument
            {
                { "buildInfo", 1 },
            });

        var document = await database.RunCommandAsync(command, cancellationToken: ct);

        var versionString = document["version"].AsString;
        var versionMajor = versionString.Split('.')[0];

        int.TryParse(versionMajor, NumberStyles.Integer, CultureInfo.InvariantCulture, out int result);

        return result;
    }

    public static async IAsyncEnumerable<T> ToAsyncEnumerable<T>(this IFindFluent<T, T> find,
        [EnumeratorCancellation] CancellationToken ct = default)
    {
        using var cursor = await find.ToCursorAsync(ct);

        while (await cursor.MoveNextAsync(ct))
        {
            foreach (var item in cursor.Current)
            {
                ct.ThrowIfCancellationRequested();

                yield return item;
            }
        }
    }
}
