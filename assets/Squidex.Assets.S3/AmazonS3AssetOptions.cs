// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

using Squidex.Hosting.Configuration;

namespace Squidex.Assets.S3;

public sealed class AmazonS3AssetOptions : IValidatableOptions
{
    public string ServiceUrl { get; set; }

    public string RegionName { get; set; }

    public string Bucket { get; set; }

    public string BucketFolder { get; set; }

    public string AccessKey { get; set; }

    public string SecretKey { get; set; }

    public bool ForcePathStyle { get; set; }

    public bool DisablePayloadSigning { get; set; }

    public IEnumerable<ConfigurationError> Validate()
    {
        if (string.IsNullOrWhiteSpace(Bucket))
        {
            yield return new ConfigurationError("Value is required.", nameof(Bucket));
        }

        if (string.IsNullOrWhiteSpace(AccessKey))
        {
            yield return new ConfigurationError("Value is required.", nameof(AccessKey));
        }

        if (string.IsNullOrWhiteSpace(SecretKey))
        {
            yield return new ConfigurationError("Value is required.", nameof(SecretKey));
        }
    }
}
