// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

using HtmlPerformanceKit;
using Microsoft.Extensions.ObjectPool;

#pragma warning disable MA0048 // File name must match type name

namespace Squidex.Text.Internal;

internal static class HtmlReaderPool
{
    private static readonly ObjectPool<PooledHtmlReader> Pool =
        new DefaultObjectPool<PooledHtmlReader>(new DefaultPooledObjectPolicy<PooledHtmlReader>());

    public static Lease Rent(string html)
    {
        var pooled = Pool.Get();

        pooled.Reset(html);
        return new Lease(pooled);
    }

    public readonly struct Lease(PooledHtmlReader pooled) : IDisposable
    {
        public HtmlReader Reader => pooled.Reader;

        public void Dispose()
        {
            Pool.Return(pooled);
        }
    }
}

internal sealed class PooledHtmlReader : IResettable
{
    private readonly ReusableStringReader textReader = new ReusableStringReader();

    public HtmlReader Reader { get; }

    public PooledHtmlReader()
    {
        Reader = new HtmlReader(textReader);
    }

    public void Reset(string html)
    {
        textReader.Reset(html);

        Reader.Reset(textReader);
    }

    public bool TryReset()
    {
        // Do not keep a reference to the last document in the pool.
        Reset(string.Empty);
        return true;
    }
}
