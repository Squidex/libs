// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

namespace Squidex.Flows.Steps.Utils;

public static class HttpExtensions
{
    public static async Task OneWayRequestAsync(this HttpClient client, HttpRequestMessage request, Action<string> logger, string? requestBody = null,
        CancellationToken ct = default)
    {
        HttpResponseMessage? response = null;
        try
        {
            response = await client.SendAsync(request, ct);

            var responseString = await response.Content.ReadAsStringAsync(ct);

            var requestDump = HttpDumpFormatter.BuildDump(request, response, requestBody, responseString);
            logger(requestDump);

            if (!response.IsSuccessStatusCode)
            {
                logger(requestDump);
                throw new HttpRequestException($"Response code does not indicate success: {(int)response.StatusCode} ({response.StatusCode}).");
            }
        }
        catch (Exception ex)
        {
            var requestDump = HttpDumpFormatter.BuildDump(request, response, requestBody, ex.ToString());
            logger(requestDump);
            throw;
        }
    }
}
