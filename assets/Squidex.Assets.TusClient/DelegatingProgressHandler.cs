// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

namespace Squidex.Assets;

public sealed class DelegatingProgressHandler : IProgressHandler
{
    internal static readonly DelegatingProgressHandler Instance = new DelegatingProgressHandler();

    public Func<UploadProgressEvent, CancellationToken, Task>? OnProgressAsync { get; set; }

    public Func<UploadCreatedEvent, CancellationToken, Task>? OnCreatedAsync { get; set; }

    public Func<UploadCompletedEvent, CancellationToken, Task>? OnCompletedAsync { get; set; }

    public Func<UploadExceptionEvent, CancellationToken, Task>? OnFailedAsync { get; set; }

    async Task IProgressHandler.OnProgressAsync(UploadProgressEvent @event,
        CancellationToken ct)
    {
        var handler = OnProgressAsync;

        if (handler != null)
        {
            await handler(@event, ct);
        }
    }

    async Task IProgressHandler.OnCreatedAsync(UploadCreatedEvent @event,
        CancellationToken ct)
    {
        var handler = OnCreatedAsync;

        if (handler != null)
        {
            await handler(@event, ct);
        }
    }

    async Task IProgressHandler.OnCompletedAsync(UploadCompletedEvent @event,
        CancellationToken ct)
    {
        var handler = OnCompletedAsync;

        if (handler != null)
        {
            await handler(@event, ct);
        }
    }

    async Task IProgressHandler.OnFailedAsync(UploadExceptionEvent @event,
        CancellationToken ct)
    {
        var handler = OnFailedAsync;

        if (handler != null)
        {
            await handler(@event, ct);
        }
    }
}
