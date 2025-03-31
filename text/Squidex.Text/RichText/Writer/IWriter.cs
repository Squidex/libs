// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

namespace Squidex.Text.RichText.Writer;

internal interface IWriter
{
    IWriter PopIndent();

    IWriter PushIndent(string indent);

    IWriter Write(string text);

    IWriter WriteLine();

    IWriter WriteLine(string text);

    IWriter EnsureLine();
}
