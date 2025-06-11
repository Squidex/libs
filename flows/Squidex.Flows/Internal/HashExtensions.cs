// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

namespace Squidex.Flows.Internal;

public static class HashExtensions
{
    public static int GetDeterministicHashCode(this string source)
    {
        uint hash = 0x811C9DC5;
        foreach (char c in source)
        {
            hash ^= c;
            hash *= 0x01000193;
        }

        return unchecked((int)hash);
    }
}
