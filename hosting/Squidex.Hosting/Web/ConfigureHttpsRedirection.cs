// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

using Microsoft.AspNetCore.HttpsPolicy;
using Microsoft.Extensions.Options;

namespace Squidex.Hosting.Web;

public sealed class ConfigureHttpsRedirection : IConfigureOptions<HttpsRedirectionOptions>
{
    private readonly UrlOptions urlOptions;

    public ConfigureHttpsRedirection(IOptions<UrlOptions> urlOptions)
    {
        this.urlOptions = urlOptions.Value;
    }

    public void Configure(HttpsRedirectionOptions options)
    {
        options.HttpsPort = urlOptions.HttpsPort;
    }
}
