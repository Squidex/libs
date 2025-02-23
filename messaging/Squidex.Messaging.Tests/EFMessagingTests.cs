// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Squidex.Messaging;

public class EFMessagingTests(EFMessagingFixture fixture)
    : MessagingTestsBase, IClassFixture<EFMessagingFixture>
{
    protected override void Configure(MessagingBuilder builder)
    {
        builder.Services.AddDbContextFactory<EFMessagingFixture.TestContext>(b =>
        {
            b.UseNpgsql(fixture.PostgresSql.GetConnectionString());
        });

        builder.AddEntityFrameworkDataStore<EFMessagingFixture.TestContext>(TestHelpers.Configuration);
        builder.AddEntityFrameworkTransport<EFMessagingFixture.TestContext>(TestHelpers.Configuration, options =>
        {
            options.PollingInterval = TimeSpan.FromSeconds(0.1);
            options.UpdateInterval = TimeSpan.FromSeconds(0.1);
        });
    }
}
