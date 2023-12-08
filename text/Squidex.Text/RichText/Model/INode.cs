// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

namespace Squidex.Text.RichText.Model;

public interface INode : IAttributed
{
    NodeType Type { get; }

    string? Text { get; }

    IMark? GetNextMark();

    void IterateContent<T>(T state, Action<INode, T, bool, bool> action);

    public void Reset()
    {
    }
}
