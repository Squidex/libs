// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

using System.Diagnostics.CodeAnalysis;
using FluentFTP;
using Microsoft.Extensions.Logging;
using Squidex.Assets.Internal;

namespace Squidex.Assets;

[ExcludeFromCodeCoverage]
public sealed class FTPAssetStore : IAssetStore
{
    private readonly ILogger<FTPAssetStore> log;
    private readonly FTPClientPool pool;
    private readonly FTPAssetOptions options;

    public FTPAssetStore(Func<IAsyncFtpClient> clientFactory, FTPAssetOptions options, ILogger<FTPAssetStore> log)
    {
        Guard.NotNull(log, nameof(log));
        Guard.NotNull(options, nameof(options));
        Guard.NotNullOrEmpty(options.Path, nameof(options.Path));

        pool = new FTPClientPool(clientFactory, 1);

        this.options = options;

        this.log = log;
    }

    public async Task InitializeAsync(
        CancellationToken ct)
    {
        var client = await GetClientAsync(ct);
        try
        {
            if (!await client.DirectoryExists(options.Path, ct))
            {
                await client.CreateDirectory(options.Path, ct);
            }
        }
        finally
        {
            pool.Return(client);
        }

        log.LogInformation("Initialized with {path}", options.Path);
    }

    public async Task<long> GetSizeAsync(string fileName,
        CancellationToken ct = default)
    {
        var name = GetFileName(fileName, nameof(fileName));

        var client = await GetClientAsync(ct);
        try
        {
            var size = await client.GetFileSize(name, 0, ct);

            if (size < 0)
            {
                throw new AssetNotFoundException(fileName);
            }

            return size;
        }
        catch (FtpException ex) when (IsNotFound(ex))
        {
            throw new AssetNotFoundException(fileName, ex);
        }
        finally
        {
            pool.Return(client);
        }
    }

    public async Task CopyAsync(string sourceFileName, string targetFileName,
        CancellationToken ct = default)
    {
        var sourceName = GetFileName(sourceFileName, nameof(sourceFileName));
        var targetName = GetFileName(targetFileName, nameof(targetFileName));

        var client = await GetClientAsync(ct);
        try
        {
            var tempPath = Path.Combine(Path.GetTempPath(), Path.GetTempFileName());

            await using (var stream = new FileStream(tempPath, FileMode.Create, FileAccess.ReadWrite, FileShare.None, 4096, FileOptions.DeleteOnClose))
            {
                try
                {
                    var found = await client.DownloadStream(stream, sourceName, token: ct);

                    if (!found)
                    {
                        throw new AssetNotFoundException(sourceFileName);
                    }
                }
                catch (FtpException ex) when (IsNotFound(ex))
                {
                    throw new AssetNotFoundException(sourceFileName, ex);
                }

                await UploadAsync(client, targetName, stream, false, ct);
            }
        }
        finally
        {
            pool.Return(client);
        }
    }

    public async Task DownloadAsync(string fileName, Stream stream, BytesRange range = default,
        CancellationToken ct = default)
    {
        Guard.NotNull(stream, nameof(stream));

        var name = GetFileName(fileName, nameof(fileName));

        var client = await GetClientAsync(ct);
        try
        {
            await using (var ftpStream = await client.OpenRead(name, FtpDataType.Binary, range.From ?? 0, true, ct))
            {
                await ftpStream.CopyToAsync(stream, range, ct, false);
            }
        }
        catch (FtpException ex) when (IsNotFound(ex))
        {
            throw new AssetNotFoundException(fileName, ex);
        }
        finally
        {
            pool.Return(client);
        }
    }

    public async Task<long> UploadAsync(string fileName, Stream stream, bool overwrite = false,
        CancellationToken ct = default)
    {
        Guard.NotNull(stream, nameof(stream));

        var name = GetFileName(fileName, nameof(fileName));

        var client = await GetClientAsync(ct);
        try
        {
            await UploadAsync(client, name, stream, overwrite, ct);

            return -1;
        }
        finally
        {
            pool.Return(client);
        }
    }

    private static async Task UploadAsync(IAsyncFtpClient client, string fileName, Stream stream, bool overwrite,
        CancellationToken ct)
    {
        if (!overwrite && await client.FileExists(fileName, ct))
        {
            throw new AssetAlreadyExistsException(fileName);
        }

        var mode = overwrite ? FtpRemoteExists.Overwrite : FtpRemoteExists.Skip;

        await client.UploadStream(stream, fileName, mode, true, null, ct);
    }

    public async Task DeleteByPrefixAsync(string prefix,
        CancellationToken ct = default)
    {
        var name = GetFileName(prefix, nameof(prefix));

        var client = await GetClientAsync(ct);
        try
        {
            await client.DeleteDirectory(name, ct);
        }
        catch (FtpException ex)
        {
            if (!IsNotFound(ex))
            {
                throw;
            }
        }
        finally
        {
            pool.Return(client);
        }
    }

    public async Task DeleteAsync(string fileName,
        CancellationToken ct = default)
    {
        var name = GetFileName(fileName, nameof(fileName));

        var client = await GetClientAsync(ct);
        try
        {
            await client.DeleteFile(name, ct);
        }
        catch (FtpException ex)
        {
            if (!IsNotFound(ex))
            {
                throw;
            }
        }
        finally
        {
            pool.Return(client);
        }
    }

    private static string GetFileName(string fileName, string parameterName)
    {
        Guard.NotNullOrEmpty(fileName, parameterName);

        return fileName.Replace("\\", "/", StringComparison.Ordinal);
    }

    private async Task<IAsyncFtpClient> GetClientAsync(
        CancellationToken ct)
    {
        var (client, isNew) = await pool.GetClientAsync(ct);
        try
        {
            if (!client.IsConnected)
            {
                await client.AutoConnect(ct);
            }

            if (isNew)
            {
                await client.SetWorkingDirectory(options.Path, ct);
            }

            return client;
        }
        catch
        {
            pool.Return(client);
            throw;
        }
    }

    private static bool IsNotFound(Exception exception)
    {
        if (exception is FtpCommandException command)
        {
            return command.CompletionCode == "550";
        }

        return exception.InnerException != null && IsNotFound(exception.InnerException);
    }
}
