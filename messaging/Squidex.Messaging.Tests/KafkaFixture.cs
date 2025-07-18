﻿// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

using System.Diagnostics;
using Confluent.Kafka;
using Confluent.Kafka.Admin;
using Testcontainers.Kafka;

namespace Squidex.Messaging;

public class KafkaFixture : IAsyncLifetime
{
    public KafkaContainer Kafka { get; } =
        new KafkaBuilder()
            .WithReuse(Debugger.IsAttached)
            .WithLabel("reuse-id", "messaging-kafka")
            .Build();

    public async Task InitializeAsync()
    {
        await Kafka.StartAsync();

        using var adminClient =
            new AdminClientBuilder(
                new AdminClientConfig { BootstrapServers = Kafka.GetBootstrapAddress() })
            .Build();

        await adminClient.CreateTopicsAsync([
            new TopicSpecification { Name = "dev" },
        ]);
    }

    public async Task DisposeAsync()
    {
        await Kafka.StopAsync();
    }
}
