// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

using Microsoft.AspNetCore.Mvc;
using Squidex.Assets;

namespace TusTestServer.Controller;

public class TusController : ControllerBase
{
    private readonly AssetTusRunner runner;
    private readonly Uri uploadUri = new Uri("http://localhost:4000/files/controller");

    public TusController(AssetTusRunner runner)
    {
        this.runner = runner;
    }

    [Route("/upload")]
    public async Task<IActionResult> UploadAsync()
    {
        using (var httpClient = new HttpClient())
        {
            var file = UploadFile.FromPath("wwwroot/LargeImage.jpg");

            var pausingStream = new PauseStream(file.Stream, 0.25);
            var pausingFile = new UploadFile(pausingStream, file.FileName, file.ContentType, file.ContentLength);
            var progressHandler = new ProgressHandler();

            await using (pausingFile.Stream)
            {
                using var cts = CancellationTokenSource.CreateLinkedTokenSource(HttpContext.RequestAborted);

                cts.CancelAfter(10_000_000);

                while (!progressHandler.IsCompleted && progressHandler.Exception == null)
                {
                    cts.Token.ThrowIfCancellationRequested();

                    progressHandler.Reset();

                    await httpClient.UploadWithProgressAsync(uploadUri, pausingFile, progressHandler.AsOptions(), cts.Token);
                    pausingStream.Reset();
                }
            }

            return Ok(progressHandler);
        }
    }

#pragma warning disable ASP0018 // Unused route parameter
    [Route("files/controller/{**catchAll}")]
#pragma warning restore ASP0018 // Unused route parameter
    public async Task<IActionResult> Tus()
    {
        var (result, file) = await runner.InvokeAsync(HttpContext, Url.Action(null, new { catchAll = (string?)null })!);

        if (file == null)
        {
            return result;
        }

        await using var fileStream = await file.OpenReadAsync(HttpContext.RequestAborted);

        var name = file.FileName;

        if (string.IsNullOrWhiteSpace(name))
        {
            name = Guid.NewGuid().ToString();
        }

        Directory.CreateDirectory("uploads");

        await using (var stream = new FileStream($"uploads/{name}", FileMode.Create))
        {
            await fileStream.CopyToAsync(stream, HttpContext.RequestAborted);
        }

        return Ok(new { json = "Test" });
    }

    public class ProgressHandler : IProgressHandler
    {
        public string FileId { get; private set; }

        public List<int> Progress { get; } = [];

        public List<int> Uploads { get; } = [];

        public Exception? Exception { get; private set; }

        public bool IsCompleted { get; set; }

        public UploadOptions AsOptions()
        {
            return new UploadOptions { ProgressHandler = this, FileId = FileId };
        }

        public void Reset()
        {
            Uploads.Add(Progress.LastOrDefault());

            Exception = null;
        }

        public Task OnCompletedAsync(UploadCompletedEvent @event,
            CancellationToken ct)
        {
            IsCompleted = true;
            return Task.CompletedTask;
        }

        public Task OnCreatedAsync(UploadCreatedEvent @event,
            CancellationToken ct)
        {
            FileId = @event.FileId;
            return Task.CompletedTask;
        }

        public Task OnProgressAsync(UploadProgressEvent @event,
            CancellationToken ct)
        {
            Progress.Add(@event.Progress);
            return Task.CompletedTask;
        }

        public Task OnFailedAsync(UploadExceptionEvent @event,
            CancellationToken ct)
        {
            Exception = @event.Exception;
            return Task.CompletedTask;
        }
    }

    public class PauseStream : DelegateStream
    {
        private readonly int maxLength;
        private long totalRead;
        private long totalRemaining;
        private long seekStart;

        public override long Length
        {
            get => Math.Min(maxLength, totalRemaining);
        }

        public override long Position
        {
            get => base.Position - seekStart;
            set => throw new NotSupportedException();
        }

        public PauseStream(Stream innerStream, double pauseAfter)
            : base(innerStream)
        {
            maxLength = (int)Math.Floor(innerStream.Length * pauseAfter) + 1;

            totalRemaining = innerStream.Length;
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            var position = seekStart = base.Seek(offset, origin);

            totalRemaining = base.Length - position;

            return position;
        }

        public void Reset()
        {
            totalRead = 0;
        }

        public override async ValueTask<int> ReadAsync(Memory<byte> buffer,
            CancellationToken cancellationToken = default)
        {
            var remaining = Length - totalRead;

            if (remaining <= 0)
            {
                return 0;
            }

            if (remaining < buffer.Length)
            {
                buffer = buffer[.. (int)remaining];
            }

            var bytesRead = await base.ReadAsync(buffer, cancellationToken);

            totalRead += bytesRead;

            return bytesRead;
        }
    }
}
