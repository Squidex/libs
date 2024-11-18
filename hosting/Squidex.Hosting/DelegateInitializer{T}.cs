// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

using Microsoft.Extensions.DependencyInjection;

namespace Squidex.Hosting;

public sealed class DelegateInitializer<T>(IServiceProvider serviceProvider, string name, int order, Func<T, CancellationToken, Task> action) : IInitializable where T : class
{
    public string Name => name;

    public int Order => order;

    public Task InitializeAsync(
        CancellationToken ct)
    {
        var service = serviceProvider.GetRequiredService<T>();

        return action(service, ct);
    }
}
