// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

using Squidex.Hosting.Configuration;

namespace Squidex.Messaging.Mongo;

public sealed class MongoSubscriptionStoreOptions : IValidatableOptions
{
    public string CollectionName { get; set; } = "Subscriptions";

    public IEnumerable<ConfigurationError> Validate()
    {
        if (string.IsNullOrWhiteSpace(CollectionName))
        {
            yield return new ConfigurationError("Value is required.", nameof(CollectionName));
        }
    }
}
