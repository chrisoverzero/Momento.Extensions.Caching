// <copyright file="Set.Refreshable.cs" company="Cimpress, Inc.">
// Copyright 2023 Cimpress, Inc.
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
// </copyright>

namespace Momento.Extensions.Caching.Unit;

/// <summary>Tests of the Set operation, async and sync.</summary>
/// <remarks>
/// A "refreshable" expiration is one which refreshes its TTL upon read.
/// It could be unlimited (slides forever) or limited (slides until…).
/// </remarks>
public static partial class Set
{
    [Property(
        Arbitrary = [typeof(Generators.Refreshable)],
        DisplayName = "A set with any refreshable expiration uses the slide.")]
    public static async Task SlidingSlideAsync(
        IOptionsSnapshot<MomentoCacheOptions> cacheOpts,
        TimeProvider time,
        NonNull<string> key,
        NonEmptyArray<byte> value,
        DistributedCacheEntryOptions entryOpts)
    {
        var cacheClient = Substitute.For<ICacheClient>();
        IDistributedCache sut = new MomentoCache(cacheClient, cacheOpts, time);

        await sut.SetAsync(key.Get, value.Get, entryOpts);

        _ = await cacheClient.Received().DictionarySetFieldsAsync(
            cacheOpts.Value.CacheName,
            key.Get,
            Arg.Any<Items>(),
            CollectionTtl.RefreshTtlIfProvided(entryOpts.SlidingExpiration));
    }

    [Property(
        Arbitrary = [typeof(Generators.Refreshable)],
        DisplayName = "A set with any refreshable expiration uses the slide, synchronously.")]
    public static void SlidingSlide(
        IOptionsSnapshot<MomentoCacheOptions> cacheOpts,
        TimeProvider time,
        NonNull<string> key,
        NonEmptyArray<byte> value,
        DistributedCacheEntryOptions entryOpts)
    {
        var cacheClient = Substitute.For<ICacheClient>();
        IDistributedCache sut = new MomentoCache(cacheClient, cacheOpts, time);

        sut.Set(key.Get, value.Get, entryOpts);

        _ = cacheClient.Received().DictionarySetFieldsAsync(
            cacheOpts.Value.CacheName,
            key.Get,
            Arg.Any<Items>(),
            CollectionTtl.RefreshTtlIfProvided(entryOpts.SlidingExpiration));
    }

    [Property(
        Arbitrary = [typeof(Generators.Refreshable)],
        DisplayName = "A set with any refreshable expiration includes a sliding expiration key.")]
    public static async Task RefreshableSlidingAsync(
        IOptionsSnapshot<MomentoCacheOptions> cacheOpts,
        TimeProvider time,
        NonNull<string> key,
        NonEmptyArray<byte> value,
        DistributedCacheEntryOptions entryOpts)
    {
        var cacheClient = Substitute.For<ICacheClient>();
        IDistributedCache sut = new MomentoCache(cacheClient, cacheOpts, time);

        await sut.SetAsync(key.Get, value.Get, entryOpts);

        _ = await cacheClient.Received().DictionarySetFieldsAsync(
            cacheOpts.Value.CacheName,
            key.Get,
            Arg.Is<Items>(static ps => ps.Any(p => p.Key == SlidingExpirationKey)),
            Arg.Any<CollectionTtl>());
    }

    [Property(
        Arbitrary = [typeof(Generators.Refreshable)],
        DisplayName = "A set with any refreshable expiration includes a sliding expiration key, synchronously.")]
    public static void RefreshableSliding(
        IOptionsSnapshot<MomentoCacheOptions> cacheOpts,
        TimeProvider time,
        NonNull<string> key,
        NonEmptyArray<byte> value,
        DistributedCacheEntryOptions entryOpts)
    {
        var cacheClient = Substitute.For<ICacheClient>();
        IDistributedCache sut = new MomentoCache(cacheClient, cacheOpts, time);

        sut.Set(key.Get, value.Get, entryOpts);

        _ = cacheClient.Received().DictionarySetFieldsAsync(
            cacheOpts.Value.CacheName,
            key.Get,
            Arg.Is<Items>(static ps => ps.Any(p => p.Key == SlidingExpirationKey)),
            Arg.Any<CollectionTtl>());
    }
}
