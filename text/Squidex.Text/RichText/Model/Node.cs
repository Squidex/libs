// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

namespace Squidex.Text.RichText.Model;

public sealed class Node : Attributed
{
    public NodeType Type { get; set; }

    public string? Text { get; set; }

    public Mark[]? Marks { get; set; }

    public Node[]? Content { get; set; }
}
