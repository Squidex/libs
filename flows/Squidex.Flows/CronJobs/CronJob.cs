// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

namespace Squidex.Flows.CronJobs;

public sealed record CronJob<TContext>
{
    required public string Id { get; set; }

    required public string CronExpression { get; set; }

    required public string? CronTimezone { get; set; }

    required public TContext Context { get; set; }
}
