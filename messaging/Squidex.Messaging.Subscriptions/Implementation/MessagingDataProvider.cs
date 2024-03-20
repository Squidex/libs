// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Squidex.Hosting;
using Squidex.Messaging.Implementation;
using Squidex.Messaging.Internal;

namespace Squidex.Messaging.Subscriptions.Implementation;

public sealed class MessagingDataProvider : IMessagingDataProvider, IBackgroundProcess
{
    private readonly Dictionary<(string Group, string Key), (SerializedObject Value, TimeSpan Expires)> localSubscriptions = [];
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
        updateTimer ??= new SimpleTimer(UpdateAliveAsync, options.SubscriptionUpdateInterval, log);

        return Task.CompletedTask;
    }

    public Task UpdateAliveAsync(
        CancellationToken ct)
    {
        KeyValuePair<(string Group, string Key), (SerializedObject Value, TimeSpan Expires)>[] subscriptions;

        lock (localSubscriptions)
        {
            subscriptions = localSubscriptions.ToArray();
        }

        if (subscriptions.Length == 0)
        {
            return Task.CompletedTask;
        }

        var now = timeProvider.GetUtcNow().UtcDateTime;

        var requests =
            subscriptions
                .Select(x =>
                    new Entry(
                        x.Key.Group,
                        x.Key.Key,
                        x.Value.Value,
                        CalculateExpiration(now, x.Value.Expires)))
                    .ToArray();

        return messagingDataStore.StoreManyAsync(requests, ct);
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

    public async Task<IReadOnlyDictionary<string, T>> GetEntriesAsync<T>(string group,
        CancellationToken ct = default) where T : notnull
    {
        var result = new Dictionary<string, T>();

        var now = timeProvider.GetUtcNow().UtcDateTime;

        foreach (var subscription in await messagingDataStore.GetEntriesAsync(group, ct))
        {
            if (subscription.Expiration < now)
            {
                await messagingDataStore.DeleteAsync(group, subscription.Key, ct);
                continue;
            }

            var deserialized = messagingSerializer.Deserialize(subscription.Value);

            if (deserialized.Message is T typed)
            {
                result[subscription.Key] = typed;
            }
        }

        return result;
    }

    public async Task<IAsyncDisposable> StoreAsync<T>(string group, string key, T value, TimeSpan expiresAfter,
        CancellationToken ct = default) where T : notnull
    {
        var serialized = messagingSerializer.Serialize(value);

        lock (localSubscriptions)
        {
            // Store complete subscriptions as local copy to update them periodically. Otherwise we could loose information in race conditions.
            localSubscriptions[(group, key)] = (serialized, expiresAfter);
        }

        var now = timeProvider.GetUtcNow().UtcDateTime;

        var subscription = new Entry(group, key, serialized, CalculateExpiration(now, expiresAfter));

        await messagingDataStore.StoreManyAsync([subscription], ct);

        return new DelegateAsyncDisposable(async () =>
        {
            await DeleteAsync(group, key, ct);
        });
    }

    public Task DeleteAsync(string group, string key,
        CancellationToken ct = default)
    {
        lock (localSubscriptions)
        {
            localSubscriptions.Remove((group, key));
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
