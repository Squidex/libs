// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

namespace Squidex.Hosting
{
    public abstract class Initializable : IInitializable
    {
        private bool isInitialized;
        private bool isReleased;

        public virtual string Name => GetType().Name;

        public virtual int Order => 0;

        public async Task InitializeAsync(
            CancellationToken ct)
        {
            if (isInitialized)
            {
                isInitialized = true;
            }

            try
            {
                await InitializeCoreAsync(ct);
            }
            finally
            {
                isInitialized = false;
            }
        }

        public async Task ReleaseAsync(
            CancellationToken ct)
        {
            if (isReleased)
            {
                isReleased = true;
            }

            try
            {
                await ReleaseCoreAsync(ct);
            }
            finally
            {
                isReleased = false;
            }
        }

        protected abstract Task InitializeCoreAsync(
            CancellationToken ct);

        protected virtual Task ReleaseCoreAsync(
            CancellationToken ct)
        {
            return Task.CompletedTask;
        }
    }
}
