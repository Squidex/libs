// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

using Microsoft.Extensions.Logging;
using Squidex.Assets.Internal;

namespace Squidex.Assets;

public sealed class FolderAssetStore : IAssetStore
{
    private const int BufferSize = 81920;
    private readonly ILogger<FolderAssetStore> log;
    private readonly DirectoryInfo directory;

    public FolderAssetStore(string path, ILogger<FolderAssetStore> log)
    {
        Guard.NotNullOrEmpty(path, nameof(path));
        Guard.NotNull(log, nameof(log));

        this.log = log;

        directory = new DirectoryInfo(path);
    }

    public Task InitializeAsync(
        CancellationToken ct)
    {
        try
        {
            if (!directory.Exists)
            {
                directory.Create();
            }

            log.LogInformation("Initialized with {folder}", directory.FullName);

            return Task.CompletedTask;
        }
        catch (Exception ex)
        {
            throw new AssetStoreException($"Cannot access directory {directory.FullName}", ex);
        }
    }

    public Task<long> GetSizeAsync(string fileName,
        CancellationToken ct = default)
    {
        var file = GetFile(fileName, nameof(fileName));

        try
        {
            return Task.FromResult(file.Length);
        }
        catch (FileNotFoundException ex)
        {
            throw new AssetNotFoundException(fileName, ex);
        }
    }

    public Task CopyAsync(string sourceFileName, string targetFileName,
        CancellationToken ct = default)
    {
        var targetFile = GetFile(targetFileName, nameof(targetFileName));
        var sourceFile = GetFile(sourceFileName, nameof(sourceFileName));

        try
        {
            Directory.CreateDirectory(targetFile.Directory!.FullName);

            sourceFile.CopyTo(targetFile.FullName);

            return Task.CompletedTask;
        }
        catch (IOException) when (targetFile.Exists)
        {
            throw new AssetAlreadyExistsException(targetFileName);
        }
        catch (FileNotFoundException ex)
        {
            throw new AssetNotFoundException(sourceFileName, ex);
        }
        catch (DirectoryNotFoundException ex)
        {
            throw new AssetNotFoundException(sourceFileName, ex);
        }
    }

    public async Task DownloadAsync(string fileName, Stream stream, BytesRange range = default,
        CancellationToken ct = default)
    {
        Guard.NotNull(stream, nameof(stream));

        var file = GetFile(fileName, nameof(fileName));

        try
        {
            await using (var fileStream = file.OpenRead())
            {
                await fileStream.CopyToAsync(stream, range, ct);
            }
        }
        catch (FileNotFoundException ex)
        {
            throw new AssetNotFoundException(fileName, ex);
        }
        catch (DirectoryNotFoundException ex)
        {
            throw new AssetNotFoundException(fileName, ex);
        }
    }

    public async Task<long> UploadAsync(string fileName, Stream stream, bool overwrite = false,
        CancellationToken ct = default)
    {
        Guard.NotNull(stream, nameof(stream));

        var file = GetFile(fileName, nameof(fileName));

        Directory.CreateDirectory(file.Directory!.FullName);

        try
        {
            await using (var fileStream = file.Open(overwrite ? FileMode.Create : FileMode.CreateNew, FileAccess.Write))
            {
                await stream.CopyToAsync(fileStream, BufferSize, ct);
            }
        }
        catch (IOException) when (file.Exists)
        {
            throw new AssetAlreadyExistsException(file.Name);
        }

        return file.Length;
    }

    public Task DeleteByPrefixAsync(string prefix,
        CancellationToken ct = default)
    {
        var cleanedPrefix = GetFileName(prefix, nameof(prefix));

        if (Delete(GetPath(prefix)))
        {
            return Task.CompletedTask;
        }

        foreach (var file in directory.GetFiles("*.*", SearchOption.AllDirectories))
        {
            var relativeName = GetFileName(Path.GetRelativePath(directory.FullName, file.FullName), string.Empty);

            if (relativeName.StartsWith(cleanedPrefix, StringComparison.Ordinal))
            {
                try
                {
                    file.Delete();
                }
                catch (DirectoryNotFoundException)
                {
                    continue;
                }
            }
        }

        return Task.CompletedTask;
    }

    public Task DeleteAsync(string fileName,
        CancellationToken ct = default)
    {
        try
        {
            var file = GetFile(fileName, nameof(fileName));

            if (file.Exists)
            {
                file.Delete();
            }

            return Task.CompletedTask;
        }
        catch (DirectoryNotFoundException)
        {
            return Task.CompletedTask;
        }
    }

    private static bool Delete(string path)
    {
        try
        {
            Directory.Delete(path, true);

            return true;
        }
        catch (IOException)
        {
            return false;
        }
    }

    private FileInfo GetFile(string fileName, string parameterName)
    {
        var cleaned = GetFileName(fileName, parameterName);

        return new FileInfo(GetPath(cleaned));
    }

    private string GetPath(string name)
    {
        return Path.Combine(directory.FullName, name);
    }

    private static string GetFileName(string fileName, string parameterName)
    {
        Guard.NotNullOrEmpty(fileName, parameterName);

        return fileName.Replace("\\", "/", StringComparison.Ordinal);
    }
}
