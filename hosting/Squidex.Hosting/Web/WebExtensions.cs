// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Squidex.Hosting;
using Squidex.Hosting.Web;

namespace Microsoft.AspNetCore.Builder
{
    public static class WebExtensions
    {
        public static void UseHtmlTransform(this IApplicationBuilder app, HtmlTransformOptions? options = null)
        {
            app.UseMiddleware<HtmlTransformMiddleware>(options ?? new HtmlTransformOptions());
        }

        public static void UsePathOverride(this IApplicationBuilder app, PathString path)
        {
            app.Use((context, next) =>
            {
                context.Request.Path = path;
                return next();
            });
        }

        public static void Use404(this IApplicationBuilder app)
        {
            app.Use((HttpContext context, RequestDelegate next) =>
            {
                context.Response.StatusCode = 404;
                return Task.CompletedTask;
            });
        }

        public static void UseDefaultPathBase(this IApplicationBuilder app)
        {
            var urlGenerator = app.ApplicationServices.GetRequiredService<IUrlGenerator>();

            var basePath = urlGenerator.BuildBasePath();

            app.UsePathBase(basePath);
        }

        public static void UseDefaultForwardRules(this IApplicationBuilder app)
        {
            var urlsOptions = app.ApplicationServices.GetRequiredService<IOptions<UrlOptions>>().Value;

            if (urlsOptions.EnableForwardHeaders)
            {
                app.UseForwardedHeaders();
            }

            app.UseMiddleware<CleanupHostMiddleware>();

            if (urlsOptions.EnforceHost)
            {
                app.UseHostFiltering();
            }

            if (urlsOptions.EnforceHTTPS)
            {
                app.UseHttpsRedirection();
            }
        }

        public static bool IsHtmlPath(this HttpContext context)
        {
            return context.Request.Path.Value?.EndsWith(".html", StringComparison.OrdinalIgnoreCase) == true;
        }

        public static bool IsIndex(this HttpContext context)
        {
            return
                context.Request.Path == "/" ||
                context.Request.Path == string.Empty ||
                context.Request.Path.StartsWithSegments("/index.html", StringComparison.OrdinalIgnoreCase);
        }
    }
}
