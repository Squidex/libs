// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

using Microsoft.Extensions.DependencyInjection;
using Squidex.Flows.Internal.Execution;

namespace Squidex.Flows;

public class EFFlowStateStoreTests(EFFlowsFixture fixture) :
    FlowStateStoreTests, IClassFixture<EFFlowsFixture>
{
    protected override Task<IFlowStateStore<TestFlowContext>> CreateSutAsync()
    {
        var store = fixture.Services.GetRequiredService<IFlowStateStore<TestFlowContext>>();
        return Task.FromResult(store);
    }
}
