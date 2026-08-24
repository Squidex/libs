// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

using System.Collections.Concurrent;
using System.Text;
using Microsoft.Extensions.Logging;

namespace Squidex.Log.Adapter;

internal sealed class SemanticLogLogger(ISemanticLog semanticLog) : ILogger
{
    private const int MaxCachedNames = 1000;
    private static readonly ConcurrentDictionary<string, string?> PropertyNames = new ConcurrentDictionary<string, string?>(StringComparer.Ordinal);

    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
    {
        var semanticLogLevel = MapLevel(logLevel);

        // The message is dropped by the log anyway, so we do not scan the state or allocate a context.
        if (!semanticLog.IsEnabled(semanticLogLevel))
        {
            return;
        }

        if (state is IReadOnlyList<KeyValuePair<string, object>> parameters)
        {
            foreach (var (_, value) in parameters)
            {
                if (value is Exception ex && exception == null)
                {
                    exception = ex;
                }
            }
        }

        var context = (eventId, state, exception, formatter);

        semanticLog.Log(semanticLogLevel, context, exception, (ctx, writer) =>
        {
            var message = ctx.formatter(ctx.state, ctx.exception);

            if (!string.IsNullOrWhiteSpace(message))
            {
                writer.WriteProperty(nameof(message), message);
            }

            if (ctx.eventId.Id > 0)
            {
                writer.WriteObject("eventId", ctx.eventId, (innerEventId, eventIdWriter) =>
                {
                    eventIdWriter.WriteProperty("id", innerEventId.Id);

                    if (!string.IsNullOrWhiteSpace(innerEventId.Name))
                    {
                        eventIdWriter.WriteProperty("name", innerEventId.Name);
                    }
                });
            }

            if (ctx.state is IReadOnlyList<KeyValuePair<string, object>> parameters2)
            {
                foreach (var (key, value) in parameters2)
                {
                    if (value == null)
                    {
                        continue;
                    }

                    var propertyName = GetPropertyName(key);

                    if (propertyName == null)
                    {
                        continue;
                    }

                    writer.WriteProperty(propertyName, value.ToString());
                }
            }
        });
    }

    /// <summary>
    /// Resolves the property name for a message template key, or null when it should be ignored. The
    /// trim and the camel casing would otherwise allocate two strings per property and per log entry.
    /// </summary>
    private static string? GetPropertyName(string key)
    {
        if (PropertyNames.TryGetValue(key, out var cached))
        {
            return cached;
        }

        var trimmedName = key.Trim('{', '}', ' ');
        var resolved = ShouldIgnoreKey(trimmedName) ? null : ToCamelCase(trimmedName);

        // Message template keys are a small fixed set, but guard against unbounded growth anyway.
        if (PropertyNames.Count < MaxCachedNames)
        {
            PropertyNames.TryAdd(key, resolved);
        }

        return resolved;
    }

    private static bool ShouldIgnoreKey(string name)
    {
        if (name.Length < 2)
        {
            return true;
        }

        if (string.Equals(name, "exception", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        return string.Equals(name, "originalFormat", StringComparison.OrdinalIgnoreCase);
    }

    public bool IsEnabled(LogLevel logLevel)
    {
        if (logLevel == LogLevel.None)
        {
            return false;
        }

        return semanticLog.IsEnabled(MapLevel(logLevel));
    }

    private static SemanticLogLevel MapLevel(LogLevel logLevel)
    {
        switch (logLevel)
        {
            case LogLevel.Trace:
                return SemanticLogLevel.Trace;
            case LogLevel.Debug:
                return SemanticLogLevel.Debug;
            case LogLevel.Information:
                return SemanticLogLevel.Information;
            case LogLevel.Warning:
                return SemanticLogLevel.Warning;
            case LogLevel.Error:
                return SemanticLogLevel.Error;
            case LogLevel.Critical:
                return SemanticLogLevel.Fatal;
            default:
                return SemanticLogLevel.Debug;
        }
    }

    public IDisposable BeginScope<TState>(TState state) where TState : notnull
    {
        return NoopDisposable.Instance;
    }

    private static string ToCamelCase(ReadOnlySpan<char> value)
    {
        const char NullChar = (char)0;

        if (value.Length == 0)
        {
            return string.Empty;
        }

        var sb = new StringBuilder(value.Length);

        var last = NullChar;
        var length = 0;

        for (var i = 0; i < value.Length; i++)
        {
            var c = value[i];

            if (c == '-' || c == '_' || c == ' ')
            {
                if (last != NullChar)
                {
                    if (sb.Length > 0)
                    {
                        sb.Append(char.ToUpperInvariant(last));
                    }
                    else
                    {
                        sb.Append(char.ToLowerInvariant(last));
                    }
                }

                last = NullChar;
                length = 0;
            }
            else
            {
                if (length > 1)
                {
                    sb.Append(c);
                }
                else if (length == 0)
                {
                    last = c;
                }
                else
                {
                    if (sb.Length > 0)
                    {
                        sb.Append(char.ToUpperInvariant(last));
                    }
                    else
                    {
                        sb.Append(char.ToLowerInvariant(last));
                    }

                    sb.Append(c);

                    last = NullChar;
                }

                length++;
            }
        }

        if (last != NullChar)
        {
            if (sb.Length > 0)
            {
                sb.Append(char.ToUpperInvariant(last));
            }
            else
            {
                sb.Append(char.ToLowerInvariant(last));
            }
        }

        return sb.ToString();
    }
}
