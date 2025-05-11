// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

using TestHelpers.MongoDb;

#pragma warning disable MA0048 // File name must match type name

namespace Squidex.Messaging;

public sealed class MongoMessagingFixture() : MongoFixture("messaging-mongo")
{
    protected override void AddServices(IServiceCollection services)
    {
    }
}

[CollectionDefinition(Name)]
public sealed class MongoMessagingCollection : ICollectionFixture<MongoMessagingFixture>
{
    public const string Name = "messaging-mongo";
}
