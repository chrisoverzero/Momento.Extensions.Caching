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
/// A "refreshable" expiration is one which refreshes its TTL upon read.
/// It could be unlimited (slides forever) or limited (slides until…).
/// </remarks>
public sealed partial class Set
{
    [Property(
        Arbitrary = [typeof(Generators.Refreshable)],
        DisplayName = "A set with any refreshable expiration uses the slide.")]
    public async Task SlidingSlideAsync(
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
            CollectionTtl.RefreshTtlIfProvided(entryOpts.SlidingExpiration));
    }

    [Property(
        Arbitrary = [typeof(Generators.Refreshable)],
        DisplayName = "A set with any refreshable expiration uses the slide, synchronously.")]
    public void SlidingSlide(
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
            CollectionTtl.RefreshTtlIfProvided(entryOpts.SlidingExpiration));
    }

    [Property(
        Arbitrary = [typeof(Generators.Refreshable)],
        DisplayName = "A set with any refreshable expiration includes a sliding expiration key.")]
    public async Task RefreshableSlidingAsync(
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
            Arg.Is<Items>(static ps => ps.Any(p => p.Key == SlidingExpirationKey)),
            Arg.Any<CollectionTtl>());
    }

    [Property(
        Arbitrary = [typeof(Generators.Refreshable)],
        DisplayName = "A set with any refreshable expiration includes a sliding expiration key, synchronously.")]
    public void RefreshableSliding(
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
            Arg.Is<Items>(static ps => ps.Any(p => p.Key == SlidingExpirationKey)),
            Arg.Any<CollectionTtl>());
    }

    [Property(
        Arbitrary = [typeof(Generators.Refreshable.UnlimitedSlide)],
        DisplayName = "A set with an unlimited sliding expiration does not include an absolute expiration key.")]
    public async Task UnlimitedSlidingNoAbsoluteAsync(
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
        Arbitrary = [typeof(Generators.Refreshable.UnlimitedSlide)],
        DisplayName = "A set with an unlimited sliding expiration does not include an absolute expiration key, synchronously.")]
    public void UnlimitedSlidingNoAbsolute(
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
        Arbitrary = [typeof(Generators.Refreshable.LimitedSlide)],
        DisplayName = "A set with a limited sliding expiration includes an absolute expiration key.")]
    public async Task SlidingAndLongLimitAbsoluteAsync(
        IOptionsMonitor<MomentoCacheOptions> cacheOpts,
        NonNull<string> key,
        NonEmptyArray<byte> value,
        DistributedCacheEntryOptions entryOpts)
    {
        var sut = new MomentoCache(_cacheClient, cacheOpts, s_time);

        await sut.SetAsync(key.Get, value.Get, entryOpts);

        _ = _cacheClient.Received().DictionarySetFieldsAsync(
            cacheOpts.CurrentValue.CacheName,
            key.Get,
            Arg.Is<Items>(ps => ps.Any(p => p.Key == AbsoluteExpirationKey)),
            Arg.Any<CollectionTtl>());
    }

    [Property(
        Arbitrary = [typeof(Generators.Refreshable.LimitedSlide)],
        DisplayName = "A set with a limited sliding expiration includes an absolute expiration key, synchronously.")]
    public void SlidingAndLongLimitSliding(
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
            Arg.Is<Items>(ps => ps.Any(p => p.Key == AbsoluteExpirationKey)),
            Arg.Any<CollectionTtl>());
    }
}
