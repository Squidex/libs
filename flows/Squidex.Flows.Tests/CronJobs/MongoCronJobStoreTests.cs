// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

using Microsoft.Extensions.DependencyInjection;
using Squidex.Flows.CronJobs.Internal;

#pragma warning disable MA0048 // File name must match type name

namespace Squidex.Flows.CronJobs;

public class MongoCronJobStoreTests(MongoFlowsFixture fixture) :
    CronJobStoreTests, IClassFixture<MongoFlowsFixture>
{
    protected override Task<ICronJobStore<TestFlowContext>> CreateSutAsync()
    {
        var store = fixture.Services.GetRequiredService<ICronJobStore<TestFlowContext>>();
        return Task.FromResult(store);
    }
}
