// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Primitives;
using tusdotnet;
using tusdotnet.Interfaces;
using tusdotnet.Models;
using tusdotnet.Models.Configuration;

namespace Squidex.Assets.TusAdapter;

public sealed class AssetTusRunner
{
    private const string TusFile = "TUS_FILE";
    private const string TusUrl = "TUS_FILE";
    private static readonly RequestDelegate Next = _ => Task.CompletedTask;
    private readonly TusCoreMiddleware middleware;

    public AssetTusRunner(
        ITusStore tusStore,
        ITusFileLockProvider tusFileLockProvider)
    {
        var events = new Events
        {
            OnFileCompleteAsync = async eventContext =>
            {
                var file = await eventContext.GetFileAsync();

                if (file is AssetTusFile tusFile)
                {
                    eventContext.HttpContext.Items[TusFile] = file;
                }
            },
        };

        middleware = new TusCoreMiddleware(Next, ctx =>
        {
            var configuration = new DefaultTusConfiguration
            {
                Store = tusStore,

                // Use a custom lock provider that supports multiple servers.
                FileLockProvider = tusFileLockProvider,

                // Reuse the events to avoid allocations.
                Events = events,

                // Get the url from the controller that is temporarily stored in the items.
                UrlPath = ctx.Items[TusUrl]!.ToString(),
            };

            return Task.FromResult(configuration);
        });
    }

    public async Task<(IActionResult Result, AssetTusFile? File)> InvokeAsync(HttpContext httpContext, string baseUrl)
    {
        var customContext = CloneContext(httpContext, baseUrl);

        await middleware.Invoke(customContext);

        var file = customContext.Items[TusFile] as AssetTusFile;

        if (file != null)
        {
            // Register the file to clean it up after the request.
            httpContext.Response.RegisterForDispose(file);
        }

        var result = new TusActionResult(customContext.Response);

        // Apply headers so that bypasses from the controller do not destroy the tus headers.
        result.ApplyHeaders(httpContext);

        return (new TusActionResult(customContext.Response), file);
    }

    // From: https://github.com/dotnet/aspnetcore/blob/main/src/SignalR/common/Http.Connections/src/Internal/HttpConnectionDispatcher.cs#L509
    private static DefaultHttpContext CloneContext(HttpContext httpContext, string baseUrl)
    {
        // The reason we're copying the base features instead of the HttpContext properties is
        // so that we can get all of the logic built into DefaultHttpContext to extract higher level
        // structure from the low level properties
        var existingRequestFeature = httpContext.Features.Get<IHttpRequestFeature>()!;

        // Create a clone of the headers.
        var requestHeaders = new Dictionary<string, StringValues>(existingRequestFeature.Headers, StringComparer.OrdinalIgnoreCase);

        var requestFeature = new HttpRequestFeature
        {
            Body = existingRequestFeature.Body,
            Headers = new HeaderDictionary(requestHeaders),
            Method = existingRequestFeature.Method,
            Path = existingRequestFeature.Path,
            PathBase = existingRequestFeature.PathBase,
            Protocol = existingRequestFeature.Protocol,
            QueryString = existingRequestFeature.QueryString,
            Scheme = existingRequestFeature.Scheme,
        };

        var responseFeature = new HttpResponseFeature();

        var features = new FeatureCollection();
        features.Set(httpContext.Features.Get<IRequestBodyPipeFeature>());
        features.Set<IHttpRequestFeature>(requestFeature);
        features.Set<IHttpResponseFeature>(responseFeature);

        // Override the body for error messages from TUS middleware. They are usually small so buffering here is okay.
        features.Set<IHttpResponseBodyFeature>(new StreamResponseBodyFeature(new MemoryStream()));

        var customContext = new DefaultHttpContext(features);

        customContext.Items[TusUrl] = baseUrl;

        return customContext;
    }
}
