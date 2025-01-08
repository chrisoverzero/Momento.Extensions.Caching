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

/// <summary>Tests of the Get operation, async and sync.</summary>
[Properties(Arbitrary = [typeof(Generators)], MaxTest = 1024, QuietOnSuccess = true)]
public static class Get
{
    [Property(DisplayName = "Any get actually attempts to get the value.")]
    public static async Task GetValueAsync(
        IOptionsMonitor<MomentoCacheOptions> cacheOpts,
        NonNull<string> key,
        TimeProvider time)
    {
        var cacheClient = Substitute.For<ICacheClient>();
        IDistributedCache sut = new MomentoCache(cacheClient, cacheOpts, time);

        _ = await sut.GetAsync(key.Get);

        _ = await cacheClient.Received().DictionaryGetFieldsAsync(
            cacheOpts.CurrentValue.CacheName,
            key.Get,
            Arg.Any<IEnumerable<string>>());
    }

    [Property(DisplayName = "Any get actually attempts to get the value, asynchronously.")]
    public static void GetValue(
        IOptionsMonitor<MomentoCacheOptions> cacheOpts,
        NonNull<string> key,
        TimeProvider time)
    {
        var cacheClient = Substitute.For<ICacheClient>();
        IDistributedCache sut = new MomentoCache(cacheClient, cacheOpts, time);

        _ = sut.Get(key.Get);

        _ = cacheClient.Received().DictionaryGetFieldsAsync(
            cacheOpts.CurrentValue.CacheName,
            key.Get,
            Arg.Any<IEnumerable<string>>());
    }

    [Property(DisplayName = "A miss returns null.")]
    public static async Task MissNullAsync(
        IOptionsMonitor<MomentoCacheOptions> cacheOpts,
        NonNull<string> key,
        TimeProvider time,
        GetFields.Miss miss)
    {
        var cacheClient = SetUpScenario(miss);
        IDistributedCache sut = new MomentoCache(cacheClient, cacheOpts, time);

        var actual = await sut.GetAsync(key.Get);

        Assert.Null(actual);
    }

    [Property(DisplayName = "A miss returns null, synchronously.")]
    public static void MissNull(
        IOptionsMonitor<MomentoCacheOptions> cacheOpts,
        NonNull<string> key,
        TimeProvider time,
        GetFields.Miss miss)
    {
        var cacheClient = SetUpScenario(miss);
        IDistributedCache sut = new MomentoCache(cacheClient, cacheOpts, time);

        var actual = sut.Get(key.Get);

        Assert.Null(actual);
    }

    [Property(DisplayName = "A miss does not attempt to update an item's TTL.")]
    public static async Task MissNoUpdateAsync(
        IOptionsMonitor<MomentoCacheOptions> cacheOpts,
        NonNull<string> key,
        TimeProvider time,
        GetFields.Miss miss)
    {
        var cacheClient = SetUpScenario(miss);
        IDistributedCache sut = new MomentoCache(cacheClient, cacheOpts, time);

        _ = await sut.GetAsync(key.Get);

        _ = await cacheClient.DidNotReceive().UpdateTtlAsync(
            Arg.Any<string>(),
            Arg.Any<string>(),
            Arg.Any<TimeSpan>());
    }

    [Property(DisplayName = "A miss does not attempt to update an item's TTL, synchronously.")]
    public static void MissNoUpdate(
        IOptionsMonitor<MomentoCacheOptions> cacheOpts,
        NonNull<string> key,
        TimeProvider time,
        GetFields.Miss miss)
    {
        var cacheClient = SetUpScenario(miss);
        IDistributedCache sut = new MomentoCache(cacheClient, cacheOpts, time);

        _ = sut.Get(key.Get);

        _ = cacheClient.DidNotReceive().UpdateTtlAsync(
            Arg.Any<string>(),
            Arg.Any<string>(),
            Arg.Any<TimeSpan>());
    }

    [Property(DisplayName = "An error initially getting the value throws.")]
    public static async Task InitialErrorThrowsAsync(
        IOptionsMonitor<MomentoCacheOptions> cacheOpts,
        NonNull<string> key,
        TimeProvider time,
        GetFields.Error err)
    {
        var cacheClient = SetUpScenario(err);
        IDistributedCache sut = new MomentoCache(cacheClient, cacheOpts, time);

        var actual = await Record.ExceptionAsync(() => sut.GetAsync(key.Get));

        var se = Assert.IsAssignableFrom<SdkException>(actual);
        Assert.Equal(err.InnerException, se);
    }

    [Property(DisplayName = "An error initially getting the value throws, synchronously.")]
    public static void InitialErrorThrows(
        IOptionsMonitor<MomentoCacheOptions> cacheOpts,
        NonNull<string> key,
        TimeProvider time,
        GetFields.Error err)
    {
        var cacheClient = SetUpScenario(err);
        IDistributedCache sut = new MomentoCache(cacheClient, cacheOpts, time);

        var actual = Record.Exception(() => sut.Get(key.Get));

        // note(cosborn) The important part of this test is that this _not_ be an `AggregateException`.
        var se = Assert.IsAssignableFrom<SdkException>(actual);
        Assert.Equal(err.InnerException, se);
    }

    [Property(DisplayName = "An error initially getting the value does not attempt to update any item's TTL.")]
    public static async Task InitialErrorNoUpdateAsync(
        IOptionsMonitor<MomentoCacheOptions> cacheOpts,
        NonNull<string> key,
        TimeProvider time,
        GetFields.Error error)
    {
        var cacheClient = SetUpScenario(error);
        IDistributedCache sut = new MomentoCache(cacheClient, cacheOpts, time);

        _ = await Record.ExceptionAsync(() => sut.GetAsync(key.Get));

        _ = await cacheClient.DidNotReceive().UpdateTtlAsync(
            Arg.Any<string>(),
            Arg.Any<string>(),
            Arg.Any<TimeSpan>());
    }

    [Property(DisplayName = "An error initially getting the value does not attempt to update any item's TTL, synchronously.")]
    public static void InitialErrorNoUpdate(
        IOptionsMonitor<MomentoCacheOptions> cacheOpts,
        NonNull<string> key,
        TimeProvider time,
        GetFields.Error error)
    {
        var cacheClient = SetUpScenario(error);
        IDistributedCache sut = new MomentoCache(cacheClient, cacheOpts, time);

        _ = Record.Exception(() => sut.Get(key.Get));

        _ = cacheClient.DidNotReceive().UpdateTtlAsync(
            Arg.Any<string>(),
            Arg.Any<string>(),
            Arg.Any<TimeSpan>());
    }

    [Property(DisplayName = "A hit of any kind returns the cached value.")]
    public static async Task HitValueAsync(
        IOptionsMonitor<MomentoCacheOptions> cacheOpts,
        NonNull<string> key,
        TimeProvider time,
        GetFields.Hit hit)
    {
        var cacheClient = SetUpScenario(hit);
        IDistributedCache sut = new MomentoCache(cacheClient, cacheOpts, time);

        var actual = await sut.GetAsync(key.Get);

        // note(cosborn) `null` is the indicator of a miss, so assert it especially.
        Assert.NotNull(actual);
        Assert.Equal(hit.ValueDictionaryStringByteArray[ValueKey], actual);
    }

    [Property(DisplayName = "A hit of any kind returns the cached value, synchronously.")]
    public static void HitValue(
        IOptionsMonitor<MomentoCacheOptions> cacheOpts,
        NonNull<string> key,
        TimeProvider time,
        GetFields.Hit hit)
    {
        var cacheClient = SetUpScenario(hit);
        IDistributedCache sut = new MomentoCache(cacheClient, cacheOpts, time);

        var actual = sut.Get(key.Get);

        // note(cosborn) `null` is the indicator of a miss, so assert it especially.
        Assert.NotNull(actual);
        Assert.Equal(hit.ValueDictionaryStringByteArray[ValueKey], actual);
    }

    [Property(
        Arbitrary = [typeof(Generators.Fixed)],
        DisplayName = "A hit with any fixed expiration does not attempt to update the item's TTL.")]
    public static async Task HitFixedNoUpdateAsync(
        IOptionsMonitor<MomentoCacheOptions> cacheOpts,
        NonNull<string> key,
        TimeProvider time,
        GetFields.Hit hit)
    {
        var cacheClient = SetUpScenario(hit);
        IDistributedCache sut = new MomentoCache(cacheClient, cacheOpts, time);

        _ = await sut.GetAsync(key.Get);

        _ = await cacheClient.DidNotReceive().UpdateTtlAsync(
            Arg.Any<string>(),
            Arg.Any<string>(),
            Arg.Any<TimeSpan>());
    }

    [Property(
        Arbitrary = [typeof(Generators.Fixed)],
        DisplayName = "A hit with any fixed expiration does not attempt to update the item's TTL, synchronously.")]
    public static void HitFixedNoUpdate(
        IOptionsMonitor<MomentoCacheOptions> cacheOpts,
        NonNull<string> key,
        TimeProvider time,
        GetFields.Hit hit)
    {
        var cacheClient = SetUpScenario(hit);
        IDistributedCache sut = new MomentoCache(cacheClient, cacheOpts, time);

        _ = sut.Get(key.Get);

        _ = cacheClient.DidNotReceive().UpdateTtlAsync(
            Arg.Any<string>(),
            Arg.Any<string>(),
            Arg.Any<TimeSpan>());
    }

    [Property(
        Arbitrary = [typeof(Generators.Refreshable)],
        DisplayName = "A hit with any refreshable expiration attempts to update the item's TTL.")]
    public static async Task HitRefreshableUpdateAsync(
        IOptionsMonitor<MomentoCacheOptions> cacheOpts,
        NonNull<string> key,
        TimeProvider time,
        GetFields.Hit hit)
    {
        var cacheClient = SetUpScenario(hit);
        IDistributedCache sut = new MomentoCache(cacheClient, cacheOpts, time);

        _ = await sut.GetAsync(key.Get);

        _ = await cacheClient.Received().UpdateTtlAsync(
            cacheOpts.CurrentValue.CacheName,
            key.Get,
            Arg.Any<TimeSpan>());
    }

    [Property(
        Arbitrary = [typeof(Generators.Refreshable)],
        DisplayName = "A hit with any refreshable expiration attempts to update the item's TTL, synchronously.")]
    public static void HitRefreshableUpdate(
        IOptionsMonitor<MomentoCacheOptions> cacheOpts,
        NonNull<string> key,
        TimeProvider time,
        GetFields.Hit hit)
    {
        var cacheClient = SetUpScenario(hit);
        IDistributedCache sut = new MomentoCache(cacheClient, cacheOpts, time);

        _ = sut.Get(key.Get);

        _ = cacheClient.Received().UpdateTtlAsync(
            cacheOpts.CurrentValue.CacheName,
            key.Get,
            Arg.Any<TimeSpan>());
    }

    [Property(
        Arbitrary = [typeof(Generators.Refreshable.LimitedSlide)],
        DisplayName = "A hit with a limited sliding expiration attempts to update the item's TTL.")]
    public static async Task HitLimitedSlidingUpdateAsync(
        IOptionsMonitor<MomentoCacheOptions> cacheOpts,
        NonNull<string> key,
        TimeProvider time,
        GetFields.Hit hit)
    {
        var cacheClient = SetUpScenario(hit);
        IDistributedCache sut = new MomentoCache(cacheClient, cacheOpts, time);

        _ = await sut.GetAsync(key.Get);

        _ = await cacheClient.Received().UpdateTtlAsync(
            cacheOpts.CurrentValue.CacheName,
            key.Get,
            Arg.Any<TimeSpan>());
    }

    [Property(
        Arbitrary = [typeof(Generators.Refreshable.LimitedSlide)],
        DisplayName = "A hit with a limited sliding expiration attempts to update the item's TTL, synchronously.")]
    public static void HitLimitedSlidingUpdate(
        IOptionsMonitor<MomentoCacheOptions> cacheOpts,
        NonNull<string> key,
        TimeProvider time,
        GetFields.Hit hit)
    {
        var cacheClient = SetUpScenario(hit);
        IDistributedCache sut = new MomentoCache(cacheClient, cacheOpts, time);

        _ = sut.Get(key.Get);

        _ = cacheClient.Received().UpdateTtlAsync(
            cacheOpts.CurrentValue.CacheName,
            key.Get,
            Arg.Any<TimeSpan>());
    }

    [Property(
        Arbitrary = [typeof(Generators.Refreshable)],
        DisplayName = "An error on updating the TTL throws.")]
    public static async Task UpdateTtlErrorThrowsAsync(
        IOptionsMonitor<MomentoCacheOptions> cacheOpts,
        NonNull<string> key,
        TimeProvider time,
        GetFields.Hit hit,
        UpdateTtl.Error err)
    {
        var cacheClient = SetUpScenario(hit, err);
        IDistributedCache sut = new MomentoCache(cacheClient, cacheOpts, time);

        var actual = await Record.ExceptionAsync(() => sut.GetAsync(key.Get));

        var se = Assert.IsAssignableFrom<SdkException>(actual);
        Assert.Equal(se, err.InnerException);
    }

    [Property(
        Arbitrary = [typeof(Generators.Refreshable)],
        DisplayName = "An error on updating the TTL throws, synchronously.")]
    public static void UpdateTtlErrorThrows(
        IOptionsMonitor<MomentoCacheOptions> cacheOpts,
        NonNull<string> key,
        TimeProvider time,
        GetFields.Hit hit,
        UpdateTtl.Error err)
    {
        var cacheClient = SetUpScenario(hit, err);
        IDistributedCache sut = new MomentoCache(cacheClient, cacheOpts, time);

        var actual = Record.Exception(() => sut.Get(key.Get));

        // note(cosborn) The important part of this test is that this _not_ be an `AggregateException`.
        var se = Assert.IsAssignableFrom<SdkException>(actual);
        Assert.Equal(se, err.InnerException);
    }

    static ICacheClient SetUpScenario<TGetFields>(TGetFields getFieldsResponse)
        where TGetFields : GetFields
    {
        var cacheClient = Substitute.For<ICacheClient>();
        _ = cacheClient.DictionaryGetFieldsAsync(
                Arg.Any<string>(),
                Arg.Any<string>(),
                Arg.Any<IEnumerable<string>>())
            .Returns(getFieldsResponse);
        return cacheClient;
    }

    static ICacheClient SetUpScenario<TGetFields, TUpdateTTL>(
        TGetFields getFieldsResponse,
        TUpdateTTL updateTtlResponse)
        where TGetFields : GetFields
        where TUpdateTTL : UpdateTtl
    {
        var cacheClient = Substitute.For<ICacheClient>();
        _ = cacheClient.DictionaryGetFieldsAsync(
                Arg.Any<string>(),
                Arg.Any<string>(),
                Arg.Any<IEnumerable<string>>())
            .Returns(getFieldsResponse);
        _ = cacheClient.UpdateTtlAsync(
                Arg.Any<string>(),
                Arg.Any<string>(),
                Arg.Any<TimeSpan>())
            .Returns(updateTtlResponse);
        return cacheClient;
    }
}
