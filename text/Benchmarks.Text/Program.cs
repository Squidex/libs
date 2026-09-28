// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

using System.Text;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;
using Squidex.Text;

#pragma warning disable MA0048 // File name must match type name

namespace Benchmarks.Text;

[SimpleJob]
[MemoryDiagnoser]
public class HtmlBenchmarks
{
    private string html = string.Empty;
    private string svg = string.Empty;

    [Params(1, 100)]
    public int Paragraphs { get; set; }

    [GlobalSetup]
    public void Prepare()
    {
        var sb = new StringBuilder();

        for (var i = 0; i < Paragraphs; i++)
        {
            sb.Append("<h2>Heading ").Append(i).Append("</h2>");
            sb.Append("<p>Lorem ipsum <strong>dolor</strong> sit amet, <a href=\"https://squidex.io\">consectetur</a> adipiscing elit &amp; more.</p>");
            sb.Append("<ul><li>First item</li><li>Second <em>item</em></li></ul>");
            sb.Append("<script>var x = '<b>ignored</b>';</script>");
        }

        html = sb.ToString();

        svg =
            "<svg xmlns=\"http://www.w3.org/2000/svg\" width=\"100\" height=\"100\" viewBox=\"0 0 100 100\">" +
                string.Concat(Enumerable.Repeat("<circle cx=\"50\" cy=\"50\" r=\"40\" stroke=\"green\" stroke-width=\"4\" fill=\"yellow\" />", Paragraphs)) +
            "</svg>";
    }

    [Benchmark]
    public string Html2Text()
    {
        return html.Html2Text();
    }

    [Benchmark]
    public object SvgMetadata()
    {
        return svg.GetSvgMetadata();
    }

    [Benchmark]
    public object SvgErrors()
    {
        return svg.GetSvgErrors();
    }
}

public static class Program
{
    public static void Main(string[] args)
    {
        BenchmarkSwitcher.FromAssembly(typeof(Program).Assembly).Run(args);
    }
}
