// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

using Microsoft.Extensions.DependencyInjection;

namespace Squidex.Hosting;

public sealed class DelegateInitializer<T> : IInitializable where T : class
{
    private readonly IServiceProvider serviceProvider;
    private readonly string name;
    private readonly int order;
    private readonly Func<T, CancellationToken, Task> action;

    public string Name => name;

    public int Order => order;

    public DelegateInitializer(IServiceProvider serviceProvider, string name, int order, Func<T, CancellationToken, Task> action)
    {
        this.serviceProvider = serviceProvider;
        this.name = name;
        this.order = order;
        this.action = action;
    }

    public Task InitializeAsync(
        CancellationToken ct)
    {
        var service = serviceProvider.GetRequiredService<T>();

        return action(service, ct);
    }
}
