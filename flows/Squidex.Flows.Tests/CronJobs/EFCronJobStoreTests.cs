﻿// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

using Microsoft.Extensions.DependencyInjection;
using Squidex.Flows.CronJobs.Internal;

namespace Squidex.Flows.CronJobs;

[Collection(EFFlowsCollection.Name)]
public class EFCronJobStoreTests(EFFlowsFixture fixture) : CronJobStoreTests
{
    protected override Task<ICronJobStore<TestFlowContext>> CreateSutAsync()
    {
        var store = fixture.Services.GetRequiredService<ICronJobStore<TestFlowContext>>();
        return Task.FromResult(store);
    }
}
