// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

using System.Text.Json;
using Microsoft.Extensions.ObjectPool;

namespace Squidex.Log;

public sealed class JsonLogWriterFactory(bool indended = false, bool formatLine = false) : IRootWriterFactory
{
    private readonly ObjectPool<JsonLogWriter> writerPool = new DefaultObjectPoolProvider().Create(new JsonLogWriterPolicy(indended, formatLine));

    internal sealed class JsonLogWriterPolicy : PooledObjectPolicy<JsonLogWriter>
    {
        private const int MaxCapacity = 5000;
        private readonly JsonWriterOptions formatting;
        private readonly bool formatLine;

        public JsonLogWriterPolicy(bool indended = false, bool formatLine = false)
        {
            formatting.Indented = indended;

            this.formatLine = formatLine;
        }

        public override JsonLogWriter Create()
        {
            return new JsonLogWriter(formatting, formatLine);
        }

        public override bool Return(JsonLogWriter obj)
        {
            if (obj.BufferSize > MaxCapacity)
            {
                return false;
            }

            obj.Reset();

            return true;
        }
    }

    public static JsonLogWriterFactory Default()
    {
        return new JsonLogWriterFactory();
    }

    public static JsonLogWriterFactory Readable()
    {
        return new JsonLogWriterFactory(true, true);
    }

    public IRootWriter Create()
    {
        return writerPool.Get();
    }

    public void Release(IRootWriter writer)
    {
        writerPool.Return((JsonLogWriter)writer);
    }
}
