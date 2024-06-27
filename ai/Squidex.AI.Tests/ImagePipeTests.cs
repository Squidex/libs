// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

using System.Text;
using FakeItEasy;
using Squidex.AI.Implementation;
using Xunit;

namespace Squidex.AI;

public class ImagePipeTests
{
    private readonly ChatProviderRequest request;
    private readonly IImageTool tool = A.Fake<IImageTool>();
    private readonly ImagePipe sut;

    public ImagePipeTests()
    {
        request = new ChatProviderRequest
        {
            ChatAgent = null!,
            Context = new ChatContext(),
            History = [],
            Tool = null,
            ToolData = [],
            Tools = []
        };

        sut = new ImagePipe(tool);
    }

    [Fact]
    public async Task Should_do_nothing_if_no_marker_found()
    {
        var source = CreateEvents("Hello World, this is just a random text.").ToAsyncEnumerable();

        var resultStream = await sut.StreamAsync(source, request).ToListAsync();
        var resultText = CombineResult(resultStream);

        Assert.Equal("Hello World, this is just a random text.", resultText);
    }

    [Fact]
    public async Task Should_do_nothing_if_no_end_marker_not_found()
    {
        var source = CreateEvents("<IMG>Puppy").ToAsyncEnumerable();

        var resultStream = await sut.StreamAsync(source, request).ToListAsync();
        var resultText = CombineResult(resultStream);

        Assert.Equal("<IMG>Puppy", resultText);
    }

    [Fact]
    public async Task Should_skip_empty_marker()
    {
        var source = CreateEvents("Hello <IMG></IMG>World").ToAsyncEnumerable();

        var resultStream = await sut.StreamAsync(source, request).ToListAsync();
        var resultText = CombineResult(resultStream);

        Assert.Equal("Hello World", resultText);
    }

    [Fact]
    public async Task Should_skip_interrupted_marker()
    {
        var source = new IEnumerable<InternalChatEvent>[]
        {
            CreateEvents("<IMG>Small "),
            [new ToolStartEvent { Tool = null! }],
            CreateEvents("Puppet</IMG>")
        }.SelectMany(x => x).ToAsyncEnumerable();

        var resultStream = await sut.StreamAsync(source, request).ToListAsync();
        var resultText = CombineResult(resultStream);

        Assert.Equal("<IMG>Small Puppet</IMG>", resultText);
    }

    [Fact]
    public async Task Should_replace_image_marker()
    {
        var source = CreateEvents("<IMG>Puppy</IMG>").ToAsyncEnumerable();

        A.CallTo(() => tool.ExecuteAsync(A<ToolContext>._, default))
            .Returns("URL_TO_PUPPY_IMAGE");

        var resultStream = await sut.StreamAsync(source, request).ToListAsync();
        var resultText = CombineResult(resultStream);

        Assert.Equal("URL_TO_PUPPY_IMAGE", resultText);
    }

    [Fact]
    public async Task Should_replace_image_marker_with_text_before()
    {
        var source = CreateEvents("Text Before <IMG>Puppy</IMG> Text After").ToAsyncEnumerable();

        A.CallTo(() => tool.ExecuteAsync(A<ToolContext>._, default))
            .Returns("URL_TO_PUPPY_IMAGE");

        var resultStream = await sut.StreamAsync(source, request).ToListAsync();
        var resultText = CombineResult(resultStream);

        Assert.Equal("Text Before URL_TO_PUPPY_IMAGE Text After", resultText);
    }

    private static string CombineResult(IEnumerable<InternalChatEvent> source)
    {
        var sb = new StringBuilder();

        foreach (var chunk in source.OfType<ChunkEvent>())
        {
            sb.Append(chunk.Content);
        }

        return sb.ToString();
    }

    private static IEnumerable<ChunkEvent> CreateEvents(string source)
    {
        for (var i = 0; i < source.Length; i += 4)
        {
            var length = Math.Min(source.Length - i, 4);

            yield return new ChunkEvent { Content = source.Substring(i, length) };
        }
    }
}
