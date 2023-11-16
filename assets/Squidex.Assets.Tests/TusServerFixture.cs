// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

using Microsoft.AspNetCore.TestHost;
using MongoDB.Driver;
using MongoDB.Driver.GridFS;
using tusdotnet;
using tusdotnet.Interfaces;
using tusdotnet.Models;
using tusdotnet.Models.Configuration;

namespace Squidex.Assets;

public class TusServerFixture
{
    public static List<AssetTusFile> Files { get; } = [];

    public TestServer TestServer { get; private set; }

    public HttpClient Client { get; private set; }

    public TusServerFixture()
    {
        TestServer = new TestServer(new WebHostBuilder()
            .ConfigureLogging((context, builder) =>
            {
                builder.ClearProviders();
                builder.ConfigureSemanticLog(context.Configuration);
            })
            .ConfigureServices(services =>
            {
                var mongoClient = new MongoClient("mongodb://localhost");
                var mongoDatabase = mongoClient.GetDatabase("TusTest");

                var gridFSBucket = new GridFSBucket<string>(mongoDatabase, new GridFSBucketOptions
                {
                    BucketName = "fs"
                });

                services.AddSingleton(mongoDatabase);
                services.AddInitializer();
                services.AddMongoAssetStore(c => gridFSBucket);
                services.AddMongoAssetKeyValueStore();
                services.AddAssetTus();
                services.AddRouting();
                services.AddMvc();
            })
            .Configure(app =>
            {
                app.UseTus(httpContext => new DefaultTusConfiguration
                {
                    Store = httpContext.RequestServices.GetRequiredService<ITusStore>(),
                    Events = new Events
                    {
                        OnFileCompleteAsync = async eventContext =>
                        {
                            var file = (AssetTusFile)(await eventContext.GetFileAsync());

                            Files.Add(file);
                        }
                    },
                    UrlPath = "/files/middleware"
                });

                app.UseRouting();

                app.UseEndpoints(builder =>
                {
                    builder.MapControllers();
                });
            }));

        Client = TestServer.CreateClient();
    }
}
