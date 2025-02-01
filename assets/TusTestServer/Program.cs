// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

using MongoDB.Driver;
using MongoDB.Driver.GridFS;
using Squidex.Assets;
using Squidex.Assets.Azure;
using Squidex.Assets.GoogleCloud;
using Squidex.Assets.Mongo;
using Squidex.Assets.S3;
using TusTestServer;

var builder = WebApplication.CreateBuilder(args);

builder.WebHost.ConfigureKestrel(options =>
{
    options.Limits.MaxRequestBodySize = null;
});

var mongoClient = new MongoClient("mongodb://localhost");
var mongoDatabase = mongoClient.GetDatabase("TusTest");

var gridFSBucket = new GridFSBucket<string>(mongoDatabase, new GridFSBucketOptions
{
    BucketName = "fs",
});

builder.Services.AddMvc();
builder.Services.AddInitializer();
builder.Services.AddSingleton(mongoDatabase);

builder.Services.AddAmazonS3AssetStore(builder.Configuration);
builder.Services.AddAssetTus();
builder.Services.AddAzureBlobAssetStore(builder.Configuration);
builder.Services.AddFolderAssetStore(builder.Configuration);
builder.Services.AddGoogleCloudAssetStore(builder.Configuration);
builder.Services.AddMongoAssetKeyValueStore();
builder.Services.AddMongoAssetStore(c => gridFSBucket);

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

await app.RunAsync();
