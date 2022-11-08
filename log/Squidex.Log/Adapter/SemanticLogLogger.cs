// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

using System.Text;
using Microsoft.Extensions.Logging;

namespace Squidex.Log.Adapter;

internal sealed class SemanticLogLogger : ILogger
{
    private readonly ISemanticLog semanticLog;

    public SemanticLogLogger(ISemanticLog semanticLog)
    {
        this.semanticLog = semanticLog;
    }

    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
    {
        SemanticLogLevel semanticLogLevel;

        switch (logLevel)
        {
            case LogLevel.Trace:
                semanticLogLevel = SemanticLogLevel.Trace;
                break;
            case LogLevel.Debug:
                semanticLogLevel = SemanticLogLevel.Debug;
                break;
            case LogLevel.Information:
                semanticLogLevel = SemanticLogLevel.Information;
                break;
            case LogLevel.Warning:
                semanticLogLevel = SemanticLogLevel.Warning;
                break;
            case LogLevel.Error:
                semanticLogLevel = SemanticLogLevel.Error;
                break;
            case LogLevel.Critical:
                semanticLogLevel = SemanticLogLevel.Fatal;
                break;
            default:
                semanticLogLevel = SemanticLogLevel.Debug;
                break;
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

                    var trimmedName = key.Trim('{', '}', ' ');

                    if (ShouldIgnoreKey(trimmedName))
                    {
                        continue;
                    }

                    writer.WriteProperty(ToCamelCase(trimmedName), value.ToString());
                }
            }
        });
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
        return true;
    }

    public IDisposable BeginScope<TState>(TState state)
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
