// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

using EventStore.Client;
using Microsoft.Extensions.Options;
using Squidex.Text;

namespace Squidex.Events.GetEventStore;

public sealed class EventStoreProjectionClient(
    EventStoreClientSettings settings,
    IOptions<GetEventStoreOptions> options)
{
    private readonly Dictionary<string, Task> projections = [];
    private readonly SemaphoreSlim semaphoreSlim = new SemaphoreSlim(1);
    private readonly EventStoreProjectionManagementClient client = new EventStoreProjectionManagementClient(settings);
    private readonly string prefix = options.Value.Prefix;

    private string CreateFilterProjectionName(string filter)
    {
        return $"by-{prefix.Slugify()}-{filter.Slugify()}";
    }

    public async Task<string> CreateProjectionAsync(StreamFilter filter, bool waitForCompletion,
        CancellationToken ct)
    {
        if (filter.Kind == StreamFilterKind.MatchFull && filter.Prefixes?.Length == 1)
        {
            return $"{prefix}-{filter.Prefixes[0]}";
        }

        var projectionRegex = filter.ToRegex();
        var projectionName = CreateFilterProjectionName(projectionRegex);
        var query =
            $@"fromAll()
                    .when({{
                        $any: function (s, e) {{
                            if (e.streamId.indexOf('{prefix}') === 0 && /{projectionRegex}/.test(e.streamId.substring({prefix.Length + 1}))) {{
                                linkTo('{projectionName}', e);
                            }}
                        }}
                    }});";

        await CreateProjectionAsync(projectionName, query, waitForCompletion, ct);
        return projectionName;
    }

    private async Task CreateProjectionAsync(string name, string query, bool waitForCompletion,
        CancellationToken ct)
    {
        Task task;
        var isCreated = false;

        await semaphoreSlim.WaitAsync(ct);
        try
        {
            if (!projections.TryGetValue(name, out var cachedTask))
            {
                cachedTask = CreateProjectionCoreAsync(name, query, waitForCompletion, ct);
                projections[name] = cachedTask;

                isCreated = true;
            }

            task = cachedTask;
        }
        finally
        {
            semaphoreSlim.Release();
        }

        if (!isCreated)
        {
            await task;
            return;
        }

        try
        {
            await task;
        }
        catch
        {
            await semaphoreSlim.WaitAsync(ct);
            try
            {
                projections.Remove(name);
            }
            finally
            {
                semaphoreSlim.Release();
            }
        }
    }

    private async Task CreateProjectionCoreAsync(string name, string query, bool waitForCompletion,
        CancellationToken ct)
    {
        await client.CreateContinuousAsync(name, query, cancellationToken: ct);
        await client.UpdateAsync(name, query, true, cancellationToken: ct);

        var waiter = options.Value.WaitTimeAfterProjection;
        if (waiter != null)
        {
            await waiter(client, name);
        }

        if (!waitForCompletion)
        {
            return;
        }

        while (!ct.IsCancellationRequested)
        {
            ct.ThrowIfCancellationRequested();

            var status = await client.GetStatusAsync(name, cancellationToken: ct);
            if (status?.Status.Contains("Running", StringComparison.Ordinal) != true)
            {
                throw new InvalidOperationException("Projection is not running.");
            }

            if (status?.Progress >= options.Value.ProgressDone)
            {
                await Task.Delay(100, ct);
                break;
            }

            await Task.Delay(100, ct);
        }
    }
}
