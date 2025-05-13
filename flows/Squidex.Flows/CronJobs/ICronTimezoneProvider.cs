// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

using System.Diagnostics.CodeAnalysis;

namespace Squidex.Flows.CronJobs;

public interface ICronTimezoneProvider
{
    bool TryParse(string id, [MaybeNullWhen(false)] out TimeZoneInfo timezone);

    IReadOnlyList<string> GetAvailableIds();
}
