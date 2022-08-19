// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

using Microsoft.AspNetCore.Http;

namespace Squidex.Hosting.Web
{
    public sealed class HtmlTransformOptions
    {
        public bool AdjustBase { get; set; } = true;

        public Func<string, HttpContext, ValueTask<string>>? Transform { get; set; }
    }
}
