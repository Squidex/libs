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
    public sealed class BackgroundHost : SystemHost<IBackgroundProcess>, IHostedService
    {
        public BackgroundHost(ISemanticLog log, IEnumerable<IBackgroundProcess> systems)
            : base(log, systems)
        {
        }

        public async Task StartAsync(
            CancellationToken cancellationToken)
        {
            if (!Systems.Any())
            {
                return;
            }

            Log.LogInformation(w => w
                .WriteArray("start", array =>
                {
                    foreach (var (_, name) in Systems)
                    {
                        array.WriteValue(name);
                    }
                }));

            foreach (var (system, name) in Systems)
            {
                await system.StartAsync(cancellationToken);

                Log.LogInformation(w => w.WriteProperty("startedSystem", name));
            }
        }

        public async Task StopAsync(
            CancellationToken cancellationToken)
        {
            foreach (var (system, name) in Systems.Reverse())
            {
                try
                {
                    await system.StopAsync(cancellationToken);

                    Log.LogInformation(w => w.WriteProperty("stoppedSystem", name));
                }
                catch (Exception ex)
                {
                    Log.LogError(ex, w => w.WriteProperty("stoppedSystemFailed", name));
                }
            }
        }
    }
}
