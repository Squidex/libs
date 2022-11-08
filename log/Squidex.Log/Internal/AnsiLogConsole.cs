﻿// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

using System.Diagnostics.CodeAnalysis;

namespace Squidex.Log.Internal;

[ExcludeFromCodeCoverage]
public sealed class AnsiLogConsole : IConsole
{
    private readonly bool logToStdError;

    public AnsiLogConsole(bool logToStdError)
    {
        this.logToStdError = logToStdError;
    }

    public void Reset()
    {
    }

    public void WriteLine(int color, string message)
    {
        if (color != 0 && logToStdError)
        {
            Console.Error.WriteLine(message);
        }
        else
        {
            Console.WriteLine(message);
        }
    }
}
