// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

namespace Squidex.AI.Mongo;

public sealed class MongoChatEntity
{
    public string Id { get; set; }

    public string Value { get; set; }

    public DateTime LastUpdated { get; set; }
}
