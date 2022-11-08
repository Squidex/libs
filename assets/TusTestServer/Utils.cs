// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

using Squidex.Assets;
using tusdotnet;
using tusdotnet.Models;
using tusdotnet.Models.Configuration;

namespace TusTestServer;

public static class Utils
{
    public static void UseMyTus<T>(this WebApplication app, string path) where T : IAssetStore
    {
        app.UseTus(httpContext => new DefaultTusConfiguration
        {
            Store = new AssetTusStore(
                httpContext.RequestServices.GetRequiredService<T>(),
                httpContext.RequestServices.GetRequiredService<IAssetKeyValueStore<TusMetadata>>()),
            UrlPath = path,
            Events = new Events
            {
                OnFileCompleteAsync = async eventContext =>
                {
                    var file = (AssetFile)(await eventContext.GetFileAsync());

                    await using var fileStream = file.OpenRead();

                    var name = file.FileName;

                    if (string.IsNullOrWhiteSpace(name))
                    {
                        name = Guid.NewGuid().ToString();
                    }

                    Directory.CreateDirectory("uploads");

                    await using (var stream = new FileStream($"uploads/{name}", FileMode.Create))
                    {
                        await fileStream.CopyToAsync(stream, eventContext.CancellationToken);
                    }
                }
            }
        });
    }
}
