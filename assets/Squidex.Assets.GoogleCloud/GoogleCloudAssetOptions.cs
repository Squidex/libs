// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

using Squidex.Hosting.Configuration;

namespace Squidex.Assets.GoogleCloud;

public sealed class GoogleCloudAssetOptions : IValidatableOptions
{
    public string Bucket { get; set; }

    public IEnumerable<ConfigurationError> Validate()
    {
        if (string.IsNullOrWhiteSpace(Bucket))
        {
            yield return new ConfigurationError("Value is required.", nameof(Bucket));
        }
    }
}
