// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

namespace Squidex.Hosting;

public interface IInitializable : ISystem
{
    bool IsOptional => false;

    Task InitializeAsync(
        CancellationToken ct);

    Task ReleaseAsync(
        CancellationToken ct)
    {
        return Task.CompletedTask;
    }
}
