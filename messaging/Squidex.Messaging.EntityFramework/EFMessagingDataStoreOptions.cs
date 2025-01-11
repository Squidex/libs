// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

namespace Squidex.Messaging.EntityFramework;

public class EFMessagingDataStoreOptions
{
    public TimeSpan CleanupTime { get; set; } = TimeSpan.FromMinutes(10);
}
