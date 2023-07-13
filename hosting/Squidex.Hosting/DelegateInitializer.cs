// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

namespace Squidex.Hosting;

public sealed class DelegateInitializer : IInitializable
{
    private readonly IServiceProvider serviceProvider;
    private readonly string name;
    private readonly int order;
    private readonly Func<IServiceProvider, CancellationToken, Task> action;

    public string Name => name;

    public int Order => order;

    public DelegateInitializer(IServiceProvider serviceProvider, string name, int order, Func<IServiceProvider, CancellationToken, Task> action)
    {
        this.serviceProvider = serviceProvider;
        this.name = name;
        this.order = order;
        this.action = action;
    }

    public Task InitializeAsync(
        CancellationToken ct)
    {
        return action(serviceProvider, ct);
    }
}
