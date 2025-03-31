// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

namespace Squidex.Text;

public sealed class SvgError(string error, int lineCount = -1, int linePosition = -1)
{
    public int LineCount { get; } = lineCount;

    public int LinePosition { get; } = linePosition;

    public string Error { get; } = error;
}
