// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

using Squidex.Hosting.Configuration;

namespace Squidex.Assets;

public sealed class FolderAssetOptions : IValidatableOptions
{
    public string Path { get; set; }

    public IEnumerable<ConfigurationError> Validate()
    {
        if (string.IsNullOrWhiteSpace(Path))
        {
            yield return new ConfigurationError("Value is required.", nameof(Path));
        }
    }
}
