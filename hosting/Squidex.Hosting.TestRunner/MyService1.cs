// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

namespace Squidex.Hosting
{
    internal class MyService1 : IBackgroundProcess, IInitializable
    {
        public Task InitializeAsync(CancellationToken ct)
        {
            return Task.CompletedTask;
        }

        public Task StartAsync(CancellationToken ct)
        {
            return Task.CompletedTask;
        }
    }
}
