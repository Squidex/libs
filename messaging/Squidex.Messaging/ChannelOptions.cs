// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

using Squidex.Messaging.Implementation.Scheduler;
using Squidex.Messaging.Internal;

#pragma warning disable MA0048 // File name must match type name

namespace Squidex.Messaging;

public delegate IMessagingTransport TransportSelector(IEnumerable<IMessagingTransport> transports, ChannelName name);

public sealed class ChannelOptions : ProducerOptions
{
    public int NumSubscriptions { get; set; } = 1;

    public Func<object, bool>? LogMessage { get; set; }

    public TransportSelector? TransportSelector { get; set; }

    public IScheduler Scheduler { get; set; } = InlineScheduler.Instance;

    public IMessagingTransport SelectTransport(IEnumerable<IMessagingTransport> transports, ChannelName name)
    {
        var result = TransportSelector?.Invoke(transports, name);

        if (result == null)
        {
            result = transports.LastOrDefault();
        }

        if (result == null)
        {
            ThrowHelper.InvalidOperationException("No transport configured.");
        }

        return result!;
    }
}
