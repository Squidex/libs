// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

namespace Squidex.Messaging.Subscriptions;

public sealed class SubscriptionOptions
{
    public TimeSpan ExpirationTime { get; set; } = TimeSpan.FromMinutes(30);

    public string GroupName { get; set; } = "__subscriptions";
}
