// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

using TestHelpers;

namespace Squidex.Messaging;

[Trait("Category", "Dependencies")]
public class GoogleCloudTests : MessagingTestsBase
{
    protected override string TopicOrQueueName => "messaging-tests";

    protected override bool CanHandleAndSimulateTimeout => false;

    protected override void Configure(MessagingBuilder builder)
    {
        builder.AddGooglePubSubTransport(TestUtils.Configuration);
    }
}
