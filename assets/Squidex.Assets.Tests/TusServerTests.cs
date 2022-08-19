﻿// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

using Xunit;

#pragma warning disable SA1300 // Element should begin with upper-case letter

namespace Squidex.Assets
{
    public class TusServerTests : IClassFixture<TusServerFixture>
    {
        private string fileId;

        public TusServerFixture _ { get; }

        public TusServerTests(TusServerFixture fixture)
        {
            _ = fixture;

            TusServerFixture.Files.Clear();
        }

        [Fact]
        public async Task Should_report_exception_on_error()
        {
            var image = GetImage("logo.png");

            var reportedException = (Exception?)null;

            await _.Client.UploadWithProgressAsync(new Uri("/404", UriKind.Relative), image,
                new UploadOptions
                {
                    ProgressHandler = new DelegatingProgressHandler
                    {
                        OnFailedAsync = (@event, _) =>
                        {
                            reportedException = @event.Exception;

                            fileId = @event.FileId;
                            return Task.CompletedTask;
                        }
                    }
                });

            Assert.IsType<HttpRequestException>(reportedException);
        }

        [Theory]
        [InlineData("/files/middleware")]
        [InlineData("/files/controller")]
        public async Task Should_upload_file_at_once(string url)
        {
            var image = GetImage("logo.png");

            await _.Client.UploadWithProgressAsync(new Uri(url, UriKind.Relative), image);

            await HasFileAsync(image);
        }

        [Theory]
        [InlineData("/files/middleware")]
        [InlineData("/files/controller")]
        public async Task Should_upload_files_with_events(string url)
        {
            var image = GetImage("logo.bmp");

            var reportedProgress = new List<int>();
            var reportedCompleted = false;

            await _.Client.UploadWithProgressAsync(new Uri(url, UriKind.Relative), image,
                new UploadOptions
                {
                    ProgressHandler = new DelegatingProgressHandler
                    {
                        OnProgressAsync = (@event, _) =>
                        {
                            reportedProgress.Add(@event.Progress);

                            fileId = @event.FileId;
                            return Task.CompletedTask;
                        },
                        OnCompletedAsync = (@event, _) =>
                        {
                            reportedCompleted = true;

                            fileId = @event.FileId;
                            return Task.CompletedTask;
                        }
                    }
                });

            Assert.True(reportedCompleted);
            Assert.Equal(Enumerable.Range(1, 100).ToArray(), reportedProgress.ToArray());
            Assert.NotNull(fileId);

            await HasFileAsync(image);
        }

        [Theory]
        [InlineData("/files/middleware")]
        [InlineData("/files/controller")]
        public async Task Should_upload_file_in_chunks(string url)
        {
            var image = GetImage("logo.bmp");

            var pausingStream = new PauseStream(image.Stream, 0.25);
            var pausingFile = new UploadFile(pausingStream, image.FileName, image.ContentType);

            var uploads = 0;
            var uploaded = false;

            while (pausingStream.Position < pausingStream.Length)
            {
                await _.Client.UploadWithProgressAsync(new Uri(url, UriKind.Relative), pausingFile,
                    new UploadOptions
                    {
                        ProgressHandler = new DelegatingProgressHandler
                        {
                            OnProgressAsync = (@event, _) =>
                            {
                                fileId = @event.FileId;
                                return Task.CompletedTask;
                            },
                            OnCompletedAsync = (@event, _) =>
                            {
                                uploaded = true;
                                return Task.CompletedTask;
                            }
                        },
                        FileId = fileId
                    });

                pausingStream.Reset();

                uploads++;
            }

            Assert.Equal(4, uploads);
            Assert.True(uploaded);

            await HasFileAsync(image);
        }

        [Theory]
        [InlineData("/files/middleware")]
        [InlineData("/files/controller")]
        public async Task Should_not_use_new_file_id_id_nothing_written_first(string url)
        {
            var image = GetImage("logo.bmp");

            var pausingStream = new PauseStream(image.Stream, 0);
            var pausingFile = new UploadFile(pausingStream, image.FileName, image.ContentType);
            var fileIds = new HashSet<string>();

            var numReads = 0;

            while (pausingStream.Position < pausingStream.Length)
            {
                await _.Client.UploadWithProgressAsync(new Uri(url, UriKind.Relative), pausingFile,
                    new UploadOptions
                    {
                        ProgressHandler = new DelegatingProgressHandler
                        {
                            OnCreatedAsync = (@event, _) =>
                            {
                                fileId = @event.FileId;
                                fileIds.Add(@event.FileId);
                                return Task.CompletedTask;
                            }
                        },
                        FileId = fileId
                    });

                pausingStream.Reset(0.25);

                numReads++;
            }

            Assert.Equal(5, numReads);
            Assert.Single(fileIds);

            await HasFileAsync(image);
        }

        private static async Task HasFileAsync(UploadFile file)
        {
            var cts = new CancellationTokenSource(10000);

            try
            {
                while (!cts.IsCancellationRequested)
                {
                    if (TusServerFixture.Files.Any(x => x.FileName == file.FileName && x.MimeType == file.ContentType))
                    {
                        break;
                    }

                    await Task.Delay(100, cts.Token);
                }
            }
            catch (OperationCanceledException)
            {
            }

            Assert.Contains(TusServerFixture.Files, x => x.FileName == file.FileName && x.MimeType == file.ContentType);
        }

        private UploadFile GetImage(string fileName)
        {
            var name = $"Squidex.Assets.Images.{fileName}";

            return new UploadFile(GetType().Assembly.GetManifestResourceStream(name)!, name, GetMimeType(fileName));
        }

        private static string GetMimeType(string fileName)
        {
            var extension = fileName.Split('.')[^1];

            var mimeType = $"image/{extension}";

            if (string.Equals(extension, "tga", StringComparison.OrdinalIgnoreCase))
            {
                mimeType = "image/x-tga";
            }

            return mimeType;
        }

        public class PauseStream : DelegateStream
        {
            private double pauseAfter = 1;
            private int totalRead;

            public PauseStream(Stream innerStream, double pauseAfter)
                : base(innerStream)
            {
                this.pauseAfter = pauseAfter;
            }

            public void Reset(double? newPauseAfter = null)
            {
                totalRead = 0;

                if (newPauseAfter.HasValue)
                {
                    pauseAfter = newPauseAfter.Value;
                }
            }

            public override async ValueTask<int> ReadAsync(Memory<byte> buffer,
                CancellationToken cancellationToken = default)
            {
                if (totalRead >= Length * pauseAfter)
                {
                    return 0;
                }

                var bytesRead = await base.ReadAsync(buffer, cancellationToken);

                totalRead += bytesRead;

                return bytesRead;
            }
        }
    }
}
