// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

using System.Runtime.CompilerServices;
using MongoDB.Driver;

namespace Squidex.Events.Mongo;

internal static class Extensions
{
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

    public static async IAsyncEnumerable<T> Empty<T>()
    {
        await Task.CompletedTask;
        yield break;
    }

    public static async IAsyncEnumerable<T> Take<T>(this IAsyncEnumerable<T> source, int count)
    {
        if (count <= 0)
        {
            yield break;
        }

        int taken = 0;
        await foreach (var item in source)
        {
            yield return item;

            taken++;
            if (taken >= count)
            {
                yield break;
            }
        }
    }
}
