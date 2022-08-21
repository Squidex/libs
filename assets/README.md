# Squidex.Assets

Abstraction over several asset storages like Amazon S3.

It provides 4 services:

1. An abstraction to store assets and files in different providers: `IAssetStore` with several implementations.
2. An abstraction to resize images: `IAssetThumbnailGenerator` with several implementations.
3. A providers for tus.io for resumable uploads: https://github.com/tusdotnet/tusdotnet.
4. An extension for HttpClient to support resumable uploads.

This library has no integration to dependency injection, therefore you have to register your services manually. 

## IAssetStore

The asset store is an abstraction to store files in your preferred locations:

### 1. Folder Provider

[FolderAssetStore.cs](Squidex.Assets/FolderAssetStore.cs)

### 2. In Memory Provider (for testing)

[MemoryAssetStore.cs](Squidex.Assets/MemoryAssetStore.cs)

> Should only be used for testing.

### 3. Azure Blob Store

[AzureBlobStore.cs](Squidex.Asset.Azure/AzureBlobStore.cs)

### 4. FTP

[FtpAssetStore.cs](Squidex.Asset.FTP/FtpAssetStore.cs)

> Should only be used if no other alternatives is suitable, because to bad performance.

### 5. Google Cloud

[FtpAssetStore.cs](Squidex.Asset.GoogleCloud/GoogleCloudAssetStore.cs)

### 6. MongoDb Grid FS

[MongoGridFsAssetStore.cs](Squidex.Asset.Mongo/MongoGridFsAssetStore.cs)

### 7. Amazon S3

[AmazonS3AssetStore.cs](Squidex.Asset.S3/AmazonS3AssetStore.cs)

## IAssetThumbnailGenerator

The idea of this interface is to provide a service to resize image and to get basic image inforamtion such as width, height and oritentation.

There are several implementations:

### 1. ImageMagick

[ImageMagickThumbnailGenerator.cs](Squidex.Assets.ImageMagick/ImageMagickThumbnailGenerator.cs)

### 2. ImageSharp

[ImageSharpThumbnailGenerator.cs](Squidex.Assets.ImageSharp/ImageSharpThumbnailGenerator.cs)