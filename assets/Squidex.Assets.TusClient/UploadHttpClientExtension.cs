// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

using System.Globalization;
using System.Net;
using System.Text;
using Microsoft.Net.Http.Headers;
using Squidex.Assets.Internal;

#pragma warning disable MA0098 // Use indexer instead of LINQ methods

namespace Squidex.Assets
{
    public static class UploadHttpClientExtension
    {
        private static readonly HttpMethod Patch = new HttpMethod("PATCH");

        private static class TusHeaders
        {
            public const string ContentType = "application/offset+octet-stream";
            public const string TusResumable = "Tus-Resumable";
            public const string TusResumableValue = "1.0.0";
            public const string UploadOffset = "Upload-Offset";
            public const string UploadLength = "Upload-Length";
            public const string UploadMetadata = "Upload-Metadata";
        }

        public static Task UploadWithProgressAsync(this HttpClient httpClient, string uri, UploadFile file, UploadOptions options = default,
            CancellationToken ct = default)
        {
            Guard.NotNullOrEmpty(uri, nameof(uri));
            Guard.NotNull(file, nameof(file));

            return httpClient.UploadWithProgressAsync(new Uri(uri, UriKind.RelativeOrAbsolute), file, options, ct);
        }

        public static async Task UploadWithProgressAsync(this HttpClient httpClient, Uri uri, UploadFile file, UploadOptions options = default,
            CancellationToken ct = default)
        {
            Guard.NotNull(uri, nameof(uri));
            Guard.NotNull(file, nameof(file));

            var handler = options.ProgressHandler ?? DelegatingProgressHandler.Instance;

            HttpResponseMessage? response = null;
            try
            {
                var fileId = options.FileId;
                var isFound = false;
                var totalProgress = 0;
                var totalBytes = file.Stream.Length;
                var bytesWritten = 0L;

                if (!string.IsNullOrWhiteSpace(fileId))
                {
                    (bytesWritten, isFound) = await httpClient.GetUploadProgressCoreAsync(uri, fileId!, ct);

                    if (bytesWritten > 0)
                    {
                        file.Stream.Seek(bytesWritten, SeekOrigin.Begin);
                    }
                }

                if (!isFound || string.IsNullOrWhiteSpace(fileId))
                {
                    fileId = await httpClient.CreateAsync(uri, file, options, ct);

                    await handler.OnCreatedAsync(new UploadCreatedEvent(fileId), ct);
                }

                var content = new ProgressableStreamContent(file.Stream, async bytes =>
                {
                    try
                    {
                        bytesWritten = bytes;

                        if (bytesWritten == totalBytes)
                        {
                            await handler.OnProgressAsync(new UploadProgressEvent(fileId!, 100, totalBytes, totalBytes), ct);
                            return;
                        }

                        var newProgress = (int)Math.Floor(100 * (double)bytesWritten / totalBytes);

                        if (newProgress != totalProgress)
                        {
                            totalProgress = newProgress;

                            await handler.OnProgressAsync(new UploadProgressEvent(fileId!, totalProgress, bytes, totalBytes), ct);
                        }
                    }
                    catch
                    {
                        return;
                    }
                });

                content.Headers.TryAddWithoutValidation(HeaderNames.ContentType, TusHeaders.ContentType);

                var request =
                    new HttpRequestMessage(Patch, GetFileIdUrl(uri, fileId!)) { Content = content }
                        .WithDefaultHeaders()
                        .WithHeader(TusHeaders.UploadOffset, bytesWritten);

                response = await httpClient.SendAsync(request, ct);
                response.EnsureSuccessStatusCode();

                var isCompleted = !response.Headers.Contains(TusHeaders.TusResumable);

                if (!isCompleted)
                {
                    var offset = GetOffset(response);

                    isCompleted = offset == totalBytes;
                }

                if (isCompleted)
                {
                    try
                    {
                        await handler.OnCompletedAsync(new UploadCompletedEvent(fileId!, response), ct);
                    }
                    finally
                    {
                        await httpClient.DeleteUploadAsync(uri, fileId!, default);
                    }
                }
            }
            catch (Exception ex)
            {
                await handler.OnFailedAsync(new UploadExceptionEvent(file.FileName, ex, response), ct);
            }
        }

        public static Task<bool> DeleteUploadAsync(this HttpClient httpClient, string uri, string fileId,
          CancellationToken ct = default)
        {
            Guard.NotNullOrEmpty(uri, nameof(uri));
            Guard.NotNullOrEmpty(fileId, nameof(fileId));

            return httpClient.DeleteUploadAsync(new Uri(uri, UriKind.RelativeOrAbsolute), fileId, ct);
        }

        public static async Task<bool> DeleteUploadAsync(this HttpClient httpClient, Uri uri, string fileId,
            CancellationToken ct = default)
        {
            Guard.NotNull(uri, nameof(uri));
            Guard.NotNullOrEmpty(fileId, nameof(fileId));

            var request =
                new HttpRequestMessage(HttpMethod.Delete, GetFileIdUrl(uri, fileId))
                    .WithDefaultHeaders();

            try
            {
                var response = await httpClient.SendAsync(request, ct);

                return response.IsSuccessStatusCode;
            }
            catch
            {
                return false;
            }
        }

        public static Task<long> GetUploadProgressAsync(this HttpClient httpClient, string uri, string fileId,
            CancellationToken ct)
        {
            Guard.NotNullOrEmpty(uri, nameof(uri));
            Guard.NotNullOrEmpty(fileId, nameof(fileId));

            return httpClient.GetUploadProgressAsync(new Uri(uri, UriKind.RelativeOrAbsolute), fileId, ct);
        }

        public static async Task<long> GetUploadProgressAsync(this HttpClient httpClient, Uri uri, string fileId,
            CancellationToken ct)
        {
            Guard.NotNull(uri, nameof(uri));
            Guard.NotNullOrEmpty(fileId, nameof(fileId));

            var (bytes, _) = await httpClient.GetUploadProgressCoreAsync(uri, fileId, ct);

            return bytes;
        }

        private static async Task<(long, bool)> GetUploadProgressCoreAsync(this HttpClient httpClient, Uri uri, string fileId,
            CancellationToken ct)
        {
            var request =
                new HttpRequestMessage(HttpMethod.Head, GetFileIdUrl(uri, fileId))
                    .WithDefaultHeaders();

            var response = await httpClient.SendAsync(request, ct);

            response.CheckTusResponse();

            if (response.StatusCode == HttpStatusCode.NotFound)
            {
                return (0, false);
            }

            response.EnsureSuccessStatusCode();

            return (GetOffset(response), true);
        }

        private static long GetOffset(HttpResponseMessage response)
        {
            if (!response.Headers.TryGetValues(TusHeaders.UploadOffset, out var offset) || !offset.Any())
            {
                throw new InvalidOperationException("TUS is not supported for this endpoint.");
            }

            if (!long.TryParse(offset.First(), NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsedNumber))
            {
                throw new InvalidOperationException("TUS is not supported for this endpoint.");
            }

            return parsedNumber;
        }

        private static async Task<string> CreateAsync(this HttpClient httpClient, Uri uri, UploadFile file, UploadOptions options,
            CancellationToken ct)
        {
            var metadata = new StringBuilder();
            metadata.Append("FileName ");
            metadata.Append(file.FileName.ToBase64());
            metadata.Append(',');
            metadata.Append("MimeType ");
            metadata.Append(file.ContentType.ToBase64());

            if (options.Metadata != null)
            {
                foreach (var kvp in options.Metadata.Where(x => x.Value != null))
                {
                    metadata.Append(',');
                    metadata.Append(kvp.Key);
                    metadata.Append(' ');
                    metadata.Append(kvp.Value.ToBase64());
                }
            }

            var request =
                new HttpRequestMessage(HttpMethod.Post, uri)
                    .WithDefaultHeaders()
                    .WithHeader(TusHeaders.UploadMetadata, metadata.ToString())
                    .WithHeader(TusHeaders.UploadLength, file.Stream.Length);

            var response = await httpClient.SendAsync(request, ct);

            response.EnsureSuccessStatusCode();
            response.CheckTusResponse();

            if (response.StatusCode != HttpStatusCode.Created)
            {
                throw new HttpRequestException($"Server did not answer with status code 201. Received: {(int)response.StatusCode}.");
            }

            if (!response.Headers.TryGetValues(HeaderNames.Location, out var location) || !location.Any())
            {
                throw new HttpRequestException($"Server did not answer location.");
            }

            var locationValue = location.First().Split(new[] { '/' }, StringSplitOptions.RemoveEmptyEntries);

            return locationValue.Last();
        }

        private static void CheckTusResponse(this HttpResponseMessage response)
        {
            if (!response.Headers.TryGetValues(TusHeaders.TusResumable, out var resumable) || resumable.FirstOrDefault() != TusHeaders.TusResumableValue)
            {
                throw new InvalidOperationException("TUS is not supported for this endpoint.");
            }
        }

        private static HttpRequestMessage WithDefaultHeaders(this HttpRequestMessage message)
        {
            message.Headers.TryAddWithoutValidation(TusHeaders.TusResumable, TusHeaders.TusResumableValue);

            return message;
        }

        private static HttpRequestMessage WithHeader(this HttpRequestMessage message, string key, object value)
        {
            message.Headers.TryAddWithoutValidation(key, Convert.ToString(value, CultureInfo.InvariantCulture));

            return message;
        }

        private static HttpRequestMessage WithHeader(this HttpRequestMessage message, string key, string value)
        {
            message.Headers.TryAddWithoutValidation(key, value);

            return message;
        }

        private static string ToBase64(this string value)
        {
            var bytes = Encoding.UTF8.GetBytes(value);

            return Convert.ToBase64String(bytes);
        }

        private static string GetFileIdUrl(Uri uri, string id)
        {
            var url = uri.ToString();

#if NETCOREAPP3_1_OR_GREATER
            if (!url.EndsWith('/'))
#else
            if (!url.EndsWith("/", StringComparison.Ordinal))
#endif
            {
                url += '/';
            }

            url += Uri.EscapeDataString(id);

            return url;
        }
    }
}
