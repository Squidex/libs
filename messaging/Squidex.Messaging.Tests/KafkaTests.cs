// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

using Xunit;

namespace Squidex.Messaging;

[Trait("Category", "Dependencies")]
public class KafkaTests(KafkaFixture fixture)
    : MessagingTestsBase, IClassFixture<KafkaFixture>
{
    protected override string TopicOrQueueName => "dev";

    protected override void Configure(MessagingBuilder builder)
    {
        builder.AddKafkaTransport(TestHelpers.Configuration, options =>
        {
            options.BootstrapServers = fixture.Kafka.GetBootstrapAddress();
            options.GroupId = Guid.NewGuid().ToString();
        });
    }
}
