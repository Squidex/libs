// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

namespace Squidex.Flows.Steps.Utils;

public static class HttpExtensions
{
    public static async Task<(string Response, string Dump)> SendAsync(this HttpClient client,
        FlowExecutionContext executionContext,
        HttpRequestMessage request,
        string? requestBody = null,
        CancellationToken ct = default)
    {
        HttpResponseMessage? response = null;
        try
        {
            response = await client.SendAsync(request, ct);

            var responseString = await response.Content.ReadAsStringAsync(ct);
            var requestDump = HttpDumpFormatter.BuildDump(request, response, requestBody, responseString);

            if (!response.IsSuccessStatusCode)
            {
                executionContext.Log("Http request failed", requestDump);
                throw new HttpRequestException($"Response code does not indicate success: {(int)response.StatusCode} ({response.StatusCode}).");
            }

            return (responseString, requestDump);
        }
        catch (Exception ex)
        {
            var requestDump = HttpDumpFormatter.BuildDump(request, response, requestBody, ex.ToString());

            executionContext.Log("Http request failed", requestDump);
            throw;
        }
    }
}
