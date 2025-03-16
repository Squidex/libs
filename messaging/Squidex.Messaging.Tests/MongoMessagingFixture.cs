// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

using TestHelpers.MongoDb;

namespace Squidex.Messaging;

public sealed class MongoMessagingFixture() : MongoFixture("messaging-mongo")
{
    protected override void AddServices(IServiceCollection services)
    {
    }
}
