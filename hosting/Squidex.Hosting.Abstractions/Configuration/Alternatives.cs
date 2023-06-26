// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

namespace Microsoft.Extensions.Configuration;

public sealed class Alternatives : Dictionary<string, Action>
{
    public Alternatives()
        : base(StringComparer.OrdinalIgnoreCase)
    {
    }
}
