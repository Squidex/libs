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
    private readonly Uri uploadUri = new Uri("http://localhost:5010/files/controller");
    private string? fileId;

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

            var numWrites = 0;
            var pausingStream = new PauseStream(file.Stream, 0.25);
            var pausingFile = new UploadFile(pausingStream, file.FileName, file.ContentType);
            var completed = false;

            await using (pausingFile.Stream)
            {
                using var cts = CancellationTokenSource.CreateLinkedTokenSource(HttpContext.RequestAborted);

                cts.CancelAfter(10_000);

                while (!completed)
                {
                    cts.Token.ThrowIfCancellationRequested();

                    pausingStream.Reset();

                    await httpClient.UploadWithProgressAsync(uploadUri, pausingFile, new UploadOptions
                    {
                        ProgressHandler = new DelegatingProgressHandler
                        {
                            OnCreatedAsync = (@event, _) =>
                            {
                                fileId = @event.FileId;
                                return Task.CompletedTask;
                            },
                            OnCompletedAsync = (@event, _) =>
                            {
                                completed = true;
                                return Task.CompletedTask;
                            }
                        },
                        FileId = fileId
                    }, cts.Token);

                    numWrites++;
                }
            }

            return Ok(new { numWrites });
        }
    }

    [Route("files/controller/{**catchAll}")]
    public async Task<IActionResult> Tus()
    {
        var (result, file) = await runner.InvokeAsync(HttpContext, Url.Action(null, new { catchAll = (string?)null })!);

        if (file == null)
        {
            return result;
        }

        await using var fileStream = file.OpenRead();

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

    public class PauseStream : DelegateStream
    {
        private readonly double pauseAfter = 1;
        private int totalRead;

        public PauseStream(Stream innerStream, double pauseAfter)
            : base(innerStream)
        {
            this.pauseAfter = pauseAfter;
        }

        public void Reset()
        {
            totalRead = 0;
        }

        public override async ValueTask<int> ReadAsync(Memory<byte> buffer,
            CancellationToken cancellationToken = default)
        {
            if (Position >= Length)
            {
                return 0;
            }

            if (totalRead >= Length * pauseAfter)
            {
                throw new InvalidOperationException();
            }

            var bytesRead = await base.ReadAsync(buffer, cancellationToken);

            totalRead += bytesRead;

            return bytesRead;
        }
    }
}
