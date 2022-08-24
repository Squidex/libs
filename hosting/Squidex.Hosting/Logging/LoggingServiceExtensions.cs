// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Squidex.Hosting.Logging;
using Squidex.Log;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class LoggingServiceExtensions
    {
        private sealed class SemanticLogDefaultOptions
        {
            public bool Human { get; set; }

            public bool Colors { get; set; }

            public string? File { get; set; }
        }

        public static void ConfigureSemanticLog(this ILoggingBuilder builder, IConfiguration config)
        {
            builder.AddSemanticLog();

            builder.Services.AddServices(config);
        }

        private static IServiceCollection AddServices(this IServiceCollection services, IConfiguration config)
        {
            services.Configure<SemanticLogOptions>(config, "logging");
            services.Configure<SemanticLogDefaultOptions>(config, "logging");

            services.AddSingletonAs(c =>
                {
                    var human = c.GetRequiredService<IOptions<SemanticLogDefaultOptions>>().Value.Human;

                    return human ? JsonLogWriterFactory.Readable() : JsonLogWriterFactory.Default();
                })
                .As<IRootWriterFactory>();

            services.AddSingletonAs(c =>
                {
                    var useColors = c.GetRequiredService<IOptions<SemanticLogDefaultOptions>>().Value.Colors;

                    return new ConsoleLogChannel(useColors);
                })
                .As<ILogChannel>();

            services.AddSingletonAs(c =>
                {
                    var file = c.GetRequiredService<IOptions<SemanticLogDefaultOptions>>().Value.File;

                    return !string.IsNullOrWhiteSpace(file) ? (ILogChannel)new FileChannel(file) : new NoopChannel();
                })
                .As<ILogChannel>();

            services.AddSingletonAs<TimestampLogAppender>()
                .As<ILogAppender>();

            services.AddSingletonAs<DebugLogChannel>()
                .As<ILogChannel>();

            services.AddSingletonAs<SemanticLog>()
                .As<ISemanticLog>();

            return services;
        }
    }
}
