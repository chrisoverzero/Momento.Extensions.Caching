// Copyright 2024 Cimpress, Inc.
//
// Licensed under the Apache License, Version 2.0 (the "License") –
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//  http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

namespace Momento.Extensions.Caching.Unit;

/// <summary>Tests of the Set operation, async and sync.</summary>
[Properties(Arbitrary = [typeof(Generators)], MaxTest = 1024, QuietOnSuccess = true)]
public static partial class Set
{
    [Property(DisplayName = "Any set uses the provided value.")]
    public static async Task SetUsesProvidedValueAsync(
        IOptionsMonitor<MomentoCacheOptions> cacheOpts,
        TimeProvider time,
        NonNull<string> key,
        NonEmptyArray<byte> value,
        DistributedCacheEntryOptions entryOpts)
    {
        var cacheClient = Substitute.For<ICacheClient>();
        IDistributedCache sut = new MomentoCache(cacheClient, cacheOpts, time);

        _ = sut.SetAsync(key.Get, value.Get, entryOpts);

        _ = await cacheClient.Received().DictionarySetFieldsAsync(
            cacheOpts.CurrentValue.CacheName,
            key.Get,
            Arg.Is<Items>(ps => ps.Any(p => p.Key == ValueKey && p.Value.SequenceEqual(value.Get))),
            Arg.Any<CollectionTtl>());
    }

    [Property(DisplayName = "Any set uses the provided value, synchronously.")]
    public static void SetUsesProvidedValue(
        IOptionsMonitor<MomentoCacheOptions> cacheOpts,
        TimeProvider time,
        NonNull<string> key,
        NonEmptyArray<byte> value,
        DistributedCacheEntryOptions entryOpts)
    {
        var cacheClient = Substitute.For<ICacheClient>();
        IDistributedCache sut = new MomentoCache(cacheClient, cacheOpts, time);

        sut.Set(key.Get, value.Get, entryOpts);

        _ = cacheClient.Received().DictionarySetFieldsAsync(
            cacheOpts.CurrentValue.CacheName,
            key.Get,
            Arg.Is<Items>(ps => ps.Any(p => p.Key == ValueKey && p.Value.SequenceEqual(value.Get))),
            Arg.Any<CollectionTtl>());
    }

    [Property(DisplayName = "A set with default entry options uses the cache's default TTL.")]
    public static async Task DefaultDefaultTtlAsync(
        IOptionsMonitor<MomentoCacheOptions> cacheOpts,
        TimeProvider time,
        NonNull<string> key,
        NonEmptyArray<byte> value)
    {
        var cacheClient = Substitute.For<ICacheClient>();
        IDistributedCache sut = new MomentoCache(cacheClient, cacheOpts, time);

        await sut.SetAsync(key.Get, value.Get, new());

        _ = await cacheClient.Received().DictionarySetFieldsAsync(
            cacheOpts.CurrentValue.CacheName,
            key.Get,
            Arg.Any<Items>(),
            CollectionTtl.FromCacheTtl());
    }

    [Property(DisplayName = "A set with default entry options uses the cache's default TTL, synchronously.")]
    public static void DefaultDefaultTtl(
        IOptionsMonitor<MomentoCacheOptions> cacheOpts,
        TimeProvider time,
        NonNull<string> key,
        NonEmptyArray<byte> value)
    {
        var cacheClient = Substitute.For<ICacheClient>();
        IDistributedCache sut = new MomentoCache(cacheClient, cacheOpts, time);

        sut.Set(key.Get, value.Get, new());

        _ = cacheClient.Received().DictionarySetFieldsAsync(
            cacheOpts.CurrentValue.CacheName,
            key.Get,
            Arg.Any<Items>(),
            CollectionTtl.FromCacheTtl());
    }

    [Property(
        Arbitrary = [typeof(Generators.Fixed.Absolute)],
        DisplayName = "A set with an absolute expiration uses the time between now and then.")]
    public static async Task AbsoluteDifferenceAsync(
        IOptionsMonitor<MomentoCacheOptions> cacheOpts,
        TimeProvider time,
        NonNull<string> key,
        NonEmptyArray<byte> value,
        DistributedCacheEntryOptions entryOpts)
    {
        var cacheClient = Substitute.For<ICacheClient>();
        IDistributedCache sut = new MomentoCache(cacheClient, cacheOpts, time);

        await sut.SetAsync(key.Get, value.Get, entryOpts);

        _ = await cacheClient.Received().DictionarySetFieldsAsync(
            cacheOpts.CurrentValue.CacheName,
            key.Get,
            Arg.Any<Items>(),
            CollectionTtl.RefreshTtlIfProvided(entryOpts.AbsoluteExpirationRelativeToNow));
    }

    [Property(
        Arbitrary = [typeof(Generators.Fixed.Absolute)],
        DisplayName = "A set with an absolute expiration uses the time between now and then, synchronously.")]
    public static void AbsoluteDifference(
        IOptionsMonitor<MomentoCacheOptions> cacheOpts,
        TimeProvider time,
        NonNull<string> key,
        NonEmptyArray<byte> value,
        DistributedCacheEntryOptions entryOpts)
    {
        var cacheClient = Substitute.For<ICacheClient>();
        IDistributedCache sut = new MomentoCache(cacheClient, cacheOpts, time);

        sut.Set(key.Get, value.Get, entryOpts);

        _ = cacheClient.Received().DictionarySetFieldsAsync(
            cacheOpts.CurrentValue.CacheName,
            key.Get,
            Arg.Any<Items>(),
            CollectionTtl.RefreshTtlIfProvided(entryOpts.AbsoluteExpirationRelativeToNow));
    }

    [Property(
        Arbitrary = [typeof(Generators.Refreshable.UnlimitedSlide)],
        DisplayName = "A set with an unlimited sliding expiration does not include an absolute expiration key.")]
    public static async Task UnlimitedSlidingNoAbsoluteAsync(
        IOptionsMonitor<MomentoCacheOptions> cacheOpts,
        TimeProvider time,
        NonNull<string> key,
        NonEmptyArray<byte> value,
        DistributedCacheEntryOptions entryOpts)
    {
        var cacheClient = Substitute.For<ICacheClient>();
        IDistributedCache sut = new MomentoCache(cacheClient, cacheOpts, time);

        await sut.SetAsync(key.Get, value.Get, entryOpts);

        _ = await cacheClient.Received().DictionarySetFieldsAsync(
            cacheOpts.CurrentValue.CacheName,
            key.Get,
            Arg.Is<Items>(static ps => ps.All(p => p.Key != AbsoluteExpirationKey)),
            Arg.Any<CollectionTtl>());
    }

    [Property(
        Arbitrary = [typeof(Generators.Refreshable.UnlimitedSlide)],
        DisplayName = "A set with an unlimited sliding expiration does not include an absolute expiration key, synchronously.")]
    public static void UnlimitedSlidingNoAbsolute(
        IOptionsMonitor<MomentoCacheOptions> cacheOpts,
        TimeProvider time,
        NonNull<string> key,
        NonEmptyArray<byte> value,
        DistributedCacheEntryOptions entryOpts)
    {
        var cacheClient = Substitute.For<ICacheClient>();
        IDistributedCache sut = new MomentoCache(cacheClient, cacheOpts, time);

        sut.Set(key.Get, value.Get, entryOpts);

        _ = cacheClient.Received().DictionarySetFieldsAsync(
            cacheOpts.CurrentValue.CacheName,
            key.Get,
            Arg.Is<Items>(static ps => ps.All(p => p.Key != AbsoluteExpirationKey)),
            Arg.Any<CollectionTtl>());
    }

    [Property(
        Arbitrary = [typeof(Generators.Refreshable.LimitedSlide)],
        DisplayName = "A set with a limited sliding expiration includes an absolute expiration key.")]
    public static async Task SlidingAndLongLimitAbsoluteAsync(
        IOptionsMonitor<MomentoCacheOptions> cacheOpts,
        TimeProvider time,
        NonNull<string> key,
        NonEmptyArray<byte> value,
        DistributedCacheEntryOptions entryOpts)
    {
        var cacheClient = Substitute.For<ICacheClient>();
        IDistributedCache sut = new MomentoCache(cacheClient, cacheOpts, time);

        await sut.SetAsync(key.Get, value.Get, entryOpts);

        _ = cacheClient.Received().DictionarySetFieldsAsync(
            cacheOpts.CurrentValue.CacheName,
            key.Get,
            Arg.Is<Items>(ps => ps.Any(p => p.Key == AbsoluteExpirationKey)),
            Arg.Any<CollectionTtl>());
    }

    [Property(
        Arbitrary = [typeof(Generators.Refreshable.LimitedSlide)],
        DisplayName = "A set with a limited sliding expiration includes an absolute expiration key, synchronously.")]
    public static void SlidingAndLongLimitSliding(
        IOptionsMonitor<MomentoCacheOptions> cacheOpts,
        TimeProvider time,
        NonNull<string> key,
        NonEmptyArray<byte> value,
        DistributedCacheEntryOptions entryOpts)
    {
        var cacheClient = Substitute.For<ICacheClient>();
        IDistributedCache sut = new MomentoCache(cacheClient, cacheOpts, time);

        sut.Set(key.Get, value.Get, entryOpts);

        _ = cacheClient.Received().DictionarySetFieldsAsync(
            cacheOpts.CurrentValue.CacheName,
            key.Get,
            Arg.Is<Items>(ps => ps.Any(p => p.Key == AbsoluteExpirationKey)),
            Arg.Any<CollectionTtl>());
    }

    [Property(
        Arbitrary = [typeof(Generators.Fixed.BlockedSlide)],
        DisplayName = "A set with a blocked sliding expiration uses the limit.")]
    public static async Task SlidingAndShortLimitLimitAsync(
        IOptionsMonitor<MomentoCacheOptions> cacheOpts,
        TimeProvider time,
        NonNull<string> key,
        NonEmptyArray<byte> value,
        DistributedCacheEntryOptions entryOpts)
    {
        var cacheClient = Substitute.For<ICacheClient>();
        IDistributedCache sut = new MomentoCache(cacheClient, cacheOpts, time);

        await sut.SetAsync(key.Get, value.Get, entryOpts);

        _ = await cacheClient.Received().DictionarySetFieldsAsync(
            cacheOpts.CurrentValue.CacheName,
            key.Get,
            Arg.Any<Items>(),
            CollectionTtl.RefreshTtlIfProvided(entryOpts.AbsoluteExpirationRelativeToNow));
    }

    [Property(
        Arbitrary = [typeof(Generators.Fixed.BlockedSlide)],
        DisplayName = "A set with a blocked sliding expiration uses the limit, synchronously.")]
    public static void SlidingAndShortLimitLimit(
        IOptionsMonitor<MomentoCacheOptions> cacheOpts,
        TimeProvider time,
        NonNull<string> key,
        NonEmptyArray<byte> value,
        DistributedCacheEntryOptions entryOpts)
    {
        var cacheClient = Substitute.For<ICacheClient>();
        IDistributedCache sut = new MomentoCache(cacheClient, cacheOpts, time);

        sut.Set(key.Get, value.Get, entryOpts);

        _ = cacheClient.Received().DictionarySetFieldsAsync(
            cacheOpts.CurrentValue.CacheName,
            key.Get,
            Arg.Any<Items>(),
            CollectionTtl.RefreshTtlIfProvided(entryOpts.AbsoluteExpirationRelativeToNow));
    }

    [Property(DisplayName = "An error setting throws.")]
    public static async Task ErrorThrowsAsync(
        IOptionsMonitor<MomentoCacheOptions> cacheOpts,
        DistributedCacheEntryOptions entryOpts,
        NonNull<string> key,
        NonEmptyArray<byte> value,
        TimeProvider time,
        SetFields.Error err)
    {
        var cacheClient = SetUpScenario(err);
        IDistributedCache sut = new MomentoCache(cacheClient, cacheOpts, time);

        var actual = await Record.ExceptionAsync(() => sut.SetAsync(key.Get, value.Get, entryOpts));

        var se = Assert.IsAssignableFrom<SdkException>(actual);
        Assert.Equal(err.InnerException, se);
    }

    [Property(DisplayName = "An error setting throws, synchronously.")]
    public static void ErrorThrows(
        IOptionsMonitor<MomentoCacheOptions> cacheOpts,
        DistributedCacheEntryOptions entryOpts,
        NonNull<string> key,
        NonEmptyArray<byte> value,
        TimeProvider time,
        SetFields.Error err)
    {
        var cacheClient = SetUpScenario(err);
        IDistributedCache sut = new MomentoCache(cacheClient, cacheOpts, time);

        var actual = Record.Exception(() => sut.Set(key.Get, value.Get, entryOpts));

        var se = Assert.IsAssignableFrom<SdkException>(actual);
        Assert.Equal(err.InnerException, se);
    }

    [Property(
        Arbitrary = [typeof(Generators.Fixed.Absolute)],
        DisplayName = "An absolute expiration in the past is rejected.")]
    public static async Task InvalidExpirationThrowsAsync(
        IOptionsMonitor<MomentoCacheOptions> cacheOpts,
        NonNull<string> key,
        NonEmptyArray<byte> value,
        DateTimeOffset past)
    {
        var cacheClient = Substitute.For<ICacheClient>();
        IDistributedCache sut = new MomentoCache(cacheClient, cacheOpts, StaticTimeProvider.Max);
        var entryOpts = new DistributedCacheEntryOptions().SetAbsoluteExpiration(past);

        var actual = await Record.ExceptionAsync(() => sut.SetAsync(key.Get, value.Get, entryOpts));

        _ = Assert.IsAssignableFrom<ArgumentOutOfRangeException>(actual);
    }

    [Property(
        Arbitrary = [typeof(Generators.Fixed.Absolute)],
        DisplayName = "An absolute expiration in the past is rejected, synchronously.")]
    public static void InvalidExpirationThrows(
        IOptionsMonitor<MomentoCacheOptions> cacheOpts,
        NonNull<string> key,
        NonEmptyArray<byte> value,
        DateTimeOffset past)
    {
        var cacheClient = Substitute.For<ICacheClient>();
        IDistributedCache sut = new MomentoCache(cacheClient, cacheOpts, StaticTimeProvider.Max);
        var entryOpts = new DistributedCacheEntryOptions().SetAbsoluteExpiration(past);

        var actual = Record.Exception(() => sut.Set(key.Get, value.Get, entryOpts));

        _ = Assert.IsAssignableFrom<ArgumentOutOfRangeException>(actual);
    }

    [Property(
        Arbitrary = [typeof(Generators.Fixed.Absolute)],
        DisplayName = "An absolute expiration in the past does not attempt to set any value.")]
    public static async Task InvalidExpirationNoSetAsync(
        IOptionsMonitor<MomentoCacheOptions> cacheOpts,
        NonNull<string> key,
        NonEmptyArray<byte> value,
        DateTimeOffset past)
    {
        var cacheClient = Substitute.For<ICacheClient>();
        IDistributedCache sut = new MomentoCache(cacheClient, cacheOpts, StaticTimeProvider.Max);
        var entryOpts = new DistributedCacheEntryOptions().SetAbsoluteExpiration(past);

        _ = await Record.ExceptionAsync(() => sut.SetAsync(key.Get, value.Get, entryOpts));

        _ = await cacheClient.DidNotReceive().DictionarySetFieldsAsync(
            cacheOpts.CurrentValue.CacheName,
            key.Get,
            Arg.Any<Items>(),
            Arg.Any<CollectionTtl>());
    }

    [Property(
        Arbitrary = [typeof(Generators.Fixed.Absolute)],
        DisplayName = "An absolute expiration in the past does not attempt to set any value, synchronously.")]
    public static void InvalidExpirationNoSet(
        IOptionsMonitor<MomentoCacheOptions> cacheOpts,
        NonNull<string> key,
        NonEmptyArray<byte> value,
        DateTimeOffset past)
    {
        var cacheClient = Substitute.For<ICacheClient>();
        IDistributedCache sut = new MomentoCache(cacheClient, cacheOpts, StaticTimeProvider.Max);
        var entryOpts = new DistributedCacheEntryOptions().SetAbsoluteExpiration(past);

        _ = Record.Exception(() => sut.Set(key.Get, value.Get, entryOpts));

        _ = cacheClient.DidNotReceive().DictionarySetFieldsAsync(
            Arg.Any<string>(),
            Arg.Any<string>(),
            Arg.Any<Items>(),
            Arg.Any<CollectionTtl>());
    }

    static ICacheClient SetUpScenario<TSetFields>(TSetFields response)
        where TSetFields : SetFields
    {
        var cacheClient = Substitute.For<ICacheClient>();
        _ = cacheClient.DictionarySetFieldsAsync(
                Arg.Any<string>(),
                Arg.Any<string>(),
                Arg.Any<Items>(),
                Arg.Any<CollectionTtl>())
            .Returns(response);
        return cacheClient;
    }
}
