// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

using HeyRed.Mime;

namespace Squidex.Assets;

public sealed class UploadFile
{
    public Stream Stream { get; }

    public string FileName { get; }

    public string ContentType { get; }

    public UploadFile(Stream stream, string fileName, string contentType)
    {
        Stream = stream;
        FileName = fileName;
        ContentType = contentType;
    }

    public static UploadFile FromFile(FileInfo fileInfo, string? mimeType = null)
    {
        if (string.IsNullOrEmpty(mimeType))
        {
            mimeType = MimeTypesMap.GetMimeType(fileInfo.Name);
        }

        return new UploadFile(fileInfo.OpenRead(), fileInfo.Name, mimeType!);
    }

    public static UploadFile FromPath(string path, string? mimeType = null)
    {
        return FromFile(new FileInfo(path), mimeType);
    }
}
