// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

using Squidex.Hosting.Configuration;

namespace Squidex.Assets;

public sealed class FTPAssetOptions : IValidatableOptions
{
    public string Path { get; set; } = "/";

    public string ServerHost { get; set; }

    public int ServerPort { get; set; } = 21;

    public string Username { get; set; }

    public string Password { get; set; }

    public IEnumerable<ConfigurationError> Validate()
    {
        if (string.IsNullOrWhiteSpace(ServerHost))
        {
            yield return new ConfigurationError("Value is required.", nameof(ServerHost));
        }

        if (string.IsNullOrWhiteSpace(Path))
        {
            yield return new ConfigurationError("Value is required.", nameof(Path));
        }
    }
}
