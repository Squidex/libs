// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

namespace Squidex.AI;

public sealed record ChatResult
{
    required public ChatMetadata Metadata { get; init; }

    required public List<ToolStartEvent> ToolStarts { get; set; }

    required public List<ToolEndEvent> ToolEnds { get; set; }

    required public string Content { get; init; }
}
