// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

using Squidex.Hosting.Configuration;

namespace Squidex.Messaging.GoogleCloud
{
    public sealed class GooglePubSubTransportOptions : IValidatableOptions
    {
        public string Prefix { get; set; }

        public string ProjectId { get; set; }

        public IEnumerable<ConfigurationError> Validate()
        {
            if (string.IsNullOrWhiteSpace(ProjectId))
            {
                yield return new ConfigurationError("Value is required.", nameof(ProjectId));
            }
        }
    }
}
