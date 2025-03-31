// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

using HeyRed.Mime;

namespace Squidex.Assets.TusClient;

public sealed class UploadFile(Stream stream, string fileName, string contentType, long contentLength)
{
    public Stream Stream { get; } = stream;

    public string FileName { get; } = fileName;

    public string ContentType { get; } = contentType;

    public long ContentLength { get; } = contentLength;

    public static UploadFile FromFile(FileInfo fileInfo, string? mimeType = null)
    {
        if (string.IsNullOrEmpty(mimeType))
        {
            mimeType = MimeTypesMap.GetMimeType(fileInfo.Name);
        }

        return new UploadFile(fileInfo.OpenRead(), fileInfo.Name, mimeType!, fileInfo.Length);
    }

    public static UploadFile FromPath(string path, string? mimeType = null)
    {
        return FromFile(new FileInfo(path), mimeType);
    }
}
