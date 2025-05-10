// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

using Squidex.Flows.CronJobs.Internal;

namespace Squidex.Flows.CronJobs;

public class NodaCronTimezoneProviderTests
{
    private readonly NodaCronTimezoneProvider sut = new NodaCronTimezoneProvider();

    [Fact]
    public void Should_provide_timezones()
    {
        var zones = sut.GetAvailableIds();

        Assert.NotEmpty(zones);
    }

    public sealed class TimezonesData : TheoryData<string>
    {
        public TimezonesData()
        {
            var zones = new NodaCronTimezoneProvider();

            AddRange(zones.GetAvailableIds().ToArray());
        }
    }

    [Theory]
    [ClassData(typeof(TimezonesData))]
    public void Should_parse_timezone(string zoneIdentifier)
    {
        var parsed = sut.TryParse(zoneIdentifier, out var timezone);

        Assert.True(parsed);
        Assert.NotNull(timezone);
    }
}
