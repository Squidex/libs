// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

using System.ComponentModel.DataAnnotations;

namespace Squidex.AI.Mongo;

public sealed class EFChatEntity
{
    public string Id { get; set; }

    public string Value { get; set; }

    public DateTime LastUpdated { get; set; }

    [ConcurrencyCheck]
    public Guid Version { get; set; }
}
