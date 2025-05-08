// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

using Microsoft.Extensions.DependencyInjection;
using Squidex.AI.Implementation;
using TestHelpers;
using TestHelpers.MongoDb;

#pragma warning disable MA0048 // File name must match type name

namespace Squidex.AI;

public class MongoChatStoreFixture() : MongoFixture("chat-mongo")
{
    protected override void AddServices(IServiceCollection services)
    {
        services.AddAI()
            .AddMongoChatStore(TestUtils.Configuration);
    }
}

public class MongoChatStoreTests(MongoChatStoreFixture fixture)
    : ChatStoreTests, IClassFixture<MongoChatStoreFixture>
{
    public override Task<IChatStore> CreateSutAsync()
    {
        var store = fixture.Services.GetRequiredService<IChatStore>();
        return Task.FromResult(store);
    }
}
