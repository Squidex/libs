// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

namespace Squidex.Flows;

public static class FlowConsole
{
    private static readonly AsyncLocal<Action<string>?> CurrentOutput = new AsyncLocal<Action<string>?>();

    public static Action<string>? Output
    {
        get => CurrentOutput.Value;
        set => CurrentOutput.Value = value;
    }

    public static void Out(string message)
    {
        Output?.Invoke(message);
    }
}
