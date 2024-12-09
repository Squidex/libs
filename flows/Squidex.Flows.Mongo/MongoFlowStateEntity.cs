// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

namespace Squidex.Flows.Mongo;

public sealed class MongoFlowStateEntity
{
    public Guid Id { get; set; }

    public string OwnerId { get; set; }

    public string DefinitionId { get; set; }

    public string State { get; set; }

    public DateTimeOffset? DueTime { get; set; }
}
