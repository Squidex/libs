// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

using System.Net;

namespace Squidex.Messaging.Implementation
{
    public sealed class HostNameInstanceNameProvider : IInstanceNameProvider
    {
        public string Name { get; } = Dns.GetHostName();
    }
}
