// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

using NodaTime;

namespace Squidex.Flows.CronJobs.Internal;

public sealed record CronJobEntry<T>
{
    required public CronJob<T> Job { get; set; }

    required public Instant NextTime { get; set; }
}
