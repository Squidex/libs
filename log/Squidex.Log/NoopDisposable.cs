﻿// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

namespace Squidex.Log;

public sealed class NoopDisposable : IDisposable
{
    public static readonly NoopDisposable Instance = new NoopDisposable();

    private NoopDisposable()
    {
    }

    public void Dispose()
    {
    }
}
