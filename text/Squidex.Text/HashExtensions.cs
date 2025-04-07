// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

namespace Squidex.Text;

public static class HashExtensions
{
    public static int GetDeterministicHashCode(this string source)
    {
        const uint prime = 0x01000193;
        const uint basis = 0x811C9DC5;

        uint hash = basis;
        foreach (char c in source)
        {
            hash ^= c;
            hash *= prime;
        }

        return unchecked((int)hash);
    }
}
