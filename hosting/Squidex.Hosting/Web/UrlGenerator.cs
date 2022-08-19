// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using Squidex.Hosting.Configuration;

namespace Squidex.Hosting.Web
{
    public sealed class UrlGenerator : IUrlGenerator
    {
        private readonly HashSet<HostString> allTrustedHosts = new HashSet<HostString>();
        private readonly string baseUrl;
        private readonly string basePath;
        private readonly string? callbackUrl;

        public UrlGenerator(IOptions<UrlOptions> options)
        {
            var option = options.Value;

            if (TryBuildHost(option.BaseUrl, out var host1))
            {
                allTrustedHosts.Add(host1);
            }

            if (TryBuildHost(option.CallbackUrl, out var host2))
            {
                allTrustedHosts.Add(host2);
            }

            if (option.TrustedHosts != null)
            {
                foreach (var host in option.TrustedHosts)
                {
                    if (TryBuildHost(host, out var host3))
                    {
                        allTrustedHosts.Add(host3);
                    }
                }
            }

            basePath = GetBasePath(option.BasePath);
            baseUrl = GetFullUrl(option.BaseUrl, basePath);

            if (!string.IsNullOrWhiteSpace(option.CallbackUrl))
            {
                callbackUrl = GetFullUrl(option.CallbackUrl, basePath);
            }
        }

        public string BuildCallbackUrl()
        {
            if (string.IsNullOrWhiteSpace(callbackUrl))
            {
                return baseUrl;
            }

            return callbackUrl;
        }

        public string BuildCallbackUrl(string path, bool trailingSlash = true)
        {
            if (string.IsNullOrWhiteSpace(callbackUrl))
            {
                return BuildUrl(path, trailingSlash);
            }

            return callbackUrl.BuildFullUrl(path, trailingSlash);
        }

        public (string, int? Port) BuildHost()
        {
            if (!TryBuildHost(baseUrl, out var host))
            {
                var error = new ConfigurationError("urls:baseurl", "Value is required.");

                throw new ConfigurationException(error);
            }

            return (host.Host, host.Port);
        }

        public string BuildBasePath()
        {
            return basePath;
        }

        public string BuildUrl()
        {
            return baseUrl;
        }

        public string BuildUrl(string path, bool trailingSlash = true)
        {
            return baseUrl.BuildFullUrl(path, trailingSlash);
        }

        public bool IsAllowedHost(string? url)
        {
            if (!Uri.TryCreate(url, UriKind.RelativeOrAbsolute, out var uri))
            {
                return false;
            }

            return IsAllowedHost(uri);
        }

        public bool IsAllowedHost(Uri uri)
        {
            if (!uri.IsAbsoluteUri)
            {
                return true;
            }

            return allTrustedHosts.Contains(BuildHost(uri));
        }

        private static bool TryBuildHost(string? urlOrHost, out HostString host)
        {
            host = default;

            if (string.IsNullOrWhiteSpace(urlOrHost))
            {
                return false;
            }

            if (Uri.TryCreate(urlOrHost, UriKind.Absolute, out var uri1))
            {
                host = BuildHost(uri1);

                return true;
            }

            if (Uri.TryCreate($"http://{urlOrHost}", UriKind.Absolute, out var uri2))
            {
                host = BuildHost(uri2);

                return true;
            }

            return false;
        }

        private static HostString BuildHost(Uri uri)
        {
            return BuildHost(uri.Host, uri.Port);
        }

        private static HostString BuildHost(string host, int port)
        {
            if (port == 443 || port == 80)
            {
                return new HostString(host.ToLowerInvariant());
            }
            else
            {
                return new HostString(host.ToLowerInvariant(), port);
            }
        }

        private static string GetBasePath(string? basePath)
        {
            var path = basePath?.Trim(' ', '/');

            if (path == null)
            {
                return string.Empty;
            }

            return $"/{path}";
        }

        private static string GetFullUrl(string baseUrl, string basePath)
        {
            var url = baseUrl.TrimEnd(' ', '/');

            return url.EndsWith(basePath, StringComparison.OrdinalIgnoreCase) ? url : $"{url}{basePath}";
        }
    }
}
