// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

using Testcontainers.RabbitMq;
using Xunit;

namespace Squidex.Messaging;

public class RabbitMqFixture : IAsyncLifetime
{
    public RabbitMqContainer RabbitMq { get; } = new RabbitMqBuilder().Build();

    public async Task DisposeAsync()
    {
        await RabbitMq.StopAsync();
    }

    public async Task InitializeAsync()
    {
        await RabbitMq.StartAsync();
    }
}
