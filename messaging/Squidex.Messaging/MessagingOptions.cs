﻿// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

namespace Squidex.Messaging;

public sealed class MessagingOptions
{
    public RoutingCollection Routing { get; set; } = [];

    public RoutingCollection Topics { get; set; } = [];

    public TimeSpan DataCacheDuration { get; set; } = TimeSpan.FromSeconds(30);

    public TimeSpan DataAliveUpdateInterval { get; set; } = TimeSpan.FromSeconds(30);
}
