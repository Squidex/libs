// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

using Squidex.Hosting.Configuration;

namespace Squidex.Events.Mongo;

public sealed class MongoEventStoreOptions : IValidatableOptions
{
    public TimeSpan PollingInterval { get; set; } = TimeSpan.FromSeconds(5);

    public string CollectionName { get; set; } = "Events2";

    public string PositionCollectionName { get; set; } = "Event2Position";

    public bool UseChangeStreams { get; set; }

    public IEnumerable<ConfigurationError> Validate()
    {
        if (PollingInterval < TimeSpan.Zero || PollingInterval > TimeSpan.FromMinutes(10))
        {
            yield return new ConfigurationError("Value must be between 00:00:00 and 00:10:00.", nameof(PollingInterval));
        }
    }
}
