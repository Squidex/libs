﻿// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

using Squidex.Hosting.Configuration;

namespace Squidex.Messaging.EntityFramework;

public sealed class EFTransportOptions : IValidatableOptions
{
    public TimeSpan UpdateInterval { get; set; } = TimeSpan.FromSeconds(5);

    public TimeSpan PollingInterval { get; set; } = TimeSpan.FromSeconds(1);

    public TimeSpan SubscriptionExpiration { get; set; } = TimeSpan.FromSeconds(30);

    public IEnumerable<ConfigurationError> Validate()
    {
        if (UpdateInterval < TimeSpan.Zero || UpdateInterval > TimeSpan.FromMinutes(10))
        {
            yield return new ConfigurationError("Value must be between 00:00:00 and 00:10:00.", nameof(UpdateInterval));
        }

        if (PollingInterval < TimeSpan.Zero || PollingInterval > TimeSpan.FromMinutes(10))
        {
            yield return new ConfigurationError("Value must be between 00:00:00 and 00:10:00.", nameof(PollingInterval));
        }
    }
}
