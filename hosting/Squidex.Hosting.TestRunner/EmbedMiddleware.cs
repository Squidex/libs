// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

namespace Squidex.Hosting
{
    public sealed class EmbedMiddleware
    {
        private readonly RequestDelegate next;

        public EmbedMiddleware(RequestDelegate next)
        {
            this.next = next;
        }

        public Task InvokeAsync(HttpContext context)
        {
            var request = context.Request;

            if (request.Path.StartsWithSegments("/embed", StringComparison.Ordinal, out var remaining))
            {
                request.Path = remaining;
                request.PathBase += "/embed";

                context.Items["embed"] = true;
            }

            return next(context);
        }
    }
}
