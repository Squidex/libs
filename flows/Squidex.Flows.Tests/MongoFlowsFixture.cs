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

[CollectionDefinition(Name)]
public class MongoFlowsCollection : ICollectionFixture<MongoFlowsFixture>
{
    public const string Name = "flows-mongo";
}
