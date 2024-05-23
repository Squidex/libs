// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Squidex.Hosting;

namespace Squidex.Messaging.Implementation;

public sealed class MessagingDataProvider : IMessagingDataProvider, IBackgroundProcess
{
    private readonly Dictionary<(string Group, string Key), (SerializedObject Value, TimeSpan Expires)> localData = [];
    private readonly MessagingOptions options;
    private readonly IMessagingDataStore messagingDataStore;
    private readonly IMessagingSerializer messagingSerializer;
    private readonly ILogger<MessagingDataProvider> log;
    private readonly TimeProvider timeProvider;
    private SimpleTimer? updateTimer;

    public MessagingDataProvider(
        IMessagingDataStore messagingDataStore,
        IMessagingSerializer messagingSerializer,
        IOptions<MessagingOptions> options,
        TimeProvider timeProvider,
        ILogger<MessagingDataProvider> log)
    {
        this.options = options.Value;
        this.messagingDataStore = messagingDataStore;
        this.messagingSerializer = messagingSerializer;
        this.timeProvider = timeProvider;
        this.log = log;
    }

    public Task StartAsync(
        CancellationToken ct)
    {
        // Just a guard when this method is called twice.
        updateTimer ??= new SimpleTimer(UpdateAliveAsync, options.DataAliveUpdateInterval, log);

        return Task.CompletedTask;
    }

    public async Task StopAsync(
        CancellationToken ct)
    {
        if (updateTimer != null)
        {
            await updateTimer.DisposeAsync();

            updateTimer = null;
        }
    }

    public Task UpdateAliveAsync(
        CancellationToken ct = default)
    {
        KeyValuePair<(string Group, string Key), (SerializedObject Value, TimeSpan Expires)>[] localEntries;

        lock (localData)
        {
            localEntries = localData.ToArray();
        }

        if (localEntries.Length == 0)
        {
            return Task.CompletedTask;
        }

        var now = timeProvider.GetUtcNow().UtcDateTime;

        var requests =
            localEntries
                .Select(x =>
                    new Entry(
                        x.Key.Group,
                        x.Key.Key,
                        x.Value.Value,
                        CalculateExpiration(now, x.Value.Expires)))
                    .ToArray();

        return messagingDataStore.StoreManyAsync(requests, ct);
    }

    public async Task<IReadOnlyDictionary<string, T>> GetEntriesAsync<T>(string group,
        CancellationToken ct = default) where T : notnull
    {
        var result = new Dictionary<string, T>();

        var now = timeProvider.GetUtcNow().UtcDateTime;

        foreach (var entry in await messagingDataStore.GetEntriesAsync(group, ct))
        {
            // Check for expiration again.
            if (entry.Expiration < now)
            {
                await messagingDataStore.DeleteAsync(group, entry.Key, ct);
                continue;
            }

            var deserialized = messagingSerializer.Deserialize(entry.Value);

            // Ignore the message if the type does not match to the expected type.
            if (deserialized.Message is T typed)
            {
                result[entry.Key] = typed;
            }
        }

        return result;
    }

    public async Task<IAsyncDisposable> StoreAsync<T>(string group, string key, T value, TimeSpan expiresAfter,
        CancellationToken ct = default) where T : notnull
    {
        var serialized = messagingSerializer.Serialize(value);

        lock (localData)
        {
            // Store complete subscriptions as local copy to update them periodically. Otherwise we could loose information in race conditions.
            localData[(group, key)] = (serialized, expiresAfter);
        }

        var now = timeProvider.GetUtcNow().UtcDateTime;

        // The entry is already serialized, so the store can be dump.
        var entry = new Entry(group, key, serialized, CalculateExpiration(now, expiresAfter));

        await messagingDataStore.StoreManyAsync([entry], ct);

        return new DelegateAsyncDisposable(async () =>
        {
            await DeleteAsync(group, key, ct);
        });
    }

    public Task DeleteAsync(string group, string key,
        CancellationToken ct = default)
    {
        lock (localData)
        {
            localData.Remove((group, key));
        }

        return messagingDataStore.DeleteAsync(group, key, ct);
    }

    private static DateTime CalculateExpiration(DateTime now, TimeSpan expires)
    {
        if (expires <= TimeSpan.Zero)
        {
            return DateTime.MaxValue;
        }

        return now + expires;
    }
}
