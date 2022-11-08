// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

using System.Text;

namespace Squidex.Text;

/// <summary>
/// Provides helper methods to convert the casing of strings.
/// </summary>
public static class CasingExtensions
{
    private const char NullChar = (char)0;

    /// <summary>
    /// Converts the given string to pascal case.
    /// </summary>
    /// <param name="value">The string to convert.</param>
    /// <returns>
    /// The given string in pascal case.
    /// </returns>
    public static string ToPascalCase(this string value)
    {
        return value.AsSpan().ToPascalCase();
    }

    /// <summary>
    /// Converts the given string to pascal case.
    /// </summary>
    /// <param name="value">The string to convert.</param>
    /// <returns>
    /// The given string in pascal case.
    /// </returns>
    public static string ToPascalCase(this ReadOnlySpan<char> value)
    {
        if (value.Length == 0)
        {
            return string.Empty;
        }

        var sb = new StringBuilder(value.Length);

        var last = NullChar;
        var length = 0;

        for (var i = 0; i < value.Length; i++)
        {
            var c = value[i];

            if (c == '-' || c == '_' || c == ' ')
            {
                if (last != NullChar)
                {
                    sb.Append(char.ToUpperInvariant(last));
                }

                last = NullChar;
                length = 0;
            }
            else
            {
                if (length > 1)
                {
                    sb.Append(c);
                }
                else if (length == 0)
                {
                    last = c;
                }
                else
                {
                    sb.Append(char.ToUpperInvariant(last));
                    sb.Append(c);

                    last = NullChar;
                }

                length++;
            }
        }

        if (last != NullChar)
        {
            sb.Append(char.ToUpperInvariant(last));
        }

        return sb.ToString();
    }

    /// <summary>
    /// Converts the given string to kebap case.
    /// </summary>
    /// <param name="value">The string to convert.</param>
    /// <returns>
    /// The given string in kebap case.
    /// </returns>
    public static string ToKebabCase(this string value)
    {
        return value.AsSpan().ToKebabCase();
    }

    /// <summary>
    /// Converts the given string to kebap case.
    /// </summary>
    /// <param name="value">The string to convert.</param>
    /// <returns>
    /// The given string in kebap case.
    /// </returns>
    public static string ToKebabCase(this ReadOnlySpan<char> value)
    {
        if (value.Length == 0)
        {
            return string.Empty;
        }

        var sb = new StringBuilder(value.Length);

        var length = 0;

        for (var i = 0; i < value.Length; i++)
        {
            var c = value[i];

            if (c == '-' || c == '_' || c == ' ')
            {
                length = 0;
            }
            else
            {
                if (length > 0)
                {
                    sb.Append(char.ToLowerInvariant(c));
                }
                else
                {
                    if (sb.Length > 0)
                    {
                        sb.Append('-');
                    }

                    sb.Append(char.ToLowerInvariant(c));
                }

                length++;
            }
        }

        return sb.ToString();
    }

    /// <summary>
    /// Converts the given string to camel case.
    /// </summary>
    /// <param name="value">The string to convert.</param>
    /// <returns>
    /// The given string in camel case.
    /// </returns>
    public static string ToCamelCase(this string value)
    {
        return value.AsSpan().ToCamelCase();
    }

    /// <summary>
    /// Converts the given string to camel case.
    /// </summary>
    /// <param name="value">The string to convert.</param>
    /// <returns>
    /// The given string in camel case.
    /// </returns>
    public static string ToCamelCase(this ReadOnlySpan<char> value)
    {
        if (value.Length == 0)
        {
            return string.Empty;
        }

        var sb = new StringBuilder(value.Length);

        var last = NullChar;
        var length = 0;

        for (var i = 0; i < value.Length; i++)
        {
            var c = value[i];

            if (c == '-' || c == '_' || c == ' ')
            {
                if (last != NullChar)
                {
                    if (sb.Length > 0)
                    {
                        sb.Append(char.ToUpperInvariant(last));
                    }
                    else
                    {
                        sb.Append(char.ToLowerInvariant(last));
                    }
                }

                last = NullChar;
                length = 0;
            }
            else
            {
                if (length > 1)
                {
                    sb.Append(c);
                }
                else if (length == 0)
                {
                    last = c;
                }
                else
                {
                    if (sb.Length > 0)
                    {
                        sb.Append(char.ToUpperInvariant(last));
                    }
                    else
                    {
                        sb.Append(char.ToLowerInvariant(last));
                    }

                    sb.Append(c);

                    last = NullChar;
                }

                length++;
            }
        }

        if (last != NullChar)
        {
            if (sb.Length > 0)
            {
                sb.Append(char.ToUpperInvariant(last));
            }
            else
            {
                sb.Append(char.ToLowerInvariant(last));
            }
        }

        return sb.ToString();
    }
}
