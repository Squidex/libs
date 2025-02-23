// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

using Xunit;

namespace Squidex.Messaging;

public class RabbitMqTests(RabbitMqFixture fixture)
    : MessagingTestsBase, IClassFixture<RabbitMqFixture>
{
    protected override string TopicOrQueueName => "dev";

    protected override bool CanHandleAndSimulateTimeout => false;

    protected override void Configure(MessagingBuilder builder)
    {
        builder.AddRabbitMqTransport(TestHelpers.Configuration, options =>
        {
            options.Uri = new Uri(fixture.RabbitMq.GetConnectionString());
        });
    }
}
