// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

namespace Squidex.Caching;

internal sealed class StripedLock
{
    private readonly object[] locks;

    public StripedLock(int count)
    {
        // The modulo below is only uniform when the count is a power of two.
        if (count <= 0 || (count & (count - 1)) != 0)
        {
            throw new ArgumentOutOfRangeException(nameof(count), "Count must be a power of two.");
        }

        locks = new object[count];
        for (var i = 0; i < count; i++)
        {
            locks[i] = new object();
        }
    }

    public object Get(object key)
    {
        // Math.Abs would throw for int.MinValue, so mask the sign bit instead.
        var index = (key.GetHashCode() & int.MaxValue) % locks.Length;

        return locks[index];
    }
}
