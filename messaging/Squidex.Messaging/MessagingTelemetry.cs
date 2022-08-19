// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

using System.Diagnostics;

namespace Squidex.Messaging
{
    public static class MessagingTelemetry
    {
        public static readonly ActivitySource Activities = new ActivitySource("Squidex.Messaging");
    }
}
