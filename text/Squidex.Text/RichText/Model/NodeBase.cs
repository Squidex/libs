﻿// ==========================================================================
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

    public abstract MarkBase? GetNextMark();

    public abstract void IterateContent<T>(T state, Action<NodeBase, T> action);

    public virtual void Reset()
    {
    }
}
