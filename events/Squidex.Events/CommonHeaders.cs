// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

namespace Squidex.Events;

public static class CommonHeaders
{
    public static readonly string CommitId = nameof(CommitId);

    public static readonly string EventId = nameof(EventId);

    public static readonly string EventStreamNumber = nameof(EventStreamNumber);

    public static readonly string Timestamp = nameof(Timestamp);
}
