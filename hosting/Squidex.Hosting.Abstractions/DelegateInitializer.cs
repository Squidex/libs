// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

namespace Squidex.Hosting;

public sealed class DelegateInitializer : SystemBase, IInitializable
{
    private readonly Func<CancellationToken, Task> action;

    public DelegateInitializer(string name, Func<CancellationToken, Task> action)
        : base(name, 0)
    {
        this.action = action;
    }

    public async Task InitializeAsync(
        CancellationToken ct)
    {
        if (action != null)
        {
            await action(ct);
        }
    }
}
