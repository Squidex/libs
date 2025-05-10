// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

namespace Squidex.Flows.CronJobs;

public sealed class CronJobsOptions
{
    public TimeSpan UpdateInterval { get; set; } = TimeSpan.FromMinutes(10);

    public TimeSpan UpdateLimit { get; set; } = TimeSpan.FromMinutes(5);
}
