// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

namespace Squidex.AI;

public sealed class ChatConfiguration
{
    public string[]? SystemMessages { get; set; }

    public string[]? Tools { get; set; }
}
