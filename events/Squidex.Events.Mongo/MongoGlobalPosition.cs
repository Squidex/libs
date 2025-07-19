// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

namespace Squidex.Events.Mongo;

internal sealed class MongoGlobalPosition
{
    public long Id { get; set; }

    public long Position { get; set; }

    public DateTimeOffset Expired { get; set; }

    public Guid OwnerId { get; set; }
}
