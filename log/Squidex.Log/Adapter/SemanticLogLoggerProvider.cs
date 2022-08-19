// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Squidex.Log.Adapter
{
    public class SemanticLogLoggerProvider : ILoggerProvider
    {
        private readonly ConcurrentDictionary<string, SemanticLogLogger> loggers = new ConcurrentDictionary<string, SemanticLogLogger>();
        private readonly IServiceProvider services;
        private ISemanticLog? log;

        public SemanticLogLoggerProvider(IServiceProvider services)
        {
            Guard.NotNull(services, nameof(services));

            this.services = services;
        }

        internal SemanticLogLoggerProvider(ISemanticLog? log)
        {
            this.log = log;
        }

        public static SemanticLogLoggerProvider ForTesting(ISemanticLog? log)
        {
            return new SemanticLogLoggerProvider(log);
        }

        public ILogger CreateLogger(string categoryName)
        {
            if (log == null && services != null)
            {
                log = services.GetService(typeof(ISemanticLog)) as ISemanticLog;
            }

            if (log == null)
            {
                return NullLogger.Instance;
            }

            return loggers.TryGetValue(categoryName, out var logger) ?
                logger :
#pragma warning disable MA0106 // Avoid closure by using an overload with the 'factoryArgument' parameter
                loggers.GetOrAdd(categoryName, x =>
                {
                    var appender = new CategoryNameAppender(x);

                    return new SemanticLogLogger(log.CreateScope(appender));
                });
#pragma warning restore MA0106 // Avoid closure by using an overload with the 'factoryArgument' parameter
        }

        public void Dispose()
        {
            GC.SuppressFinalize(this);
        }
    }
}
