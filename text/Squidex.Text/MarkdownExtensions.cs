// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

using Markdig;

namespace Squidex.Text
{
    public static class MarkdownExtensions
    {
        public static string Markdown2Text(this string markdown)
        {
            return Markdown.ToPlainText(markdown).Trim(' ', '\n', '\r');
        }
    }
}
