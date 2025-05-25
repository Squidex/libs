// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

using System.Diagnostics.CodeAnalysis;
using NodaTime;

namespace Squidex.Flows.CronJobs.Internal;

public sealed class NodaCronTimezoneProvider : ICronTimezoneProvider
{
    private readonly List<string> timezones = [];

    public NodaCronTimezoneProvider()
    {
        foreach (var id in DateTimeZoneProviders.Tzdb.Ids)
        {
            if (TimeZoneInfo.TryFindSystemTimeZoneById(id, out var _))
            {
                timezones.Add(id);
            }
        }
    }

    public IReadOnlyList<string> GetAvailableIds()
    {
        return timezones;
    }

    public bool TryParse(string id, [MaybeNullWhen(false)] out TimeZoneInfo timezone)
    {
        timezone = null;
        if (string.IsNullOrWhiteSpace(id))
        {
            return false;
        }

        return TimeZoneInfo.TryFindSystemTimeZoneById(id, out timezone);
    }
}
