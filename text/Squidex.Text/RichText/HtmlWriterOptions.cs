// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

namespace Squidex.Text.RichText;

public struct HtmlWriterOptions
{
    public int Indentation { get; set; } = 4;

    public HtmlWriterOptions()
    {
        Indentation = 4;
    }
}
