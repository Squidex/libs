// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

using System.Globalization;

namespace Squidex.Text;

/// <summary>
/// Calculates the boundary of words.
/// </summary>
public static class WordBoundary
{
    private static readonly HashSet<char> NewLines = new HashSet<char>
    {
        '\u000A', // Line Feed
        '\u000B', // Vertical Tab
        '\u000C', // Form Feed
        '\u000D', // Carriage Return
        '\u0085', // Next Line
        '\u2028', // Line Separator
        '\u2029', // Paragraph Separator
    };

    private static readonly HashSet<char> MidLetter = new HashSet<char>
    {
        ':',
        '.',
        '’',
        '\'',
        '\u00B7', // Middot
        '\u05F4', // Hebrew Punctuation
        '\u2027', // Hyphenation Point
        '\uFE13', // Presentation form for vertical colon
        '\uFE55', // Small Colon
        '\uFF1A', // Full Width Colon
    };

    /// <summary>
    /// Checks if the given char in string is the end of a word.
    /// </summary>
    /// <param name="value">The string.</param>
    /// <param name="index">The index withing the string.</param>
    /// <returns>
    /// True if the string is the end of a word.
    /// </returns>
    public static bool IsBoundary(ReadOnlySpan<char> value, int index)
    {
        if (value.Length == 0)
        {
            return true;
        }

        if (index < 0 || index > value.Length - 1)
        {
            return false;
        }

        var current = value[index];

        if (index == value.Length - 1)
        {
            return true;
        }

        var next = value[index + 1];

        // Don't break inside CRLF.
        if (current == '\r' && next == '\n')
        {
            return false;
        }

        if (index < value.Length - 2)
        {
            var nextNext = value[index + 2];

            // Don't break letters across certain punctuation.
            if (IsNonCJKLetter(current) && IsMidLetter(next) && IsNonCJKLetter(nextNext))
            {
                return false;
            }

            // Don't break inside number sequences like "3.2" or "3,456.789".
            if (char.IsNumber(current) && IsMidNumeric(next) && char.IsNumber(nextNext))
            {
                return false;
            }
        }

        if (index > 0)
        {
            var prev = value[index - 1];

            // Don't break letters across certain punctuation.
            if (IsMidLetter(current) && IsNonCJKLetter(next) && IsNonCJKLetter(prev))
            {
                return false;
            }

            if (IsMidNumeric(current) && char.IsNumber(prev) && char.IsNumber(next))
            {
                return false;
            }
        }

        // Don't break between most letters.
        if (IsNumberOrLetter(current) && IsNumberOrLetter(next))
        {
            return false;
        }

        // Break before newlines (including CR and LF).
        if (IsNewLine(current))
        {
            return true;
        }

        // Break after newlines (including CR and LF).
        if (IsNewLine(next))
        {
            return true;
        }

        // Don't break between Katakana characters.
        if (IsKatakana(current) && IsKatakana(next))
        {
            return false;
        }

        return true;
    }

    private static bool IsNewLine(char c)
    {
        return NewLines.Contains(c);
    }

    private static bool IsMidLetter(char c)
    {
        return MidLetter.Contains(c);
    }

    private static bool IsNumberOrLetter(char c)
    {
        return char.IsNumber(c) || IsNonCJKLetter(c);
    }

    private static bool IsMidNumeric(char c)
    {
        return char.IsPunctuation(c) || IsMathSymbol(c);
    }

    private static bool IsMathSymbol(char c)
    {
        return char.GetUnicodeCategory(c) == UnicodeCategory.MathSymbol;
    }

    private static bool IsNonCJKLetter(char c)
    {
        return char.IsLetter(c) && char.GetUnicodeCategory(c) != UnicodeCategory.OtherLetter;
    }

    private static bool IsKatakana(char c)
    {
        return c >= '\u30A0' && c <= '\u30FF';
    }
}
