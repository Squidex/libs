// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Squidex.AI.EntityFramework;

public sealed class EFChatEntity
{
    public string Id { get; set; }

    public string Value { get; set; }

    public DateTime LastUpdated { get; set; }

    [ConcurrencyCheck]
    [Column("Version")]
    public Guid LastVersion { get; set; }
}
