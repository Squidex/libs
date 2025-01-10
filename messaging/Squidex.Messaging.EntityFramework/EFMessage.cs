// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

using System.ComponentModel.DataAnnotations;
using System.Text.Json;

namespace Squidex.Messaging.EntityFramework;

public sealed class EFMessage
{
    public string Id { get; init; }

    public string ChannelName { get; init; }

    public string QueueName { get; init; }

    public byte[] MessageData { get; init; }

    public string MessageHeaders { get; init; }

    public DateTime TimeToLive { get; init; }

    public DateTime? TimeHandled { get; set; }

    [ConcurrencyCheck]
    public Guid Version { get; set; }

    public TransportResult ToTransportResult()
    {
        var message =
            new TransportMessage(
                MessageData,
                null,
                JsonSerializer.Deserialize<TransportHeaders>(MessageHeaders)!);

        return new TransportResult(message, Id);
    }
}
