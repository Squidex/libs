// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

using Microsoft.AspNetCore.TestHost;
using MongoDB.Driver;
using MongoDB.Driver.GridFS;
using Squidex.Assets.TusAdapter;
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
        var builder = WebApplication.CreateBuilder();
        builder.Logging.ClearProviders();
        builder.Logging.ConfigureSemanticLog(builder.Configuration);

        builder.Services.AddSingleton(() =>
        {
            var mongoClient = new MongoClient("mongodb://localhost");
            var mongoDatabase = mongoClient.GetDatabase("TusTest");

            return mongoDatabase;
        });
        builder.Services.AddInitializer();
        builder.Services.AddMongoAssetStore(c =>
        {
            var mongoDatabase = c.GetRequiredService<IMongoDatabase>();

            var gridFSBucket = new GridFSBucket<string>(mongoDatabase, new GridFSBucketOptions
            {
                BucketName = "fs",
            });

            return gridFSBucket;
        });
        builder.Services.AddMongoAssetKeyValueStore();
        builder.Services.AddAssetTus();
        builder.Services.AddRouting();
        builder.Services.AddMvc();
        builder.WebHost.UseTestServer();

        var app = builder.Build();
        app.UseTus(httpContext => new DefaultTusConfiguration
        {
            Store = httpContext.RequestServices.GetRequiredService<ITusStore>(),
            Events = new Events
            {
                OnFileCompleteAsync = async eventContext =>
                {
                    var file = (AssetTusFile)(await eventContext.GetFileAsync());

                    Files.Add(file);
                },
            },
            UrlPath = "/files/middleware",
        });

        app.UseRouting();
        app.MapControllers();

        app.Start();

        TestServer = app.GetTestServer();

        Client = TestServer.CreateClient();
    }
}
