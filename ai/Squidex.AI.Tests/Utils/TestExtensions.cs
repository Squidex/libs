// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

using FluentAssertions.Equivalency;

namespace Squidex.AI.Utils;

public static class TestExtensions
{
    public static EquivalencyOptions<T> ExcludeToolValuesAs<T>(this EquivalencyOptions<T> options)
    {
        return options.Excluding(x => x.Name.StartsWith("As", StringComparison.Ordinal));
    }
}
