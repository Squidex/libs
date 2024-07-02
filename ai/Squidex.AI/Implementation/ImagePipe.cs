// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

using System.Runtime.CompilerServices;
using System.Text;

namespace Squidex.AI.Implementation;

public sealed class ImagePipe : IChatPipe
{
    private const string ImageStart = "<IMG>";
    private const string ImageEnd = "</IMG>";
    private const int BufferLength = 5;
    private readonly IImageTool imageGenerator;

    public ImagePipe(IImageTool imageGenerator)
    {
        this.imageGenerator = imageGenerator;
    }

    public async IAsyncEnumerable<InternalChatEvent> StreamAsync(IAsyncEnumerable<InternalChatEvent> source, ChatProviderRequest request,
        [EnumeratorCancellation] CancellationToken ct = default)
    {
        // Use a limited buffer of five elements to search for the image start.
        var chunkBuffer = new Queue<ChunkEvent>(BufferLength);
        var chunkText = new StringBuilder();

        var imageStart = -1;

        // Unfortunately it is not possible to split this into small nature due to the enumeration.
        await foreach (var @event in source.WithCancellation(ct))
        {
            if (@event is ChunkEvent chunk)
            {
                chunkBuffer.Enqueue(chunk);

                if (imageStart >= 0)
                {
                    chunkText.Append(chunk.Content);

                    var bufferText = chunkText.ToString();

                    var imageEnd = bufferText.IndexOf(ImageEnd, StringComparison.Ordinal);
                    if (imageEnd > imageStart)
                    {
                        string? result = null;

                        var description = bufferText[(imageStart + ImageStart.Length)..imageEnd];

                        if (!string.IsNullOrEmpty(description))
                        {
                            var args = new Dictionary<string, ToolValue>
                            {
                                ["query"] = new ToolStringValue(description),
                            };

                            yield return new ToolStartEvent { Tool = imageGenerator, Arguments = args };

                            var toolContext = new ToolContext
                            {
                                Arguments = args,
                                ChatAgent = request.ChatAgent,
                                Context = request.Context,
                                ToolData = request.ToolData
                            };

                            result = await imageGenerator.GenerateAsync(toolContext, ct);

                            yield return new ToolEndEvent { Tool = imageGenerator, Result = result };
                        }

                        var beforeImage = bufferText[..imageStart];

                        // Chunks with only whitespaces are valid.
                        if (!string.IsNullOrEmpty(beforeImage))
                        {
                            yield return new ChunkEvent { Content = beforeImage };
                        }

                        // Empty images are not needed in this case.
                        if (!string.IsNullOrWhiteSpace(result))
                        {
                            yield return new ChunkEvent { Content = result };
                        }

                        var afterImage = bufferText[(imageEnd + ImageEnd.Length)..];

                        // Chunks with only whitespaces are valid.
                        if (!string.IsNullOrEmpty(afterImage))
                        {
                            yield return new ChunkEvent { Content = afterImage };
                        }

                        chunkBuffer.Clear();

                        imageStart = -1;
                    }
                }
                else
                {
                    // Use the image start marker for our state indicator.
                    imageStart = FindImageStart(chunkBuffer, chunkText);

                    if (imageStart < 0)
                    {
                        while (chunkBuffer.Count > BufferLength)
                        {
                            var first = chunkBuffer.Dequeue();
                            yield return first;
                        }
                    }
                }
            }
            else
            {
                imageStart = -1;

                while (chunkBuffer.Count > 0)
                {
                    var first = chunkBuffer.Dequeue();
                    yield return first;
                }

                yield return @event;
            }
        }

        while (chunkBuffer.Count > 0)
        {
            var first = chunkBuffer.Dequeue();
            yield return first;
        }
    }

    private static int FindImageStart(Queue<ChunkEvent> buffer, StringBuilder textBuffer)
    {
        textBuffer.Clear();

        foreach (var previousChunk in buffer)
        {
            textBuffer.Append(previousChunk.Content);
        }

        // Unfortunately we need to calculate the calculate the buffered text with each request.
        var bufferedText = textBuffer.ToString();

        return bufferedText.IndexOf(ImageStart, StringComparison.Ordinal);
    }
}
