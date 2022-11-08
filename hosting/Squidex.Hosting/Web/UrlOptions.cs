// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

using Squidex.Hosting.Configuration;

#pragma warning disable SA1011 // Closing square brackets should be spaced correctly
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.

namespace Squidex.Hosting.Web;

public sealed class UrlOptions : IValidatableOptions
{
    public string[]? KnownProxies { get; set; }

    public string[]? TrustedHosts { get; set; }

    public bool EnableForwardHeaders { get; set; } = true;

    public bool EnforceHTTPS { get; set; } = false;

    public bool EnforceHost { get; set; } = false;

    public int? HttpsPort { get; set; } = 443;

    public string? CallbackUrl { get; set; }

    public string BaseUrl { get; set; }

    public string? BasePath { get; set; }

    public IEnumerable<ConfigurationError> Validate()
    {
        if (string.IsNullOrWhiteSpace(BaseUrl))
        {
            yield return new ConfigurationError("Value is required.", nameof(BaseUrl));
        }

        if (!Uri.IsWellFormedUriString(BaseUrl, UriKind.Absolute))
        {
            yield return new ConfigurationError("Value is not an absolute URL.", nameof(BaseUrl));
        }
    }
}
