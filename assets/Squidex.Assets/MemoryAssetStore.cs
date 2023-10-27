// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

using System.Collections.Concurrent;
using Squidex.Assets.Internal;

namespace Squidex.Assets;

public class MemoryAssetStore : IAssetStore
{
    private readonly ConcurrentDictionary<string, MemoryStream> streams = new ConcurrentDictionary<string, MemoryStream>();
    private readonly AsyncLock readerLock = new AsyncLock();
    private readonly AsyncLock writerLock = new AsyncLock();

    public async Task<long> GetSizeAsync(string fileName,
        CancellationToken ct = default)
    {
        var name = GetFileName(fileName, nameof(fileName));

        if (!streams.TryGetValue(name, out var sourceStream))
        {
            throw new AssetNotFoundException(fileName);
        }

        using (await readerLock.LockAsync())
        {
            return sourceStream.Length;
        }
    }

    public virtual async Task CopyAsync(string sourceFileName, string targetFileName,
        CancellationToken ct = default)
    {
        Guard.NotNullOrEmpty(targetFileName, nameof(targetFileName));

        var sourceName = GetFileName(sourceFileName, nameof(sourceFileName));

        if (!streams.TryGetValue(sourceName, out var sourceStream))
        {
            throw new AssetNotFoundException(sourceName);
        }

        using (await readerLock.LockAsync())
        {
            await UploadAsync(targetFileName, sourceStream, false, ct);
        }
    }

    public virtual async Task DownloadAsync(string fileName, Stream stream, BytesRange range = default,
        CancellationToken ct = default)
    {
        Guard.NotNull(stream, nameof(stream));

        var name = GetFileName(fileName, nameof(fileName));

        if (!streams.TryGetValue(name, out var sourceStream))
        {
            throw new AssetNotFoundException(fileName);
        }

        using (await readerLock.LockAsync())
        {
            try
            {
                await sourceStream.CopyToAsync(stream, range, ct);
            }
            finally
            {
                sourceStream.Position = 0;
            }
        }
    }

    public virtual async Task<long> UploadAsync(string fileName, Stream stream, bool overwrite = false,
        CancellationToken ct = default)
    {
        Guard.NotNull(stream, nameof(stream));

        var name = GetFileName(fileName, nameof(fileName));

        var memoryStream = new MemoryStream();

        async Task CopyAsync()
        {
            using (await writerLock.LockAsync())
            {
                try
                {
                    await stream.CopyToAsync(memoryStream, 81920, ct);
                }
                finally
                {
                    memoryStream.Position = 0;
                }
            }
        }

        if (overwrite)
        {
            await CopyAsync();

            streams[name] = memoryStream;
        }
        else if (streams.TryAdd(name, memoryStream))
        {
            await CopyAsync();
        }
        else
        {
            throw new AssetAlreadyExistsException(name);
        }

        return memoryStream.Length;
    }

    public virtual Task DeleteByPrefixAsync(string prefix,
        CancellationToken ct = default)
    {
        Guard.NotNullOrEmpty(prefix, nameof(prefix));

        // ToList on concurrent dictionary is not thread safe, therefore we maintain our own local copy.
        HashSet<string>? toRemove = null;

        foreach (var (key, _) in streams)
        {
            if (key.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
            {
                toRemove ??= new HashSet<string>();
                toRemove.Add(key);
            }
        }

        if (toRemove != null)
        {
            foreach (var key in toRemove)
            {
                streams.Remove(key, out _);
            }
        }

        return Task.CompletedTask;
    }

    public virtual Task DeleteAsync(string fileName,
        CancellationToken ct = default)
    {
        var name = GetFileName(fileName, nameof(fileName));

        streams.TryRemove(name, out _);

        return Task.CompletedTask;
    }

    private static string GetFileName(string fileName, string parameterName)
    {
        Guard.NotNullOrEmpty(fileName, parameterName);

        return FilePathHelper.EnsureThatPathIsChildOf(fileName.Replace('\\', '/'), "./");
    }
}
