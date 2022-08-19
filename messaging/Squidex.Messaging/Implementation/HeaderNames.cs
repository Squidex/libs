// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

namespace Squidex.Messaging.Implementation
{
    public static class HeaderNames
    {
        public const string Id = "messaging.id";

        public const string Type = "messaging.type";

        public const string TimeCreated = "messaging.created";

        public const string TimeExpires = "messaging.expires";

        public const string TimeTimeout = "messaging.retry";
    }
}
