// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

using System.Text;
using Squidex.Flows.Steps.Utils;

namespace Squidex.Flows.Steps;

public sealed partial class WebhookStep
{
    public string? Signature { get; set; }

    public Dictionary<string, string>? ParsedHeaders { get; set; }

    public ValueTask PrepareAsync(FlowContext context, FlowExecutionContext executionContext,
        CancellationToken ct)
    {
        ParsedHeaders = ParseHeaders(Headers);

        Signature = $"{Payload}{SharedSecret}".ToSha256Base64();
        return default;
    }

    private static Dictionary<string, string>? ParseHeaders(string? headers)
    {
        if (string.IsNullOrWhiteSpace(headers))
        {
            return null;
        }

        var headersDictionary = new Dictionary<string, string>();

        var lines = headers.Split('\n');

        foreach (var line in lines)
        {
            var indexEqual = line.IndexOf('=', StringComparison.Ordinal);

            if (indexEqual > 0 && indexEqual < line.Length - 1)
            {
                var headerKey = line[..indexEqual];
                var headerValue = line[(indexEqual + 1)..];
                headersDictionary[headerKey] = headerValue!;
            }
        }

        return headersDictionary;
    }

    public async ValueTask<FlowStepResult> ExecuteAsync(FlowContext context, FlowExecutionContext executionContext,
        CancellationToken ct)
    {
        var method = HttpMethod.Post;
        switch (Method)
        {
            case WebhookMethod.PUT:
                method = HttpMethod.Put;
                break;
            case WebhookMethod.GET:
                method = HttpMethod.Get;
                break;
            case WebhookMethod.DELETE:
                method = HttpMethod.Delete;
                break;
            case WebhookMethod.PATCH:
                method = HttpMethod.Patch;
                break;
        }

        if (executionContext.IsSimulation)
        {
            executionContext.Log($"{method} {Uri}");
            return FlowStepResult.Next();
        }

        var httpClient = executionContext.Resolve<IHttpClientFactory>().CreateClient("FlowClient");

        var request = new HttpRequestMessage(method, Uri);

        if (!string.IsNullOrEmpty(Payload) && Method != WebhookMethod.GET)
        {
            var mediaType = PayloadType;
            if (string.IsNullOrEmpty(mediaType))
            {
                mediaType = "application/json";
            }

            request.Content = new StringContent(Payload, Encoding.UTF8, mediaType);
        }

        if (ParsedHeaders != null)
        {
            foreach (var (key, value) in ParsedHeaders)
            {
                request.Headers.TryAddWithoutValidation(key, value);
            }
        }

        if (!string.IsNullOrWhiteSpace(Signature))
        {
            request.Headers.Add("X-Signature", Signature);
        }

        await httpClient.OneWayRequestAsync(request, executionContext.Log, Payload, ct);
        return FlowStepResult.Next();
    }
}
