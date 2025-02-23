// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

using Microsoft.EntityFrameworkCore;
using Squidex.AI;
using Squidex.AI.Implementation;
using Squidex.AI.Mongo;

namespace Microsoft.Extensions.DependencyInjection;

public static class EFChatServiceExtensions
{
    public static AIBuilder AddEntityFrameworkChatStore<T>(this AIBuilder builder)
        where T : DbContext
    {
        builder.Services.AddSingletonAs<EFChatStore<T>>()
            .As<IChatStore>();

        return builder;
    }
}
