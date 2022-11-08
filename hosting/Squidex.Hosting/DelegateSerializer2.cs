// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

namespace Squidex.Hosting;

public sealed class DelegateSerializer2 : SystemBase, IInitializable
{
    private readonly IServiceProvider serviceProvider;
    private readonly Func<IServiceProvider, CancellationToken, Task> action;

    public DelegateSerializer2(IServiceProvider serviceProvider, string name, int order, Func<IServiceProvider, CancellationToken, Task> action)
        : base(name, order)
    {
        this.serviceProvider = serviceProvider;

        this.action = action;
    }

    public Task InitializeAsync(
        CancellationToken ct)
    {
        return action(serviceProvider, ct);
    }
}
