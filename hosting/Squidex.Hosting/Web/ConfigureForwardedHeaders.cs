// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

using System.Net;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.Extensions.Options;

namespace Squidex.Hosting.Web
{
    public sealed class ConfigureForwardedHeaders : IConfigureOptions<ForwardedHeadersOptions>
    {
        private readonly UrlOptions urlOptions;
        private readonly IUrlGenerator urlGenerator;

        public ConfigureForwardedHeaders(IOptions<UrlOptions> urlOptions, IUrlGenerator urlGenerator)
        {
            this.urlOptions = urlOptions.Value;
            this.urlGenerator = urlGenerator;
        }

        public void Configure(ForwardedHeadersOptions options)
        {
            options.AllowedHosts = new List<string>
            {
                urlGenerator.BuildHost().ToString()
            };

            options.ForwardedHeaders = ForwardedHeaders.XForwardedProto | ForwardedHeaders.XForwardedHost;
            options.ForwardLimit = null;
            options.RequireHeaderSymmetry = false;

            options.KnownNetworks.Clear();
            options.KnownProxies.Clear();

            if (urlOptions.KnownProxies != null)
            {
                foreach (var proxy in urlOptions.KnownProxies)
                {
                    if (IPAddress.TryParse(proxy, out var address))
                    {
                        options.KnownProxies.Add(address);
                    }
                }
            }
        }
    }
}
