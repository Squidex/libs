// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

namespace Squidex.Text
{
    public sealed class HtmlSvgError
    {
        public int LineCount { get; }

        public int LinePosition { get; }

        public string Error { get; }

        public HtmlSvgError(string error, int lineCount = -1, int linePosition = -1)
        {
            Error = error;

            LineCount = lineCount;
            LinePosition = linePosition;
        }
    }
}
