// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

using Confluent.Kafka;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Squidex.Messaging.Kafka;

public sealed class KafkaOwner(IOptions<KafkaTransportOptions> options,
    ILogger<KafkaTransport> log)
{
    private readonly IProducer<Null, Null> producer =
            new ProducerBuilder<Null, Null>(options.Value)
                .SetLogHandler(KafkaLogFactory<Null, Null>.ProducerLog(log))
                .SetErrorHandler(KafkaLogFactory<Null, Null>.ProducerError(log))
                .SetStatisticsHandler(KafkaLogFactory<Null, Null>.ProducerStats(log))
                .Build();

    public Handle Handle => producer.Handle;

    public KafkaTransportOptions Options { get; } = options.Value;

    public void Dispose()
    {
        producer.Dispose();
    }
}
