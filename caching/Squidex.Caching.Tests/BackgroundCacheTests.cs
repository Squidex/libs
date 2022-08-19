// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using Xunit;

namespace Squidex.Caching
{
    public class BackgroundCacheTests
    {
        private readonly Random random = new Random();
        private readonly BackgroundCache sut = new BackgroundCache(CreateMemoryCache());
        private int factoryInvoked;
        private DateTimeOffset now;

        public BackgroundCacheTests()
        {
            now = DateTimeOffset.UtcNow;

            sut.Clock = () =>
            {
                return now;
            };
        }

        [Fact]
        public async Task Should_get_from_factory_if_not_found()
        {
            var value1 = await GetOrCreateValue();
            var value2 = await GetOrCreateValue();

            Assert.Equal(11, value1);
            Assert.Equal(11, value2);
            Assert.Equal(1, factoryInvoked);
        }

        [Fact]
        public async Task Should_recreate_in_background_if_invalid()
        {
            var value1 = await GetOrCreateValue();
            var value2 = await GetOrCreateValue(isValid: false);

            await Task.Delay(500);

            var value3 = await GetOrCreateValue();

            Assert.Equal(11, value1);
            Assert.Equal(11, value2);
            Assert.Equal(12, value3);
            Assert.Equal(2, factoryInvoked);
        }

        [Fact]
        public async Task Should_get_from_factory_if_not_found_and_create_once()
        {
            var values = await Task.WhenAll(Enumerable.Repeat(1, 20).Select(x => GetOrCreateValue()));

            foreach (var value in values)
            {
                Assert.Equal(11, value);
            }

            Assert.Equal(1, factoryInvoked);
        }

        [Fact]
        public async Task Should_recreate_in_background()
        {
            var initialValues = await Task.WhenAll(Enumerable.Repeat(1, 20).Select(x => GetOrCreateValue()));

            foreach (var value in initialValues)
            {
                Assert.Equal(11, value);
            }

            now += TimeSpan.FromMinutes(58);

            var valuesAfterExpiration = await Task.WhenAll(Enumerable.Repeat(1, 20).Select(x => GetOrCreateValue()));

            foreach (var value in valuesAfterExpiration)
            {
                Assert.Equal(11, value);
            }

            await Task.Delay(500);

            var valuesAfterRecreation = await Task.WhenAll(Enumerable.Repeat(1, 20).Select(x => GetOrCreateValue()));

            foreach (var value in valuesAfterRecreation)
            {
                Assert.Equal(12, value);
            }

            Assert.Equal(2, factoryInvoked);
        }

        private Task<int> GetOrCreateValue(bool isValid = true)
        {
            return sut.GetOrCreateAsync("BackgroundKey", TimeSpan.FromHours(1), async x =>
            {
                Interlocked.Increment(ref factoryInvoked);

                await Task.Delay(random.Next(50 + 100));

                return factoryInvoked + 10;
            }, x => Task.FromResult(isValid));
        }

        private static MemoryCache CreateMemoryCache()
        {
            return new MemoryCache(Options.Create(new MemoryCacheOptions()));
        }
    }
}
