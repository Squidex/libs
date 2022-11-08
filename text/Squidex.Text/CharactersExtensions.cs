// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

namespace Squidex.Text;

/// <summary>
/// Provides helpers for characters.
/// </summary>
public static class CharactersExtensions
{
    /// <summary>
    /// Counts the number of characters.
    /// </summary>
    /// <param name="value">The text to calculate the characters for.</param>
    /// <param name="withPunctuation">True, to include punctuations.</param>
    /// <returns>
    /// The  number of characters.
    /// </returns>
    public static int CharacterCount(this string value, bool withPunctuation = false)
    {
        return value.AsSpan().CharacterCount(withPunctuation);
    }

    /// <summary>
    /// Counts the number of characters.
    /// </summary>
    /// <param name="value">The text to calculate the characters for.</param>
    /// <param name="withPunctuation">True, to include punctuations.</param>
    /// <returns>
    /// The  number of characters.
    /// </returns>
    public static int CharacterCount(this ReadOnlySpan<char> value, bool withPunctuation = false)
    {
        var result = 0;

        for (var i = 0; i < value.Length; i++)
        {
            var c = value[i];

            if (char.IsLetterOrDigit(c) && (!withPunctuation || !char.IsPunctuation(c)))
            {
                result++;
            }
        }

        return result;
    }
}
