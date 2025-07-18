﻿// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

using Microsoft.EntityFrameworkCore;
using TestHelpers;

namespace Squidex.Messaging;

public class EFMessagingTests(EFMessagingFixture fixture)
    : MessagingTestsBase, IClassFixture<EFMessagingFixture>
{
    protected override void Configure(MessagingBuilder builder)
    {
        builder.Services.AddDbContextFactory<TestDbContext>(b =>
        {
            b.UseNpgsql(fixture.PostgreSql.GetConnectionString());
        });

        builder.AddEntityFrameworkDataStore<TestDbContext>(TestUtils.Configuration);
        builder.AddEntityFrameworkTransport<TestDbContext>(TestUtils.Configuration, options =>
        {
            options.PollingInterval = TimeSpan.FromSeconds(0.1);
            options.UpdateInterval = TimeSpan.FromSeconds(0.1);
        });
    }
}
