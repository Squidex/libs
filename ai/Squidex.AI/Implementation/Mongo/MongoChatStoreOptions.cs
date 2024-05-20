// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

using Squidex.Hosting.Configuration;

namespace Squidex.AI.Implementation.Mongo;

public sealed class MongoChatStoreOptions : IValidatableOptions
{
    public string CollectionName { get; set; } = "Chat";

    public IEnumerable<ConfigurationError> Validate()
    {
        if (string.IsNullOrWhiteSpace(CollectionName))
        {
            yield return new ConfigurationError("Value is required.", nameof(CollectionName));
        }
    }
}
