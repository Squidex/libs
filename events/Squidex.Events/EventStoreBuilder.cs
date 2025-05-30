﻿// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

using Microsoft.Extensions.DependencyInjection;

namespace Squidex.Events.EntityFramework;

public sealed class EventStoreBuilder(IServiceCollection services)
{
    public IServiceCollection Services { get; } = services;
}
