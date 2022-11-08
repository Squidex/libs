// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

namespace Squidex.Log;

#pragma warning disable MA0048 // File name must match type name
public delegate void LogFormatter(IObjectWriter writer);

public delegate void LogFormatter<in T>(T context, IObjectWriter writer);
#pragma warning restore MA0048 // File name must match type name

public interface ISemanticLog
{
    void Log<T>(SemanticLogLevel logLevel, T context, Exception? exception, LogFormatter<T> action);

    void Log(SemanticLogLevel logLevel, Exception? exception, LogFormatter action);

    ISemanticLog CreateScope(ILogAppender appender);

    ISemanticLog CreateScope(Action<IObjectWriter> objectWriter);
}
