// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

using System.Security.Claims;

namespace Squidex.AI;

public class ChatContext
{
    public ClaimsPrincipal? User { get; set; } = ClaimsPrincipal.Current;

    public Dictionary<string, object> Data { get; set; } = [];
}
