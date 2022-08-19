// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

using System.Text;
using Microsoft.AspNetCore.Http;

namespace Squidex.Hosting.Web
{
    public sealed class HtmlTransformMiddleware
    {
        private readonly HtmlTransformOptions options;
        private readonly RequestDelegate next;

        public HtmlTransformMiddleware(HtmlTransformOptions options, RequestDelegate next)
        {
            this.options = options;
            this.next = next;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            var response = context.Response;

            var responseBuffer = new MemoryStream();
            var responseBody = response.Body;

            response.Body = responseBuffer;
            try
            {
                await next(context);
            }
            finally
            {
                response.Body = responseBody;
            }

            // Nothing needs to be written here.
            if (responseBuffer.Length == 0 || response.StatusCode == StatusCodes.Status304NotModified)
            {
                return;
            }

            // We need to change the content length header.
            if (!response.HasStarted && response.ContentType.StartsWith("text/html", StringComparison.OrdinalIgnoreCase))
            {
                var html = Encoding.UTF8.GetString(responseBuffer.ToArray());

                if (options.AdjustBase)
                {
                    html = html.AdjustBase(context);
                }

                if (options.Transform != null)
                {
                    html = await options.Transform(html, context);
                }

                var bytes = Encoding.UTF8.GetBytes(html);

                // Change the content length in case the transformation has added chars.
                response.ContentLength = bytes.Length;

                await response.BodyWriter.WriteAsync(bytes, context.RequestAborted);
            }
            else
            {
                responseBuffer.Position = 0;

                await responseBuffer.CopyToAsync(responseBody, context.RequestAborted);
            }
        }
    }
}
