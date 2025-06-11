// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

#pragma warning disable CA2253 // Named placeholders should not be numeric values

namespace Squidex.Log;

public class SemanticLogTests
{
    private readonly List<ILogAppender> appenders = [];
    private readonly List<ILogChannel> channels = [];
    private readonly IOptions<SemanticLogOptions> options = Options.Create(new SemanticLogOptions());
    private readonly Lazy<SemanticLog> log;
    private readonly ILogChannel channel = A.Fake<ILogChannel>();
    private string output = string.Empty;

    public SemanticLog Log
    {
        get { return log.Value; }
    }

    public SemanticLogTests()
    {
        options.Value.Level = SemanticLogLevel.Trace;

        channels.Add(channel);

        A.CallTo(() => channel.Log(A<SemanticLogLevel>._, A<string>._))
            .Invokes((SemanticLogLevel level, string message) =>
            {
                output += message;
            });

        log = new Lazy<SemanticLog>(() => new SemanticLog(options, channels, appenders, JsonLogWriterFactory.Default()));
    }

    [Fact]
    public void Should_log_multiple_lines()
    {
        Log.Log(SemanticLogLevel.Error, null, w => w.WriteProperty("logMessage", "Msg1"));
        Log.Log(SemanticLogLevel.Error, null, w => w.WriteProperty("logMessage", "Msg2"));

        var expected1 =
            LogTest(w => w
                .WriteProperty("logLevel", "Error")
                .WriteProperty("logMessage", "Msg1"));

        var expected2 =
            LogTest(w => w
                .WriteProperty("logLevel", "Error")
                .WriteProperty("logMessage", "Msg2"));

        Assert.Equal(expected1 + expected2, output);
    }

    [Fact]
    public void Should_log_timestamp()
    {
        var now = DateTime.UtcNow;

        appenders.Add(new TimestampLogAppender(GetTimeProvider(new DateTimeOffset(now, default))));

        Log.LogFatal(w => { /* Do Nothing */ });

        var expected =
            LogTest(w => w
                .WriteProperty("logLevel", "Fatal")
                .WriteProperty("timestamp", now));

        Assert.Equal(expected, output);
    }

    [Fact]
    public void Should_log_values_with_appender()
    {
        appenders.Add(new ConstantsLogAppender(w => w.WriteProperty("logValue", 1500)));

        Log.LogFatal(m => { });

        var expected =
            LogTest(w => w
                .WriteProperty("logLevel", "Fatal")
                .WriteProperty("logValue", 1500));

        Assert.Equal(expected, output);
    }

    [Fact]
    public void Should_log_application_info()
    {
        var sessionId = Guid.NewGuid();

        appenders.Add(new ApplicationInfoLogAppender(GetType(), sessionId));

        Log.LogFatal(m => { });

        var expected =
            LogTest(w => w
                .WriteProperty("logLevel", "Fatal")
                .WriteObject("app", a => a
                    .WriteProperty("name", "Squidex.Log.Tests")
                    .WriteProperty("version", GetType().Assembly.GetName().Version!.ToString())
                    .WriteProperty("sessionId", sessionId.ToString())));

        Assert.Equal(expected, output);
    }

    [Fact]
    public void Should_log_with_trace()
    {
        Log.LogTrace(w => w.WriteProperty("logValue", 1500));

        var expected =
            LogTest(w => w
                .WriteProperty("logLevel", "Trace")
                .WriteProperty("logValue", 1500));

        Assert.Equal(expected, output);
    }

    [Fact]
    public void Should_log_with_trace_and_context()
    {
        Log.LogTrace(1500, (ctx, w) => w.WriteProperty("logValue", ctx));

        var expected =
            LogTest(w => w
                .WriteProperty("logLevel", "Trace")
                .WriteProperty("logValue", 1500));

        Assert.Equal(expected, output);
    }

    [Fact]
    public void Should_log_with_debug()
    {
        Log.LogDebug(w => w.WriteProperty("logValue", 1500));

        var expected =
            LogTest(w => w
                .WriteProperty("logLevel", "Debug")
                .WriteProperty("logValue", 1500));

        Assert.Equal(expected, output);
    }

    [Fact]
    public void Should_log_with_debug_and_context()
    {
        Log.LogDebug(1500, (ctx, w) => w.WriteProperty("logValue", ctx));

        var expected =
            LogTest(w => w
                .WriteProperty("logLevel", "Debug")
                .WriteProperty("logValue", 1500));

        Assert.Equal(expected, output);
    }

    [Fact]
    public void Should_log_with_information()
    {
        Log.LogInformation(w => w.WriteProperty("logValue", 1500));

        var expected =
            LogTest(w => w
                .WriteProperty("logLevel", "Information")
                .WriteProperty("logValue", 1500));

        Assert.Equal(expected, output);
    }

    [Fact]
    public void Should_log_with_information_and_context()
    {
        Log.LogInformation(1500, (ctx, w) => w.WriteProperty("logValue", ctx));

        var expected =
            LogTest(w => w
                .WriteProperty("logLevel", "Information")
                .WriteProperty("logValue", 1500));

        Assert.Equal(expected, output);
    }

    [Fact]
    public void Should_log_with_warning()
    {
        Log.LogWarning(w => w.WriteProperty("logValue", 1500));

        var expected =
            LogTest(w => w
                .WriteProperty("logLevel", "Warning")
                .WriteProperty("logValue", 1500));

        Assert.Equal(expected, output);
    }

    [Fact]
    public void Should_log_with_warning_and_context()
    {
        Log.LogWarning(1500, (ctx, w) => w.WriteProperty("logValue", ctx));

        var expected =
            LogTest(w => w
                .WriteProperty("logLevel", "Warning")
                .WriteProperty("logValue", 1500));

        Assert.Equal(expected, output);
    }

    [Fact]
    public void Should_log_with_warning_exception()
    {
        var exception = new InvalidOperationException();

        Log.LogWarning(exception, w => { });

        var expected =
            LogTest(w => w
                .WriteProperty("logLevel", "Warning")
                .WriteException(exception));

        Assert.Equal(expected, output);
    }

    [Fact]
    public void Should_log_with_warning_exception_and_context()
    {
        var exception = new InvalidOperationException();

        Log.LogWarning(exception, 1500, (ctx, w) => w.WriteProperty("logValue", ctx));

        var expected =
            LogTest(w => w
                .WriteProperty("logLevel", "Warning")
                .WriteProperty("logValue", 1500)
                .WriteException(exception));

        Assert.Equal(expected, output);
    }

    [Fact]
    public void Should_log_with_error()
    {
        Log.LogError(w => w.WriteProperty("logValue", 1500));

        var expected =
            LogTest(w => w
                .WriteProperty("logLevel", "Error")
                .WriteProperty("logValue", 1500));

        Assert.Equal(expected, output);
    }

    [Fact]
    public void Should_log_with_error_and_context()
    {
        Log.LogError(1500, (ctx, w) => w.WriteProperty("logValue", ctx));

        var expected =
            LogTest(w => w
                .WriteProperty("logLevel", "Error")
                .WriteProperty("logValue", 1500));

        Assert.Equal(expected, output);
    }

    [Fact]
    public void Should_log_with_error_exception()
    {
        var exception = new InvalidOperationException();

        Log.LogError(exception, w => { });

        var expected =
            LogTest(w => w
                .WriteProperty("logLevel", "Error")
                .WriteException(exception));

        Assert.Equal(expected, output);
    }

    [Fact]
    public void Should_log_with_error_exception_and_context()
    {
        var exception = new InvalidOperationException();

        Log.LogError(exception, 1500, (ctx, w) => w.WriteProperty("logValue", ctx));

        var expected =
            LogTest(w => w
                .WriteProperty("logLevel", "Error")
                .WriteProperty("logValue", 1500)
                .WriteException(exception));

        Assert.Equal(expected, output);
    }

    [Fact]
    public void Should_log_with_fatal()
    {
        Log.LogFatal(w => w.WriteProperty("logValue", 1500));

        var expected =
            LogTest(w => w
                .WriteProperty("logLevel", "Fatal")
                .WriteProperty("logValue", 1500));

        Assert.Equal(expected, output);
    }

    [Fact]
    public void Should_log_with_fatal_and_context()
    {
        Log.LogFatal(1500, (ctx, w) => w.WriteProperty("logValue", ctx));

        var expected =
            LogTest(w => w
                .WriteProperty("logLevel", "Fatal")
                .WriteProperty("logValue", 1500));

        Assert.Equal(expected, output);
    }

    [Fact]
    public void Should_log_with_fatal_exception()
    {
        var exception = new InvalidOperationException();

        Log.LogFatal(exception, w => { });

        var expected =
            LogTest(w => w
                .WriteProperty("logLevel", "Fatal")
                .WriteException(exception));

        Assert.Equal(expected, output);
    }

    [Fact]
    public void Should_log_with_fatal_exception_and_context()
    {
        var exception = new InvalidOperationException();

        Log.LogFatal(exception, 1500, (ctx, w) => w.WriteProperty("logValue", ctx));

        var expected =
            LogTest(w => w
                .WriteProperty("logLevel", "Fatal")
                .WriteProperty("logValue", 1500)
                .WriteException(exception));

        Assert.Equal(expected, output);
    }

    [Fact]
    public void Should_measure_trace()
    {
        Log.MeasureTrace(w => w.WriteProperty("message", "My Message")).Dispose();

        var expected =
            LogTest(w => w
                .WriteProperty("logLevel", "Trace")
                .WriteProperty("message", "My Message")
                .WriteProperty("elapsedMs", 0));

        Assert.StartsWith(expected[..55], output, StringComparison.Ordinal);
    }

    [Fact]
    public void Should_measure_trace_with_contex()
    {
        Log.MeasureTrace("My Message", (ctx, w) => w.WriteProperty("message", ctx)).Dispose();

        var expected =
            LogTest(w => w
                .WriteProperty("logLevel", "Trace")
                .WriteProperty("message", "My Message")
                .WriteProperty("elapsedMs", 0));

        Assert.StartsWith(expected[..55], output, StringComparison.Ordinal);
    }

    [Fact]
    public void Should_measure_debug()
    {
        Log.MeasureDebug(w => w.WriteProperty("message", "My Message")).Dispose();

        var expected =
            LogTest(w => w
                .WriteProperty("logLevel", "Debug")
                .WriteProperty("message", "My Message")
                .WriteProperty("elapsedMs", 0));

        Assert.StartsWith(expected[..55], output, StringComparison.Ordinal);
    }

    [Fact]
    public void Should_measure_debug_with_contex()
    {
        Log.MeasureDebug("My Message", (ctx, w) => w.WriteProperty("message", ctx)).Dispose();

        var expected =
            LogTest(w => w
                .WriteProperty("logLevel", "Debug")
                .WriteProperty("message", "My Message")
                .WriteProperty("elapsedMs", 0));

        Assert.StartsWith(expected[..55], output, StringComparison.Ordinal);
    }

    [Fact]
    public void Should_measure_information()
    {
        Log.MeasureInformation(w => w.WriteProperty("message", "My Message")).Dispose();

        var expected =
            LogTest(w => w
                .WriteProperty("logLevel", "Information")
                .WriteProperty("message", "My Message")
                .WriteProperty("elapsedMs", 0));

        Assert.StartsWith(expected[..55], output, StringComparison.Ordinal);
    }

    [Fact]
    public void Should_measure_information_with_contex()
    {
        Log.MeasureInformation("My Message", (ctx, w) => w.WriteProperty("message", ctx)).Dispose();

        var expected =
            LogTest(w => w
                .WriteProperty("logLevel", "Information")
                .WriteProperty("message", "My Message")
                .WriteProperty("elapsedMs", 0));

        Assert.StartsWith(expected[..55], output, StringComparison.Ordinal);
    }

    [Fact]
    public void Should_log_with_extensions_logger()
    {
        var exception = new InvalidOperationException();

        var loggerFactory =
            new LoggerFactory()
                .AddSemanticLog(Log);
        var loggerInstance = loggerFactory.CreateLogger<SemanticLogTests>();

        loggerInstance.LogCritical(new EventId(123, "EventName"), exception, "Log {0}", 123);

        var expected =
            LogTest(w => w
                .WriteProperty("logLevel", "Fatal")
                .WriteProperty("message", "Log 123")
                .WriteObject("eventId", e => e
                    .WriteProperty("id", 123)
                    .WriteProperty("name", "EventName"))
                .WriteProperty("category", "Squidex.Log.SemanticLogTests")
                .WriteException(exception));

        Assert.Equal(expected, output);
    }

    [Fact]
    public void Should_catch_all_exceptions_from_all_channels_when_exceptions_are_thrown()
    {
        var exception1 = new InvalidOperationException();
        var exception2 = new InvalidOperationException();

        var channel1 = A.Fake<ILogChannel>();
        var channel2 = A.Fake<ILogChannel>();

        A.CallTo(() => channel1.Log(A<SemanticLogLevel>._, A<string>._)).Throws(exception1);
        A.CallTo(() => channel2.Log(A<SemanticLogLevel>._, A<string>._)).Throws(exception2);

        var sut = new SemanticLog(options, [channel1, channel2], [], JsonLogWriterFactory.Default());

        try
        {
            sut.Log(SemanticLogLevel.Debug, null, w => w.WriteProperty("should", "throw"));

            Assert.False(true);
        }
        catch (AggregateException ex)
        {
            Assert.Equal(exception1, ex.InnerExceptions[0]);
            Assert.Equal(exception2, ex.InnerExceptions[1]);
        }
    }

    [Fact]
    public void Should_not_log_if_level_is_too_low()
    {
        options.Value.Level = SemanticLogLevel.Error;

        Log.LogWarning(w => w.WriteProperty("Property", "Value"));

        Assert.Equal(string.Empty, output);
    }

    private static TimeProvider GetTimeProvider(DateTimeOffset now)
    {
        var timeProvider = A.Fake<TimeProvider>();

        A.CallTo(() => timeProvider.GetUtcNow())
            .Returns(now);

        return timeProvider;
    }

    private static string LogTest(Action<IObjectWriter> writer)
    {
        var sut = JsonLogWriterFactory.Default().Create();

        sut.Start();

        writer(sut);

        return sut.End();
    }
}
