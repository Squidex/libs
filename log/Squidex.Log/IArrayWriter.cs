﻿// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

namespace Squidex.Log
{
    public interface IArrayWriter
    {
        IArrayWriter WriteValue(string value);

        IArrayWriter WriteValue(double value);

        IArrayWriter WriteValue(long value);

        IArrayWriter WriteValue(bool value);

        IArrayWriter WriteValue(TimeSpan value);

        IArrayWriter WriteValue(DateTime value);

        IArrayWriter WriteValue(DateTimeOffset value);

        IArrayWriter WriteObject(Action<IObjectWriter> objectWriter);

        IArrayWriter WriteObject<T>(T context, Action<T, IObjectWriter> objectWriter);
    }
}
