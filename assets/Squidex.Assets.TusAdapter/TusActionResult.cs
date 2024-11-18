// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Squidex.Assets;

internal sealed class TusActionResult(HttpResponse response) : IActionResult
{
    public int StatusCode => response.StatusCode;

    public IHeaderDictionary Headers => response.Headers;

    public Stream Body => response.Body;

    public void ApplyHeaders(HttpContext context)
    {
        foreach (var (key, value) in Headers)
        {
            context.Response.Headers[key] = value;
        }
    }

    public async Task ExecuteResultAsync(ActionContext context)
    {
        if (StatusCode > 0)
        {
            context.HttpContext.Response.StatusCode = StatusCode;
        }

        ApplyHeaders(context.HttpContext);

        if (Body.Length > 0)
        {
            Body.Seek(0, SeekOrigin.Begin);

            await Body.CopyToAsync(context.HttpContext.Response.Body, context.HttpContext.RequestAborted);
        }
    }
}
