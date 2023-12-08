// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

namespace Squidex.Text.RichText.Model;

public abstract class NodeBase : Attributed
{
    public abstract NodeType GetNodeType();

    public abstract string GetText();

    public abstract MarkBase? GetNextMarkReverse();

    public abstract NodeBase? GetNextNode();

    public virtual void Reset()
    {
    }
}
