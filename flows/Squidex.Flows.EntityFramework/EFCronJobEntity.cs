// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

namespace Squidex.Flows.EntityFramework;

public sealed class EFCronJobEntity
{
    public string Id { get; set; }

    public DateTimeOffset DueTime { get; set; }

    public string Data { get; set; }
}
