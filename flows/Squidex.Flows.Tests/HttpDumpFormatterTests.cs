// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

using System.Net;
using System.Net.Http.Headers;
using System.Text;
using Squidex.Flows.Steps.Utils;

namespace Squidex.Flows;

public class HttpDumpFormatterTests
{
    [Fact]
    public async Task Should_format_dump_without_response()
    {
        var httpRequest = CreateRequest();

        var dump = HttpDumpFormatter.BuildDump(httpRequest, null, null, null, TimeSpan.FromMinutes(1), true);

        await Verify(dump);
    }

    [Fact]
    public async Task Should_format_dump_without_content()
    {
        var httpRequest = CreateRequest();
        var httpResponse = CreateResponse();

        var dump = HttpDumpFormatter.BuildDump(httpRequest, httpResponse, null, null, TimeSpan.FromMinutes(1), false);

        await Verify(dump);
    }

    [Fact]
    public async Task Should_format_dump_with_content_without_timeout()
    {
        var httpRequest = CreateRequest(new StringContent("Hello Squidex", Encoding.UTF8, "text/plain"));
        var httpResponse = CreateResponse(new StringContent("Hello Back", Encoding.UTF8, "text/plain"));

        var dump = HttpDumpFormatter.BuildDump(httpRequest, httpResponse, "Hello Squidex", "Hello Back", TimeSpan.FromMinutes(1), false);

        await Verify(dump);
    }

    private static HttpRequestMessage CreateRequest(HttpContent? content = null)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, new Uri("https://cloud.squidex.io"));
        request.Headers.UserAgent.Add(new ProductInfoHeaderValue("Squidex", "1.0"));
        request.Headers.AcceptLanguage.Add(new StringWithQualityHeaderValue("de"));
        request.Headers.AcceptLanguage.Add(new StringWithQualityHeaderValue("en"));
        request.Headers.AcceptEncoding.Add(new StringWithQualityHeaderValue("UTF-8"));
        request.Content = content;

        return request;
    }

    private static HttpResponseMessage CreateResponse(HttpContent? content = null)
    {
        var response = new HttpResponseMessage(HttpStatusCode.OK);
        response.Headers.TransferEncoding.Add(new TransferCodingHeaderValue("UTF-8"));
        response.Headers.Trailer.Add("Expires");
        response.Content = content;

        return response;
    }
}
