﻿// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

using System.Globalization;
using System.Text;
using System.Text.Json;

namespace Squidex.Log;

public sealed class JsonLogWriter : IRootWriter, IArrayWriter
{
    private readonly JsonWriterOptions formatting;
    private readonly bool formatLine;
    private readonly MemoryStream stream = new MemoryStream();
    private readonly StreamReader streamReader;
    private Utf8JsonWriter? jsonWriter;

    public long BufferSize
    {
        get { return stream.Length; }
    }

    internal JsonLogWriter(JsonWriterOptions formatting, bool formatLine)
    {
        this.formatLine = formatLine;
        this.formatting = formatting;

        streamReader = new StreamReader(stream, Encoding.UTF8);
    }

    internal void Reset()
    {
        jsonWriter = null;

        stream.Position = 0;
        stream.SetLength(0);
    }

    IArrayWriter IArrayWriter.WriteValue(string value)
    {
        jsonWriter?.WriteStringValue(value);

        return this;
    }

    IArrayWriter IArrayWriter.WriteValue(double value)
    {
        jsonWriter?.WriteNumberValue(value);

        return this;
    }

    IArrayWriter IArrayWriter.WriteValue(long value)
    {
        jsonWriter?.WriteNumberValue(value);

        return this;
    }

    IArrayWriter IArrayWriter.WriteValue(bool value)
    {
        jsonWriter?.WriteBooleanValue(value);

        return this;
    }

    IArrayWriter IArrayWriter.WriteValue(DateTime value)
    {
        jsonWriter?.WriteStringValue(ToIso8601(value.ToUniversalTime()));

        return this;
    }

    IArrayWriter IArrayWriter.WriteValue(DateTimeOffset value)
    {
        jsonWriter?.WriteStringValue(ToIso8601(value.UtcDateTime));

        return this;
    }

    IArrayWriter IArrayWriter.WriteValue(TimeSpan value)
    {
        jsonWriter?.WriteStringValue(value.ToString());

        return this;
    }

    IObjectWriter IObjectWriter.WriteProperty(string property, string? value)
    {
        jsonWriter?.WriteString(property, value);

        return this;
    }

    IObjectWriter IObjectWriter.WriteProperty(string property, double value)
    {
        jsonWriter?.WriteNumber(property, value);

        return this;
    }

    IObjectWriter IObjectWriter.WriteProperty(string property, long value)
    {
        jsonWriter?.WriteNumber(property, value);

        return this;
    }

    IObjectWriter IObjectWriter.WriteProperty(string property, bool value)
    {
        jsonWriter?.WriteBoolean(property, value);

        return this;
    }

    IObjectWriter IObjectWriter.WriteProperty(string property, DateTime value)
    {
        jsonWriter?.WriteString(property, ToIso8601(value.ToUniversalTime()));

        return this;
    }

    IObjectWriter IObjectWriter.WriteProperty(string property, DateTimeOffset value)
    {
        jsonWriter?.WriteString(property, ToIso8601(value.UtcDateTime));

        return this;
    }

    IObjectWriter IObjectWriter.WriteProperty(string property, TimeSpan value)
    {
        jsonWriter?.WriteString(property, value.ToString());

        return this;
    }

    IObjectWriter IObjectWriter.WriteObject(string property, Action<IObjectWriter> objectWriter)
    {
        if (jsonWriter != null)
        {
            jsonWriter.WritePropertyName(property);
            jsonWriter.WriteStartObject();

            objectWriter?.Invoke(this);

            jsonWriter.WriteEndObject();
        }

        return this;
    }

    IObjectWriter IObjectWriter.WriteObject<T>(string property, T context, Action<T, IObjectWriter> objectWriter)
    {
        if (jsonWriter != null)
        {
            jsonWriter.WritePropertyName(property);
            jsonWriter.WriteStartObject();

            objectWriter?.Invoke(context, this);

            jsonWriter.WriteEndObject();
        }

        return this;
    }

    IObjectWriter IObjectWriter.WriteArray(string property, Action<IArrayWriter> arrayWriter)
    {
        if (jsonWriter != null)
        {
            jsonWriter.WritePropertyName(property);
            jsonWriter.WriteStartArray();

            arrayWriter?.Invoke(this);

            jsonWriter.WriteEndArray();
        }

        return this;
    }

    IObjectWriter IObjectWriter.WriteArray<T>(string property, T context, Action<T, IArrayWriter> arrayWriter)
    {
        if (jsonWriter != null)
        {
            jsonWriter.WritePropertyName(property);
            jsonWriter.WriteStartArray();

            arrayWriter?.Invoke(context, this);

            jsonWriter.WriteEndArray();
        }

        return this;
    }

    IArrayWriter IArrayWriter.WriteObject(Action<IObjectWriter> objectWriter)
    {
        if (jsonWriter != null)
        {
            jsonWriter.WriteStartObject();

            objectWriter?.Invoke(this);

            jsonWriter.WriteEndObject();
        }

        return this;
    }

    IArrayWriter IArrayWriter.WriteObject<T>(T context, Action<T, IObjectWriter> objectWriter)
    {
        if (jsonWriter != null)
        {
            jsonWriter.WriteStartObject();

            objectWriter?.Invoke(context, this);

            jsonWriter.WriteEndObject();
        }

        return this;
    }

    void IRootWriter.Start()
    {
        if (jsonWriter != null)
        {
            throw new InvalidOperationException("Already started.");
        }

        jsonWriter = new Utf8JsonWriter(stream, formatting);
        jsonWriter.WriteStartObject();
    }

    public string End()
    {
        if (jsonWriter == null)
        {
            throw new InvalidOperationException("Already started.");
        }

        try
        {
            jsonWriter.WriteEndObject();
            jsonWriter.Flush();

            stream.Position = 0;
            streamReader.DiscardBufferedData();

            var json = streamReader.ReadToEnd();

            if (formatLine)
            {
                json += Environment.NewLine;
            }

            return json;
        }
        finally
        {
            Reset();
        }
    }

    private static string ToIso8601(DateTime value)
    {
        return value.ToUniversalTime().ToString("yyyy-MM-ddTHH:mm:ssZ", CultureInfo.InvariantCulture);
    }
}
