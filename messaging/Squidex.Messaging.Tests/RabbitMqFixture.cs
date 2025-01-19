// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

using System.Diagnostics;
using Testcontainers.RabbitMq;
using Xunit;

namespace Squidex.Messaging;

public class RabbitMqFixture : IAsyncLifetime
{
    public RabbitMqContainer RabbitMq { get; } =
        new RabbitMqBuilder()
            .WithReuse(Debugger.IsAttached)
            .WithLabel("reuse-id", "messaging-rabbit")
            .Build();

    public async Task InitializeAsync()
    {
        await RabbitMq.StartAsync();
    }

    public async Task DisposeAsync()
    {
        await RabbitMq.StopAsync();
    }
}
