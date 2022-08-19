// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

namespace Squidex.Hosting.Configuration
{
    [Serializable]
    public sealed class ConfigurationError
    {
        public string Message { get; }

        public string? Path { get; }

        public ConfigurationError(string message, string? path = null)
        {
            if (string.IsNullOrWhiteSpace(message))
            {
                throw new ArgumentException("Message cannot be null or empty.", nameof(message));
            }

            Path = path;

            Message = message;
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
}
