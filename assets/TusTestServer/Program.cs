// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

using MongoDB.Driver;
using MongoDB.Driver.GridFS;
using Squidex.Assets;
using Squidex.Assets.Internal;
using tusdotnet.Interfaces;
using TusTestServer;
using TutTestServer;

var builder = WebApplication.CreateBuilder(args);

builder.WebHost.ConfigureKestrel(options =>
{
    options.Limits.MaxRequestBodySize = null;
});

var mongoClient = new MongoClient("mongodb://localhost");
var mongoDatabase = mongoClient.GetDatabase("TusTest");

var gridFSBucket = new GridFSBucket<string>(mongoDatabase, new GridFSBucketOptions
{
    BucketName = "fs"
});

builder.Services.AddMvc();

builder.Services.AddSingleton(
    mongoDatabase);

builder.Services.AddSingleton<IHostedService,
    Initializer>();

builder.Services.AddSingleton<AssetTusRunner,
    AssetTusRunner>();

builder.Services.AddSingleton<ITusStore,
    AssetTusStore>();
builder.Services.AddSingleton<ITusFileLockProvider,
    AssetFileLockProvider>();

builder.Services.AddSingleton(
    new MongoGridFsAssetStore(gridFSBucket));
builder.Services.AddSingleton<IAssetStore>(c => c.GetRequiredService<MongoGridFsAssetStore>());

builder.Services.AddSingleton(
    c => ActivatorUtilities.CreateInstance<AmazonS3AssetStore>(c,
        builder.Configuration.GetSection("amazonS3").Get<AmazonS3AssetOptions>()!));
builder.Services.AddSingleton<IAssetStore>(c => c.GetRequiredService<AmazonS3AssetStore>());

builder.Services.AddSingleton(
    c => ActivatorUtilities.CreateInstance<AzureBlobAssetStore>(c,
        builder.Configuration.GetSection("azureBlob").Get<AzureBlobAssetOptions>()!));
builder.Services.AddSingleton<IAssetStore>(c => c.GetRequiredService<AzureBlobAssetStore>());

builder.Services.AddSingleton(
    c => ActivatorUtilities.CreateInstance<GoogleCloudAssetStore>(c,
        builder.Configuration.GetSection("googleCloud").Get<GoogleCloudAssetOptions>()!));
builder.Services.AddSingleton<IAssetStore>(c => c.GetRequiredService<GoogleCloudAssetStore>());

builder.Services.AddSingleton(
    c => ActivatorUtilities.CreateInstance<FolderAssetStore>(c, "uploads"));
builder.Services.AddSingleton<IAssetStore>(c => c.GetRequiredService<FolderAssetStore>());

builder.Services.AddSingleton<IAssetKeyValueStore<TusMetadata>,
    MongoAssetKeyValueStore<TusMetadata>>();

var app = builder.Build();

app.UseMyTus<MongoGridFsAssetStore>(
    "/files/mongodb/");

app.UseMyTus<FolderAssetStore>(
    "/files/folder/");

app.UseMyTus<AmazonS3AssetStore>(
    "/files/amazon-s3/");

app.UseMyTus<AzureBlobAssetStore>(
    "/files/azure-blob/");

app.UseMyTus<GoogleCloudAssetStore>(
    "/files/google-cloud/");

app.UseStaticFiles();

app.UseRouting();
app.MapControllers();

app.Run();
