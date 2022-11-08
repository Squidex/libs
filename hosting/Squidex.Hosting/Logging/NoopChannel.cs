// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

using Squidex.Log;

namespace Squidex.Hosting.Logging;

public sealed class NoopChannel : ILogChannel
{
    public void Log(SemanticLogLevel logLevel, string message)
    {
    }
}
