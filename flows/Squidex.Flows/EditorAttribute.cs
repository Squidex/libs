// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

namespace Squidex.Flows;

[AttributeUsage(AttributeTargets.Property)]
public sealed class EditorAttribute(string editor) : Attribute
{
    public string Editor { get; } = editor;
}
