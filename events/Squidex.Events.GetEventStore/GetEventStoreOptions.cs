// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

using EventStore.Client;
using Squidex.Hosting.Configuration;

namespace Squidex.Events.GetEventStore;

public sealed class GetEventStoreOptions : IValidatableOptions
{
    public Func<EventStoreProjectionManagementClient, string, Task>? WaitTimeAfterProjection { get; set; }

    public string Prefix { get; set; } = "squidex";

    public long ProgressDone { get; set; } = 95;

    public IEnumerable<ConfigurationError> Validate()
    {
        if (string.IsNullOrWhiteSpace(Prefix))
        {
            yield return new ConfigurationError("Value is required.", nameof(Prefix));
        }
    }
}
