// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

using Microsoft.Extensions.DependencyInjection;

namespace Squidex.Hosting;

public sealed class DelegateSerializer2<T> : SystemBase, IInitializable where T : class
{
    private readonly IServiceProvider serviceProvider;
    private readonly Func<T, CancellationToken, Task> action;

    public DelegateSerializer2(IServiceProvider serviceProvider, string name, int order, Func<T, CancellationToken, Task> action)
        : base(name, order)
    {
        this.serviceProvider = serviceProvider;

        this.action = action;
    }

    public Task InitializeAsync(
        CancellationToken ct)
    {
        var service = serviceProvider.GetRequiredService<T>();

        return action(service, ct);
    }
}
