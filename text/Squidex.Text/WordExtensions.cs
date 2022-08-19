// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

namespace Squidex.Text
{
    /// <summary>
    /// Helper methods to deal with words.
    /// </summary>
    public static class WordExtensions
    {
        /// <summary>
        /// Counts the number of words in a text.
        /// </summary>
        /// <param name="value">The given text.</param>
        /// <returns>
        /// The number of found words in the text.
        /// </returns>
        public static int WordCount(this string value)
        {
            if (value == null)
            {
                return 0;
            }

            return value.AsSpan().WordCount();
        }

        /// <summary>
        /// Counts the number of words in a text.
        /// </summary>
        /// <param name="value">The given text.</param>
        /// <returns>
        /// The number of found words in the text.
        /// </returns>
        public static int WordCount(this ReadOnlySpan<char> value)
        {
            if (value.Length == 0)
            {
                return 0;
            }

            var result = 0;

            for (var i = 0; i < value.Length; i++)
            {
                var current = value[i];

                if (!char.IsWhiteSpace(current) && !char.IsPunctuation(current) && WordBoundary.IsBoundary(value, i))
                {
                    result++;
                }
            }

            return result;
        }
    }
}
