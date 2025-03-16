// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

using TestHelpers.EntityFramework;

namespace Squidex.Messaging;

public sealed class EFMessagingFixture() : PostgresFixture<TestDbContext>("messaging-postgres")
{
    protected override void AddServices(IServiceCollection services)
    {
    }
}
