// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

using Microsoft.Extensions.DependencyInjection;
using TestHelpers;
using TestHelpers.MongoDb;

namespace Squidex.Flows;

public sealed class MongoFlowsFixture() : MongoFixture("flows-mongo")
{
    protected override void AddServices(IServiceCollection services)
    {
        services.AddFlows<TestFlowContext>(TestUtils.Configuration)
            .AddMongoStore<TestFlowContext>();

        services.AddCronJobs<TestFlowContext>(TestUtils.Configuration)
            .AddMongoStore<TestFlowContext>();
    }
}
