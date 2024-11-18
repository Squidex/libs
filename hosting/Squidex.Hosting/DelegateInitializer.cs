// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

namespace Squidex.Hosting;

public sealed class DelegateInitializer(IServiceProvider serviceProvider, string name, int order, Func<IServiceProvider, CancellationToken, Task> action) : IInitializable
{
    public string Name => name;

    public int Order => order;

    public Task InitializeAsync(
        CancellationToken ct)
    {
        return action(serviceProvider, ct);
    }
}
