// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

namespace Squidex.Flows;

public static class FlowConsole
{
    private static readonly AsyncLocal<Action<string, object?>?> CurrentOutput = new AsyncLocal<Action<string, object?>?>();

    public static Action<string, object?>? Output
    {
        get => CurrentOutput.Value;
        set => CurrentOutput.Value = value;
    }

    public static void Out(string message, object? dump = null)
    {
        Output?.Invoke(message, dump);
    }
}
