// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

using Squidex.Messaging.Implementation;
using Xunit;

namespace Squidex.Messaging
{
    public abstract class SubscriptionStoreTestsBase
    {
        private readonly DateTime now = DateTime.UtcNow;
        private readonly string queue1 = $"queue1_{Guid.NewGuid()}";
        private readonly string queue2 = $"queue2_{Guid.NewGuid()}";
        private readonly string topic = $"topic_{Guid.NewGuid()}";

        public abstract Task<ISubscriptionStore> CreateSubscriptionStoreAsync();

        [Fact]
        public async Task Should_subscribe()
        {
            var sut = await CreateSubscriptionStoreAsync();

            await sut.SubscribeAsync(topic, queue1, now, TimeSpan.FromDays(30), default);
            await sut.SubscribeAsync(topic, queue2, now, TimeSpan.FromDays(30), default);

            SetEquals(new[] { queue1, queue2 }.ToHashSet(), await sut.GetSubscriptionsAsync(topic, now, default));
        }

        [Fact]
        public async Task Should_unsubscribe()
        {
            var sut = await CreateSubscriptionStoreAsync();

            await sut.SubscribeAsync(topic, queue1, now, TimeSpan.FromDays(30), default);
            await sut.SubscribeAsync(topic, queue2, now, TimeSpan.FromDays(30), default);

            await sut.UnsubscribeAsync(topic, queue1, default);

            SetEquals(new[] { queue2 }, await sut.GetSubscriptionsAsync(topic, now, default));
        }

        [Fact]
        public async Task Should_not_return_expired_subscriptions()
        {
            var sut = await CreateSubscriptionStoreAsync();

            await sut.SubscribeAsync(topic, queue1, now, TimeSpan.FromDays(30), default);
            await sut.SubscribeAsync(topic, queue2, now, TimeSpan.FromSeconds(30), default);

            SetEquals(new[] { queue1 }, await sut.GetSubscriptionsAsync(topic, now.AddDays(1), default));
        }

        [Fact]
        public async Task Should_update_expiration()
        {
            var sut = await CreateSubscriptionStoreAsync();

            await sut.SubscribeAsync(topic, queue1, now, TimeSpan.FromDays(30), default);
            await sut.SubscribeAsync(topic, queue2, now, TimeSpan.FromSeconds(30), default);

            // Does not expires because last activity is in the future.
            await sut.UpdateAliveAsync(new[] { queue2 }, now.AddDays(2), default);

            SetEquals(new[] { queue1, queue2 }, await sut.GetSubscriptionsAsync(topic, now.AddDays(1), default));
        }

        [Fact]
        public async Task Should_cleanup_subscriptions()
        {
            var sut = await CreateSubscriptionStoreAsync();

            await sut.SubscribeAsync(topic, queue1, now, TimeSpan.FromDays(30), default);
            await sut.SubscribeAsync(topic, queue2, now, TimeSpan.FromDays(10), default);

            // Expires in the future to force expiration.
            await sut.CleanupAsync(now.AddDays(20), default);

            SetEquals(new[] { queue1 }, await sut.GetSubscriptionsAsync(topic, now.AddDays(1), default));
        }

        [Fact]
        public async Task Should_not_cleanup_subscriptions_that_never_expires()
        {
            var sut = await CreateSubscriptionStoreAsync();

            await sut.SubscribeAsync(topic, queue1, now, TimeSpan.Zero, default);
            await sut.SubscribeAsync(topic, queue2, now, TimeSpan.Zero, default);

            // Expires in the future to force expiration.
            await sut.CleanupAsync(now.AddDays(30), default);

            SetEquals(new[] { queue1, queue2 }, await sut.GetSubscriptionsAsync(topic, now, default));
        }

        private static void SetEquals<T>(IEnumerable<T> expected, IEnumerable<T> actual)
        {
            Assert.Equal(expected.OrderBy(x => x).ToArray(), actual.OrderBy(x => x).ToArray());
        }
    }
}
