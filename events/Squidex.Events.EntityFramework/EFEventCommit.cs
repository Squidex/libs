// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Squidex.Events.EntityFramework;

[Table("Events")]
public sealed class EFEventCommit
{
    [Key]
    public Guid Id { get; set; }

    public string EventStream { get; set; }

    public long EventStreamOffset { get; set; }

    public long EventsCount { get; set; }

    public string[] Events { get; set; }

    public DateTime Timestamp { get; set; }

    public long? Position { get; set; } = null!;
}
