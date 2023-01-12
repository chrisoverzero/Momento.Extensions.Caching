// <copyright file="Set.cs" company="Cimpress, Inc.">
//   Copyright 2023 Cimpress, Inc.
//
//   Licensed under the Apache License, Version 2.0 (the "License") –
//   you may not use this file except in compliance with the License.
//   You may obtain a copy of the License at
//
//     http://www.apache.org/licenses/LICENSE-2.0
//
//   Unless required by applicable law or agreed to in writing, software
//   distributed under the License is distributed on an "AS IS" BASIS,
//   WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
//   See the License for the specific language governing permissions and
//   limitations under the License.
// </copyright>

namespace Momento.Extensions.Caching.Unit;

/// <summary>Tests of the Remove operation, async and sync.</summary>
[Properties(Arbitrary = new[] { typeof(Generators) }, QuietOnSuccess = true)]
public static class Set
{
    [Property(DisplayName = "An error setting the fields throws.")]
    public static async Task SetFieldsErrorThrows(
        MomentoCacheOptions cacheOpts,
        NonNull<string> key,
        NonEmptyArray<byte> value,
        SetFields.Error err)
    {
        var simpleCacheClient = SetupScenario(err);
        IDistributedCache sut = new MomentoCache(simpleCacheClient.Object, cacheOpts);

        var actual = await Record.ExceptionAsync(() => sut.SetAsync(key.Get, value.Get));

        var se = Assert.IsAssignableFrom<SdkException>(actual);
        Assert.Equal(err.Exception, se);
        simpleCacheClient.Verify(c => c.DictionarySetFieldsAsync(
            cacheOpts.CacheName,
            key.Get,
            It.Is<Items>(ps => ps.Any(p => p.Key == ValueKey && p.Value.SequenceEqual(value.Get))),
            It.IsAny<CollectionTtl>()));
    }

    [Property(DisplayName = "An error setting the fields throws, synchronously.")]
    public static void SetFieldsErrorThrows_sync(
        MomentoCacheOptions cacheOpts,
        NonNull<string> key,
        NonEmptyArray<byte> value,
        SetFields.Error err)
    {
        var simpleCacheClient = SetupScenario(err);
        IDistributedCache sut = new MomentoCache(simpleCacheClient.Object, cacheOpts);

        var actual = Record.Exception(() => sut.Set(key.Get, value.Get));

        var se = Assert.IsAssignableFrom<SdkException>(actual);
        Assert.Equal(err.Exception, se);
        simpleCacheClient.Verify(c => c.DictionarySetFieldsAsync(
            cacheOpts.CacheName,
            key.Get,
            It.Is<Items>(ps => ps.Any(p => p.Key == ValueKey && p.Value.SequenceEqual(value.Get))),
            It.IsAny<CollectionTtl>()));
    }

    [Property(DisplayName = "A success with no specified entry options uses the cache's default TTL.")]
    public static async Task DefaultingSetFieldsSuccess(
        MomentoCacheOptions cacheOpts,
        NonNull<string> key,
        NonEmptyArray<byte> value,
        SetFields.Success success)
    {
        var simpleCacheClient = SetupScenario(success);
        IDistributedCache sut = new MomentoCache(simpleCacheClient.Object, cacheOpts);

        await sut.SetAsync(key.Get, value.Get);

        simpleCacheClient.Verify(c => c.DictionarySetFieldsAsync(
            cacheOpts.CacheName,
            key.Get,
            It.Is<Items>(ps => ps.Any(p => p.Key == ValueKey && p.Value.SequenceEqual(value.Get))),
            CollectionTtl.FromCacheTtl()));
    }

    [Property(DisplayName = "A success with no specified entry options uses the cache's default TTL, synchronously.")]
    public static void DefaultingSetFieldsSuccess_sync(
        MomentoCacheOptions cacheOpts,
        NonNull<string> key,
        NonEmptyArray<byte> value,
        SetFields.Success success)
    {
        var simpleCacheClient = SetupScenario(success);
        IDistributedCache sut = new MomentoCache(simpleCacheClient.Object, cacheOpts);

        sut.Set(key.Get, value.Get);

        simpleCacheClient.Verify(c => c.DictionarySetFieldsAsync(
            cacheOpts.CacheName,
            key.Get,
            It.Is<Items>(ps => ps.Any(p => p.Key == ValueKey && p.Value.SequenceEqual(value.Get))),
            CollectionTtl.FromCacheTtl()));
    }

    [Property(DisplayName = "A success with entry options specifying an absolute expiration uses the time between now and then.")]
    public static async Task AbsoluteSetFieldsSuccess(
        MomentoCacheOptions cacheOpts,
        NonNull<string> key,
        NonEmptyArray<byte> value,
        SetFields.Success success,
        DateTimeOffset now,
        Duration duration)
    {
        cacheOpts.Clock = Mock.Of<ISystemClock>(c => c.UtcNow == now);
        var simpleCacheClient = SetupScenario(success);
        IDistributedCache sut = new MomentoCache(simpleCacheClient.Object, cacheOpts);
        var entryOpts = new DistributedCacheEntryOptions().SetAbsoluteExpiration(duration);

        await sut.SetAsync(key.Get, value.Get, entryOpts);

        simpleCacheClient.Verify(c => c.DictionarySetFieldsAsync(
            cacheOpts.CacheName,
            key.Get,
            It.Is<Items>(ps => ps.Any(p => p.Key == ValueKey && p.Value.SequenceEqual(value.Get))),
            CollectionTtl.Of(duration)));
    }

    [Property(DisplayName = "A success with entry options specifying an absolute expiration uses the time between now and then, synchronously.")]
    public static void AbsoluteSetFieldsSuccess_sync(
        MomentoCacheOptions cacheOpts,
        NonNull<string> key,
        NonEmptyArray<byte> value,
        SetFields.Success success,
        DateTimeOffset now,
        Duration duration)
    {
        cacheOpts.Clock = Mock.Of<ISystemClock>(c => c.UtcNow == now);
        var simpleCacheClient = SetupScenario(success);
        IDistributedCache sut = new MomentoCache(simpleCacheClient.Object, cacheOpts);
        var entryOpts = new DistributedCacheEntryOptions().SetAbsoluteExpiration(duration);

        sut.Set(key.Get, value.Get, entryOpts);

        simpleCacheClient.Verify(c => c.DictionarySetFieldsAsync(
            cacheOpts.CacheName,
            key.Get,
            It.Is<Items>(ps => ps.Any(p => p.Key == ValueKey && p.Value.SequenceEqual(value.Get))),
            CollectionTtl.Of(duration)));
    }

    [Property(DisplayName = "A success with entry options specifying a sliding expiration uses that.")]
    public static async Task SlidingSetFieldsSuccess(
        MomentoCacheOptions cacheOpts,
        NonNull<string> key,
        NonEmptyArray<byte> value,
        SetFields.Success success,
        Duration duration)
    {
        var simpleCacheClient = SetupScenario(success);
        IDistributedCache sut = new MomentoCache(simpleCacheClient.Object, cacheOpts);
        var entryOpts = new DistributedCacheEntryOptions().SetSlidingExpiration(duration);

        await sut.SetAsync(key.Get, value.Get, entryOpts);

        simpleCacheClient.Verify(c => c.DictionarySetFieldsAsync(
            cacheOpts.CacheName,
            key.Get,
            It.Is<Items>(ps =>
                ps.Any(p => p.Key == ValueKey && p.Value.SequenceEqual(value.Get))
                && ps.Any(p => p.Key == SlidingExpirationKey)),
            CollectionTtl.Of(duration)));
    }

    [Property(DisplayName = "A success with entry options specifying a sliding expiration uses that, synchronously.")]
    public static void SlidingSetFieldsSuccess_sync(
        MomentoCacheOptions cacheOpts,
        NonNull<string> key,
        NonEmptyArray<byte> value,
        SetFields.Success success,
        Duration duration)
    {
        var simpleCacheClient = SetupScenario(success);
        IDistributedCache sut = new MomentoCache(simpleCacheClient.Object, cacheOpts);
        var entryOpts = new DistributedCacheEntryOptions().SetSlidingExpiration(duration);

        sut.Set(key.Get, value.Get, entryOpts);

        simpleCacheClient.Verify(c => c.DictionarySetFieldsAsync(
            cacheOpts.CacheName,
            key.Get,
            It.Is<Items>(ps =>
                ps.Any(p => p.Key == ValueKey && p.Value.SequenceEqual(value.Get))
                && ps.Any(p => p.Key == SlidingExpirationKey)),
            CollectionTtl.Of(duration)));
    }

    [Property(DisplayName = "An absolute expiration in the past is rejected.")]
    public static async Task ExpirationInThePastThrows(
        MomentoCacheOptions cacheOpts,
        NonNull<string> key,
        NonEmptyArray<byte> value,
        SetFields.Success success,
        DateTimeOffset past)
    {
        cacheOpts.Clock = Mock.Of<ISystemClock>(c => c.UtcNow == DateTimeOffset.MaxValue);
        var simpleCacheClient = SetupScenario(success);
        IDistributedCache sut = new MomentoCache(simpleCacheClient.Object, cacheOpts);
        var entryOpts = new DistributedCacheEntryOptions().SetAbsoluteExpiration(past);

        var actual = await Record.ExceptionAsync(() => sut.SetAsync(key.Get, value.Get, entryOpts));

        _ = Assert.IsAssignableFrom<ArgumentOutOfRangeException>(actual);
        simpleCacheClient.Verify(
            static c => c.DictionarySetFieldsAsync(
                It.IsNotNull<string>(),
                It.IsNotNull<string>(),
                It.IsNotNull<Items>(),
                It.IsAny<CollectionTtl>()),
            Times.Never);
    }

    [Property(DisplayName = "An absolute expiration in the past is rejected, synchronously.")]
    public static void ExpirationInThePastThrows_sync(
        MomentoCacheOptions cacheOpts,
        NonNull<string> key,
        NonEmptyArray<byte> value,
        SetFields.Success success,
        DateTimeOffset past)
    {
        cacheOpts.Clock = Mock.Of<ISystemClock>(c => c.UtcNow == DateTimeOffset.MaxValue);
        var simpleCacheClient = SetupScenario(success);
        IDistributedCache sut = new MomentoCache(simpleCacheClient.Object, cacheOpts);
        var entryOpts = new DistributedCacheEntryOptions().SetAbsoluteExpiration(past);

        var actual = Record.Exception(() => sut.Set(key.Get, value.Get, entryOpts));

        _ = Assert.IsAssignableFrom<ArgumentOutOfRangeException>(actual);
        simpleCacheClient.Verify(
            static c => c.DictionarySetFieldsAsync(
                It.IsNotNull<string>(),
                It.IsNotNull<string>(),
                It.IsNotNull<Items>(),
                It.IsAny<CollectionTtl>()),
            Times.Never);
    }

    static Mock<ISimpleCacheClient> SetupScenario<TSetFields>(TSetFields response)
        where TSetFields : SetFields
    {
        var simpleCacheClient = new Mock<ISimpleCacheClient>();
        _ = simpleCacheClient
            .Setup(static c => c.DictionarySetFieldsAsync(
                It.IsNotNull<string>(),
                It.IsNotNull<string>(),
                It.IsNotNull<Items>(),
                It.IsAny<CollectionTtl>()))
            .ReturnsAsync(response);
        return simpleCacheClient;
    }
}
