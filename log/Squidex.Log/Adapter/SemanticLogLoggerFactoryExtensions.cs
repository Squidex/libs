// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

using Microsoft.Extensions.Logging;
using Squidex.Log;
using Squidex.Log.Adapter;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class SemanticLogLoggerFactoryExtensions
    {
        public static ILoggingBuilder AddSemanticLog(this ILoggingBuilder builder)
        {
            builder.Services.AddSingleton<ILoggerProvider, SemanticLogLoggerProvider>();

            return builder;
        }

        public static ILoggerFactory AddSemanticLog(this ILoggerFactory factory, ISemanticLog log)
        {
            factory.AddProvider(new SemanticLogLoggerProvider(log));

            return factory;
        }
    }
}
