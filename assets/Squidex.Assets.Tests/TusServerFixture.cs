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
using tusdotnet.Models;
using tusdotnet.Models.Configuration;

namespace Squidex.Assets
{
    public class TusServerFixture
    {
        public static List<AssetTusFile> Files { get; } = new List<AssetTusFile>();

        public TestServer TestServer { get; private set; }

        public HttpClient Client { get; private set; }

        public sealed class Initializer : IHostedService
        {
            private readonly IEnumerable<IAssetStore> assetStores;
            private readonly IAssetKeyValueStore<TusMetadata> assetKeyValueStore;

            public Initializer(IEnumerable<IAssetStore> assetStores, IAssetKeyValueStore<TusMetadata> assetKeyValueStore)
            {
                this.assetStores = assetStores;
                this.assetKeyValueStore = assetKeyValueStore;
            }

            public async Task StartAsync(
                CancellationToken cancellationToken)
            {
                foreach (var assetStore in assetStores)
                {
                    await assetStore.InitializeAsync(cancellationToken);
                }

                await assetKeyValueStore.InitializeAsync(cancellationToken);
            }

            public Task StopAsync(
                CancellationToken cancellationToken)
            {
                return Task.CompletedTask;
            }
        }

        public TusServerFixture()
        {
            TestServer = new TestServer(new WebHostBuilder()
                .ConfigureServices(services =>
                {
                    var mongoClient = new MongoClient("mongodb://localhost");
                    var mongoDatabase = mongoClient.GetDatabase("TusTest");

                    var gridFSBucket = new GridFSBucket<string>(mongoDatabase, new GridFSBucketOptions
                    {
                        BucketName = "fs"
                    });

                    services.AddSingleton(mongoDatabase);

                    services.AddSingleton<IHostedService,
                        Initializer>();

                    services.AddSingleton<AssetTusRunner,
                        AssetTusRunner>();
                    services.AddSingleton<AssetTusStore,
                        AssetTusStore>();

                    services.AddSingleton<IAssetStore>(
                        new MongoGridFsAssetStore(gridFSBucket));
                    services.AddSingleton<IAssetKeyValueStore<TusMetadata>,
                        MongoAssetKeyValueStore<TusMetadata>>();

                    services.AddRouting();
                    services.AddMvc();
                })
                .Configure(app =>
                {
                    app.UseTus(httpContext => new DefaultTusConfiguration
                    {
                        Store = new AssetTusStore(
                            httpContext.RequestServices.GetRequiredService<IAssetStore>(),
                            httpContext.RequestServices.GetRequiredService<IAssetKeyValueStore<TusMetadata>>()),
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
}
