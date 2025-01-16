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
public sealed partial class Set
    : IDisposable
{
    static readonly TimeProvider s_time = TimeProvider.System;

    readonly ICacheClient _cacheClient = Substitute.For<ICacheClient>();
    readonly DistributedCacheEntryOptions _foreverAgo = new() { AbsoluteExpiration = DateTimeOffset.MinValue };

    [Property(DisplayName = "Any set uses the provided value.")]
    public async Task SetUsesProvidedValueAsync(
        IOptionsMonitor<MomentoCacheOptions> cacheOpts,
        NonNull<string> key,
        NonEmptyArray<byte> value,
        DistributedCacheEntryOptions entryOpts)
    {
        var sut = new MomentoCache(_cacheClient, cacheOpts, s_time);

        _ = sut.SetAsync(key.Get, value.Get, entryOpts);

        _ = await _cacheClient.Received().DictionarySetFieldsAsync(
            cacheOpts.CurrentValue.CacheName,
            key.Get,
            Arg.Is<Items>(ps => ps.Any(p => p.Key == ValueKey && p.Value.SequenceEqual(value.Get))),
            Arg.Any<CollectionTtl>());
    }

    [Property(DisplayName = "Any set uses the provided value, synchronously.")]
    public void SetUsesProvidedValue(
        IOptionsMonitor<MomentoCacheOptions> cacheOpts,
        NonNull<string> key,
        NonEmptyArray<byte> value,
        DistributedCacheEntryOptions entryOpts)
    {
        IDistributedCache sut = new MomentoCache(_cacheClient, cacheOpts, s_time);

        sut.Set(key.Get, value.Get, entryOpts);

        _ = _cacheClient.Received().DictionarySetFieldsAsync(
            cacheOpts.CurrentValue.CacheName,
            key.Get,
            Arg.Is<Items>(ps => ps.Any(p => p.Key == ValueKey && p.Value.SequenceEqual(value.Get))),
            Arg.Any<CollectionTtl>());
    }

    [Property(DisplayName = "A set with default entry options uses the cache's default TTL.")]
    public async Task DefaultDefaultTtlAsync(
        IOptionsMonitor<MomentoCacheOptions> cacheOpts,
        NonNull<string> key,
        NonEmptyArray<byte> value)
    {
        var sut = new MomentoCache(_cacheClient, cacheOpts, s_time);

        await sut.SetAsync(key.Get, value.Get, new());

        _ = await _cacheClient.Received().DictionarySetFieldsAsync(
            cacheOpts.CurrentValue.CacheName,
            key.Get,
            Arg.Any<Items>(),
            CollectionTtl.FromCacheTtl());
    }

    [Property(DisplayName = "A set with default entry options uses the cache's default TTL, synchronously.")]
    public void DefaultDefaultTtl(
        IOptionsMonitor<MomentoCacheOptions> cacheOpts,
        NonNull<string> key,
        NonEmptyArray<byte> value)
    {
        IDistributedCache sut = new MomentoCache(_cacheClient, cacheOpts, s_time);

        sut.Set(key.Get, value.Get, new());

        _ = _cacheClient.Received().DictionarySetFieldsAsync(
            cacheOpts.CurrentValue.CacheName,
            key.Get,
            Arg.Any<Items>(),
            CollectionTtl.FromCacheTtl());
    }

    [Property(DisplayName = "An error setting throws.")]
    public async Task ErrorThrowsAsync(
        IOptionsMonitor<MomentoCacheOptions> cacheOpts,
        DistributedCacheEntryOptions entryOpts,
        NonNull<string> key,
        NonEmptyArray<byte> value,
        SetFields.Error err)
    {
        _cacheClient.SetUpScenario(err);
        var sut = new MomentoCache(_cacheClient, cacheOpts, s_time);

        var actual = await Record.ExceptionAsync(() => sut.SetAsync(key.Get, value.Get, entryOpts));

        var se = Assert.IsType<SdkException>(actual, exactMatch: false);
        Assert.Equal(err.InnerException, se);
    }

    [Property(DisplayName = "An error setting throws, synchronously.")]
    public void ErrorThrows(
        IOptionsMonitor<MomentoCacheOptions> cacheOpts,
        DistributedCacheEntryOptions entryOpts,
        NonNull<string> key,
        NonEmptyArray<byte> value,
        SetFields.Error err)
    {
        _cacheClient.SetUpScenario(err);
        IDistributedCache sut = new MomentoCache(_cacheClient, cacheOpts, s_time);

        var actual = Record.Exception(() => sut.Set(key.Get, value.Get, entryOpts));

        var se = Assert.IsType<SdkException>(actual, exactMatch: false);
        Assert.Equal(err.InnerException, se);
    }

    [Property(DisplayName = "An absolute expiration in the past is rejected.")]
    public async Task InvalidExpirationThrowsAsync(
        IOptionsMonitor<MomentoCacheOptions> cacheOpts,
        NonNull<string> key,
        NonEmptyArray<byte> value)
    {
        var sut = new MomentoCache(_cacheClient, cacheOpts, s_time);

        var actual = await Record.ExceptionAsync(() => sut.SetAsync(key.Get, value.Get, _foreverAgo));

        _ = Assert.IsType<ArgumentOutOfRangeException>(actual, exactMatch: false);
    }

    [Property(DisplayName = "An absolute expiration in the past is rejected, synchronously.")]
    public void InvalidExpirationThrows(
        IOptionsMonitor<MomentoCacheOptions> cacheOpts,
        NonNull<string> key,
        NonEmptyArray<byte> value)
    {
        IDistributedCache sut = new MomentoCache(_cacheClient, cacheOpts, s_time);

        var actual = Record.Exception(() => sut.Set(key.Get, value.Get, _foreverAgo));

        _ = Assert.IsType<ArgumentOutOfRangeException>(actual, exactMatch: false);
    }

    [Property(DisplayName = "An absolute expiration in the past does not attempt to set any value.")]
    public async Task InvalidExpirationNoSetAsync(
        IOptionsMonitor<MomentoCacheOptions> cacheOpts,
        NonNull<string> key,
        NonEmptyArray<byte> value)
    {
        var sut = new MomentoCache(_cacheClient, cacheOpts, s_time);

        _ = await Record.ExceptionAsync(() => sut.SetAsync(key.Get, value.Get, _foreverAgo));

        _ = await _cacheClient.DidNotReceive().DictionarySetFieldsAsync(
            cacheOpts.CurrentValue.CacheName,
            key.Get,
            Arg.Any<Items>(),
            Arg.Any<CollectionTtl>());
    }

    [Property(DisplayName = "An absolute expiration in the past does not attempt to set any value, synchronously.")]
    public void InvalidExpirationNoSet(
        IOptionsMonitor<MomentoCacheOptions> cacheOpts,
        NonNull<string> key,
        NonEmptyArray<byte> value)
    {
        IDistributedCache sut = new MomentoCache(_cacheClient, cacheOpts, s_time);

        _ = Record.Exception(() => sut.Set(key.Get, value.Get, _foreverAgo));

        _ = _cacheClient.DidNotReceive().DictionarySetFieldsAsync(
            Arg.Any<string>(),
            Arg.Any<string>(),
            Arg.Any<Items>(),
            Arg.Any<CollectionTtl>());
    }

    public void Dispose() => _cacheClient.Dispose();
}
