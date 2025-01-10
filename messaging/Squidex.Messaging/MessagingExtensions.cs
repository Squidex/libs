// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

using Squidex.Messaging.Implementation;

namespace Squidex.Messaging;

public static class MessagingExtensions
{
    public static DateTime GetTimeToLive(this TransportHeaders headers, TimeProvider? timeProvider = null)
    {
        var time = TimeSpan.FromDays(30);

        if (headers.TryGetTimestamp(HeaderNames.TimeExpires, out var expires))
        {
            time = expires;
        }

        timeProvider ??= TimeProvider.System;

        return timeProvider.GetUtcNow().UtcDateTime + time;
    }
}
