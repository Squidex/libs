// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

using Microsoft.Extensions.Hosting;
using Squidex.Log;

namespace Squidex.Hosting
{
    public sealed class InitializerHost : SystemHost<IInitializable>, IHostedService
    {
        public InitializerHost(ISemanticLog log, IEnumerable<IInitializable> systems)
            : base(log, systems)
        {
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            if (!Systems.Any())
            {
                return;
            }

            Log.LogInformation(w => w
                .WriteArray("initialize", array =>
                {
                    foreach (var (_, name) in Systems)
                    {
                        array.WriteValue(name);
                    }
                }));

            foreach (var (system, name) in Systems)
            {
                await system.InitializeAsync(cancellationToken);

                Log.LogInformation(w => w.WriteProperty("initializedSystem", name));
            }
        }

        public async Task StopAsync(CancellationToken cancellationToken)
        {
            foreach (var (system, name) in Systems.Reverse())
            {
                try
                {
                    await system.ReleaseAsync(cancellationToken);

                    Log.LogInformation(w => w.WriteProperty("releasedSystem", name));
                }
                catch (Exception ex)
                {
                    Log.LogError(ex, w => w.WriteProperty("releasedSystemFailed", name));
                }
            }
        }
    }
}
