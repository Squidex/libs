// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

#pragma warning disable SA1313 // Parameter names should begin with lower-case letter

namespace Squidex.Events;

public sealed record EventCommit(Guid Id, string StreamName, long Offset, ICollection<EventData> Events)
{
    public static EventCommit Create(Guid id, string streamName, long offset, EventData @event)
    {
        return new EventCommit(id, streamName, offset, [@event]);
    }
}
