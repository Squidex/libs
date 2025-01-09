// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

using Xunit;

namespace Squidex.Messaging;

[Trait("Category", "Dependencies")]
public class KafkaTests : MessagingTestsBase
{
    protected override string TopicOrQueueName => "dev";

    protected override void Configure(MessagingBuilder builder)
    {
        builder.AddKafkaTransport(TestHelpers.Configuration);
    }
}
