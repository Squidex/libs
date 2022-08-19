// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

using Confluent.Kafka;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Squidex.Messaging.Kafka
{
    public sealed class KafkaOwner
    {
        private readonly IProducer<Null, Null> producer;

        public Handle Handle => producer.Handle;

        public KafkaTransportOptions Options { get; }

        public KafkaOwner(IOptions<KafkaTransportOptions> options,
            ILogger<KafkaTransport> log)
        {
            Options = options.Value;

            producer =
                new ProducerBuilder<Null, Null>(options.Value)
                    .SetLogHandler(KafkaLogFactory<Null, Null>.ProducerLog(log))
                    .SetErrorHandler(KafkaLogFactory<Null, Null>.ProducerError(log))
                    .SetStatisticsHandler(KafkaLogFactory<Null, Null>.ProducerStats(log))
                    .Build();
        }

        public void Dispose()
        {
            producer.Dispose();
        }
    }
}
