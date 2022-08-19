// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

using Squidex.Hosting.Configuration;
using StackExchange.Redis;

namespace Squidex.Messaging.Redis
{
    public sealed class RedisTransportOptions : IValidatableOptions
    {
        public int Database { get; set; }

        public string QueuePrefix { get; set; } = "queue";

        public string TopicPrefix { get; set; } = "topic";

        public Func<TextWriter, Task<IConnectionMultiplexer>>? ConnectionFactory { get; set; }

        public ConfigurationOptions? Configuration { get; set; }

        public TimeSpan PollingInterval { get; set; } = TimeSpan.FromSeconds(1);

        public IEnumerable<ConfigurationError> Validate()
        {
            if (string.IsNullOrWhiteSpace(QueuePrefix))
            {
                yield return new ConfigurationError("Value is required.", nameof(QueuePrefix));
            }

            if (string.IsNullOrWhiteSpace(TopicPrefix))
            {
                yield return new ConfigurationError("Value is required.", nameof(TopicPrefix));
            }

            if (PollingInterval < TimeSpan.Zero || PollingInterval > TimeSpan.FromMinutes(10))
            {
                yield return new ConfigurationError("Value must be between 00:00:00 and 00:10:00.", nameof(PollingInterval));
            }
        }

        internal async Task<IConnectionMultiplexer> ConnectAsync(TextWriter log)
        {
            if (ConnectionFactory != null)
            {
                return await ConnectionFactory(log);
            }

            if (Configuration != null)
            {
                return await ConnectionMultiplexer.ConnectAsync(Configuration, log);
            }

            throw new InvalidOperationException("Either configuration or connection factory must be set.");
        }
    }
}
