// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

namespace Squidex.Hosting.Configuration;

[Serializable]
public sealed record ConfigurationError
{
    public string Message { get; }

    public string? Path { get; }

    public ConfigurationError(string message, string? path = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(message);

        Message = message;

        Path = path;
    }

    public override string ToString()
    {
        var result = Message;

        if (!string.IsNullOrWhiteSpace(Path))
        {
            result = $"{Path.ToLowerInvariant()}: {Message}";
        }

        if (!result.EndsWith('.'))
        {
            result += ".";
        }

        return result;
    }
}
