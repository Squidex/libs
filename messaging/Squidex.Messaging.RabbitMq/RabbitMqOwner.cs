// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

using Microsoft.Extensions.Options;
using RabbitMQ.Client;

namespace Squidex.Messaging.RabbitMq;

public sealed class RabbitMqOwner
{
    public IConnection Connection { get; }

    public RabbitMqTransportOptions Options { get; }

    public RabbitMqOwner(IOptions<RabbitMqTransportOptions> options)
    {
        Options = options.Value;

        var connectionFactory = new ConnectionFactory
        {
            Uri = options.Value.Uri,

            // Of course we want an asynchronous behavior.
            DispatchConsumersAsync = true
        };

        Connection = connectionFactory.CreateConnection();
    }
}
