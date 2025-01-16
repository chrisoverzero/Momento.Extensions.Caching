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
/// <remarks>
/// A "fixed" expiration is one which doesn't slide. It could be defaulted,
/// absolute, or a limited slide whose limit is earlier than the first slide.
/// </remarks>
public sealed partial class Set
{
    [Property(
        Arbitrary = [typeof(Generators.Fixed)],
        DisplayName = "A set with any fixed expiration does not include a sliding expiration key.")]
    public async Task FixedNoSlidingAsync(
        IOptionsMonitor<MomentoCacheOptions> cacheOpts,
        NonNull<string> key,
        NonEmptyArray<byte> value,
        DistributedCacheEntryOptions entryOpts)
    {
        var sut = new MomentoCache(_cacheClient, cacheOpts, s_time);

        await sut.SetAsync(key.Get, value.Get, entryOpts);

        _ = await _cacheClient.Received().DictionarySetFieldsAsync(
            cacheOpts.CurrentValue.CacheName,
            key.Get,
            Arg.Is<Items>(static ps => ps.All(p => p.Key != SlidingExpirationKey)),
            Arg.Any<CollectionTtl>());
    }

    [Property(
        Arbitrary = [typeof(Generators.Fixed)],
        DisplayName = "A set with any fixed expiration does not include a sliding expiration key, synchronously.")]
    public void FixedNoSliding(
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
            Arg.Is<Items>(static ps => ps.All(p => p.Key != SlidingExpirationKey)),
            Arg.Any<CollectionTtl>());
    }

    [Property(
        Arbitrary = [typeof(Generators.Fixed)],
        DisplayName = "A set with any fixed expiration does not include an absolute expiration key.")]
    public async Task AbsoluteNoAbsoluteAsync(
        IOptionsMonitor<MomentoCacheOptions> cacheOpts,
        NonNull<string> key,
        NonEmptyArray<byte> value,
        DistributedCacheEntryOptions entryOpts)
    {
        var sut = new MomentoCache(_cacheClient, cacheOpts, s_time);

        await sut.SetAsync(key.Get, value.Get, entryOpts);

        _ = await _cacheClient.Received().DictionarySetFieldsAsync(
            cacheOpts.CurrentValue.CacheName,
            key.Get,
            Arg.Is<Items>(static ps => ps.All(p => p.Key != AbsoluteExpirationKey)),
            Arg.Any<CollectionTtl>());
    }

    [Property(
        Arbitrary = [typeof(Generators.Fixed)],
        DisplayName = "A set with any fixed expiration does not include an absolute expiration key, synchronously.")]
    public void AbsoluteNoAbsolute(
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
            Arg.Is<Items>(static ps => ps.All(p => p.Key != AbsoluteExpirationKey)),
            Arg.Any<CollectionTtl>());
    }

    [Property(
        Arbitrary = [typeof(Generators.Fixed.Absolute)],
        DisplayName = "A set with a fixed absolute expiration uses the time between now and then.")]
    public async Task AbsoluteDifferenceAsync(
        IOptionsMonitor<MomentoCacheOptions> cacheOpts,
        NonNull<string> key,
        NonEmptyArray<byte> value,
        DistributedCacheEntryOptions entryOpts)
    {
        var sut = new MomentoCache(_cacheClient, cacheOpts, s_time);

        await sut.SetAsync(key.Get, value.Get, entryOpts);

        _ = await _cacheClient.Received().DictionarySetFieldsAsync(
            cacheOpts.CurrentValue.CacheName,
            key.Get,
            Arg.Any<Items>(),
            CollectionTtl.RefreshTtlIfProvided(entryOpts.AbsoluteExpirationRelativeToNow));
    }

    [Property(
        Arbitrary = [typeof(Generators.Fixed.Absolute)],
        DisplayName = "A set with a fixed absolute expiration uses the time between now and then, synchronously.")]
    public void AbsoluteDifference(
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
            Arg.Any<Items>(),
            CollectionTtl.RefreshTtlIfProvided(entryOpts.AbsoluteExpirationRelativeToNow));
    }

    [Property(
        Arbitrary = [typeof(Generators.Fixed.BlockedSlide)],
        DisplayName = "A set with a blocked sliding expiration uses the limit.")]
    public async Task SlidingAndShortLimitLimitAsync(
        IOptionsMonitor<MomentoCacheOptions> cacheOpts,
        NonNull<string> key,
        NonEmptyArray<byte> value,
        DistributedCacheEntryOptions entryOpts)
    {
        var sut = new MomentoCache(_cacheClient, cacheOpts, s_time);

        await sut.SetAsync(key.Get, value.Get, entryOpts);

        _ = await _cacheClient.Received().DictionarySetFieldsAsync(
            cacheOpts.CurrentValue.CacheName,
            key.Get,
            Arg.Any<Items>(),
            CollectionTtl.RefreshTtlIfProvided(entryOpts.AbsoluteExpirationRelativeToNow));
    }

    [Property(
        Arbitrary = [typeof(Generators.Fixed.BlockedSlide)],
        DisplayName = "A set with a blocked sliding expiration uses the limit, synchronously.")]
    public void SlidingAndShortLimitLimit(
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
            Arg.Any<Items>(),
            CollectionTtl.RefreshTtlIfProvided(entryOpts.AbsoluteExpirationRelativeToNow));
    }
}
