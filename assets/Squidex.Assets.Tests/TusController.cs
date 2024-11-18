// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

using Microsoft.AspNetCore.Mvc;

namespace Squidex.Assets;

public class TusController(AssetTusRunner runner) : Controller
{
#pragma warning disable ASP0018 // Unused route parameter
    [Route("files/controller/{**catchAll}")]
#pragma warning restore ASP0018 // Unused route parameter
    public async Task<IActionResult> Tus()
    {
        var (result, file) = await runner.InvokeAsync(HttpContext, Url.Action(null, new { catchAll = (string?)null })!);

        if (file != null)
        {
            TusServerFixture.Files.Add(file);
            return Ok(new { id = "123" });
        }

        return result;
    }
}
