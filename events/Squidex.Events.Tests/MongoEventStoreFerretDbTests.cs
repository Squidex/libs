// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

using Microsoft.Extensions.DependencyInjection;
using TestHelpers;
using TestHelpers.MongoDb;

#pragma warning disable MA0048 // File name must match type name

namespace Squidex.Events;

public sealed class MongoEventStoreFerretDbFixture() : MongoFerretFixture("eventstore-mongo-ferretdb")
{
    protected override void AddServices(IServiceCollection services)
    {
        services.AddMongoEventStore(TestUtils.Configuration, options =>
        {
            options.PollingInterval = TimeSpan.FromSeconds(0.1);
        });
    }
}

[Trait("Category", "Dependencies")]
public class MongoEventStoreFerretDbTests(MongoEventStoreFerretDbFixture fixture)
    : EventStoreTests, IClassFixture<MongoEventStoreFerretDbFixture>
{
    protected override Task<IEventStore> CreateSutAsync()
    {
        var store = fixture.Services.GetRequiredService<IEventStore>();
        return Task.FromResult(store);
    }
}
