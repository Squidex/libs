// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

namespace Squidex.Text.RichText.Model;

public interface INode : IAttributed
{
    string Type { get; }

    string? Text { get; }

    IMark? GetNextMark(RichTextOptions options);

    void IterateContent<T>(T state, RichTextOptions options, Action<INode, T, bool, bool> action);

    public void Reset()
    {
    }
}
