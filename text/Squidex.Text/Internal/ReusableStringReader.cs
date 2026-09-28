// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

namespace Squidex.Text.Internal;

// The string reader from the framework cannot be reused for another string.
internal sealed class ReusableStringReader : TextReader
{
    private string text = string.Empty;
    private int position;

    public void Reset(string value)
    {
        text = value;
        position = 0;
    }

    public override int Peek()
    {
        return position < text.Length ? text[position] : -1;
    }

    public override int Read()
    {
        return position < text.Length ? text[position++] : -1;
    }

    public override int Read(char[] buffer, int index, int count)
    {
        return Read(buffer.AsSpan(index, count));
    }

    public override int Read(Span<char> buffer)
    {
        var count = Math.Min(buffer.Length, text.Length - position);
        if (count <= 0)
        {
            return 0;
        }

        text.AsSpan(position, count).CopyTo(buffer);
        position += count;

        return count;
    }

    public override string ReadToEnd()
    {
        var result = text[position..];

        position = text.Length;
        return result;
    }
}
