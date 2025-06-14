﻿// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

using System.Diagnostics.CodeAnalysis;
using FluentFTP;
using FluentFTP.Exceptions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Squidex.Hosting;

namespace Squidex.Assets.FTP;

[ExcludeFromCodeCoverage]
public sealed class FTPAssetStore(IOptions<FTPAssetOptions> options, ILogger<FTPAssetStore> log) : IAssetStore, IInitializable
{
    private readonly FTPClientPool pool = new FTPClientPool(
            () => new AsyncFtpClient(
                options.Value.ServerHost,
                options.Value.Username,
                options.Value.Password,
                options.Value.ServerPort), 1);
    private readonly FTPAssetOptions options = options.Value;

    public async Task InitializeAsync(
        CancellationToken ct)
    {
        var client = await GetClientAsync(ct);
        try
        {
            if (options.CreateFolder && !await client.DirectoryExists(options.Path, ct))
            {
                await client.CreateDirectory(options.Path, ct);
            }

            await this.UploadTestAssetAsync(ct);
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
            await using (var tempStream = TempHelper.GetTempStream())
            {
                try
                {
                    var found = await client.DownloadStream(tempStream, sourceName, token: ct);

                    if (!found)
                    {
                        throw new AssetNotFoundException(sourceFileName);
                    }
                }
                catch (FtpException ex) when (IsNotFound(ex))
                {
                    throw new AssetNotFoundException(sourceFileName, ex);
                }

                await UploadAsync(client, targetName, tempStream, false, ct);
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
        ArgumentNullException.ThrowIfNull(stream);

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
        ArgumentNullException.ThrowIfNull(stream);

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
        ArgumentException.ThrowIfNullOrWhiteSpace(fileName, parameterName);

        return FilePathHelper.EnsureThatPathIsChildOf(fileName.Replace('\\', '/'), "./");
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
