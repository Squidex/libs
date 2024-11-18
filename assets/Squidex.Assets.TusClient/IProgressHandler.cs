// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

#pragma warning disable MA0048 // File name must match type name

namespace Squidex.Assets;

public abstract class UploadEvent(string fileId)
{
    public string FileId { get; } = fileId;
}

public sealed class UploadProgressEvent(string fileId, int progress, long bytesWritten, long bytesTotal) : UploadEvent(fileId)
{
    public int Progress { get; } = progress;

    public long BytesWritten { get; } = bytesWritten;

    public long BytesTotal { get; } = bytesTotal;
}

public sealed class UploadCompletedEvent(string fileId, HttpResponseMessage response) : UploadEvent(fileId)
{
    public HttpResponseMessage Response { get; } = response;
}

public sealed class UploadCreatedEvent(string fileId) : UploadEvent(fileId)
{
}

public sealed class UploadExceptionEvent(string fileId, Exception exception, HttpResponseMessage? response) : UploadEvent(fileId)
{
    public HttpResponseMessage? Response { get; } = response;

    public Exception Exception { get; } = exception;
}

public interface IProgressHandler
{
    Task OnProgressAsync(UploadProgressEvent @event,
        CancellationToken ct);

    Task OnCreatedAsync(UploadCreatedEvent @event,
        CancellationToken ct);

    Task OnCompletedAsync(UploadCompletedEvent @event,
        CancellationToken ct);

    Task OnFailedAsync(UploadExceptionEvent @event,
        CancellationToken ct);
}
