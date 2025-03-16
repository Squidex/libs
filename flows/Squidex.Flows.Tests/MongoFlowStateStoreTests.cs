// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

using Microsoft.Extensions.DependencyInjection;
using Squidex.Flows.Execution;
using TestHelpers;
using TestHelpers.MongoDb;

#pragma warning disable MA0048 // File name must match type name

namespace Squidex.Flows;

public sealed class MongoFlowStateStoreFixture() : MongoFixture("flows-mongo")
{
    protected override void AddServices(IServiceCollection services)
    {
        services.AddFlows<TestFlowContext>(TestUtils.Configuration)
            .AddMongoFlowStore<TestFlowContext>();
    }
}

public class MongoFlowStateStoreTests(MongoFlowStateStoreFixture fixture) :
    FlowStateStoreTests, IClassFixture<MongoFlowStateStoreFixture>
{
    protected override Task<IFlowStateStore<TestFlowContext>> CreateSutAsync()
    {
        var store = fixture.Services.GetRequiredService<IFlowStateStore<TestFlowContext>>();
        return Task.FromResult(store);
    }
}
