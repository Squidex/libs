// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

namespace Squidex.Hosting
{
    public interface IUrlGenerator
    {
        (string, int? Port) BuildHost();

        string BuildCallbackUrl();

        string BuildCallbackUrl(string path, bool trailingSlash = true);

        string BuildUrl();

        string BuildUrl(string path, bool trailingSlash = true);

        string BuildBasePath();

        bool IsAllowedHost(string? url);

        bool IsAllowedHost(Uri uri);
    }
}
