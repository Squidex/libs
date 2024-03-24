// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

namespace Squidex.Messaging;

public class KafkaTests : MessagingTestsBase
{
    protected override string TopicOrQueueName => "dev";

    protected override void Configure(MessagingBuilder builder)
    {
        builder.AddKafkaTransport(TestHelpers.Configuration);
    }
}
