// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

using Squidex.Hosting.Configuration;

namespace Squidex.Assets.Azure;

public sealed class AzureBlobAssetOptions : IValidatableOptions
{
    public string ConnectionString { get; set; }

    public string ContainerName { get; set; }

    public bool CreateFolder { get; set; } = true;

    public IEnumerable<ConfigurationError> Validate()
    {
        if (string.IsNullOrWhiteSpace(ConnectionString))
        {
            yield return new ConfigurationError("Value is required.", nameof(ConnectionString));
        }

        if (string.IsNullOrWhiteSpace(ContainerName))
        {
            yield return new ConfigurationError("Value is required.", nameof(ContainerName));
        }
    }
}
