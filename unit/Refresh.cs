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

/// <summary>Tests of the Remove operation, async and sync.</summary>
[Properties(Arbitrary = [typeof(Generators)], MaxTest = 1024, QuietOnSuccess = true)]
public static class Refresh
{
    [Property(DisplayName = "A miss does not throw.")]
    public static async Task MissNoThrowAsync(
        IOptionsMonitor<MomentoCacheOptions> cacheOpts,
        NonNull<string> key,
        TimeProvider time,
        GetFields.Miss miss)
    {
        var cacheClient = SetUpScenario(miss);
        IDistributedCache sut = new MomentoCache(cacheClient, cacheOpts, time);

        var actual = await Record.ExceptionAsync(() => sut.RefreshAsync(key.Get));

        Assert.Null(actual);
    }

    [Property(DisplayName = "A miss does not throw, synchronously.")]
    public static void MissNoThrow(
        IOptionsMonitor<MomentoCacheOptions> cacheOpts,
        NonNull<string> key,
        TimeProvider time,
        GetFields.Miss miss)
    {
        var cacheClient = SetUpScenario(miss);
        IDistributedCache sut = new MomentoCache(cacheClient, cacheOpts, time);

        var actual = Record.Exception(() => sut.Refresh(key.Get));

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

        await sut.RefreshAsync(key.Get);

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

        sut.Refresh(key.Get);

        _ = cacheClient.DidNotReceive().UpdateTtlAsync(
            Arg.Any<string>(),
            Arg.Any<string>(),
            Arg.Any<TimeSpan>());
    }

    [Property(DisplayName = "An error initially getting the value throws.")]
    public static async Task GetErrorThrowsAsync(
        IOptionsMonitor<MomentoCacheOptions> cacheOpts,
        NonNull<string> key,
        TimeProvider time,
        GetFields.Error err)
    {
        var cacheClient = SetUpScenario(err);
        IDistributedCache sut = new MomentoCache(cacheClient, cacheOpts, time);

        var actual = await Record.ExceptionAsync(() => sut.RefreshAsync(key.Get));

        var se = Assert.IsAssignableFrom<SdkException>(actual);
        Assert.Equal(err.InnerException, se);
    }

    [Property(DisplayName = "An error initially getting the value throws, synchronously.")]
    public static void GetErrorThrows(
        IOptionsMonitor<MomentoCacheOptions> cacheOpts,
        NonNull<string> key,
        TimeProvider time,
        GetFields.Error err)
    {
        var cacheClient = SetUpScenario(err);
        IDistributedCache sut = new MomentoCache(cacheClient, cacheOpts, time);

        var actual = Record.Exception(() => sut.Refresh(key.Get));

        // note(cosborn) The important part of this test is that this _not_ be an `AggregateException`.
        var se = Assert.IsAssignableFrom<SdkException>(actual);
        Assert.Equal(err.InnerException, se);
    }

    [Property(DisplayName = "An error initially getting the value does not attempt to update an item's TTL.")]
    public static async Task GetErrorNoUpdateAsync(
        IOptionsMonitor<MomentoCacheOptions> cacheOpts,
        NonNull<string> key,
        TimeProvider time,
        GetFields.Error err)
    {
        var cacheClient = SetUpScenario(err);
        IDistributedCache sut = new MomentoCache(cacheClient, cacheOpts, time);

        _ = await Record.ExceptionAsync(() => sut.RefreshAsync(key.Get));

        _ = await cacheClient.DidNotReceive().UpdateTtlAsync(
            Arg.Any<string>(),
            Arg.Any<string>(),
            Arg.Any<TimeSpan>());
    }

    [Property(DisplayName = "An error initially getting the value does not attempt to update an item's TTL, synchronously.")]
    public static void GetNoUpdateThrows(
        IOptionsMonitor<MomentoCacheOptions> cacheOpts,
        NonNull<string> key,
        TimeProvider time,
        GetFields.Error err)
    {
        var cacheClient = SetUpScenario(err);
        IDistributedCache sut = new MomentoCache(cacheClient, cacheOpts, time);

        _ = Record.Exception(() => sut.Refresh(key.Get));

        _ = cacheClient.DidNotReceive().UpdateTtlAsync(
            Arg.Any<string>(),
            Arg.Any<string>(),
            Arg.Any<TimeSpan>());
    }

    [Property(
        Arbitrary = [typeof(Generators.Fixed)],
        DisplayName = "A hit with any fixed expiration does not attempt to update the item's TTL.")]
    public static async Task FixedValueDoesNotRefreshAsync(
        IOptionsMonitor<MomentoCacheOptions> cacheOpts,
        NonNull<string> key,
        TimeProvider time,
        GetFields.Hit hit)
    {
        var cacheClient = SetUpScenario(hit);
        IDistributedCache sut = new MomentoCache(cacheClient, cacheOpts, time);

        await sut.RefreshAsync(key.Get);

        _ = await cacheClient.DidNotReceive().UpdateTtlAsync(
            Arg.Any<string>(),
            Arg.Any<string>(),
            Arg.Any<TimeSpan>());
    }

    [Property(
        Arbitrary = [typeof(Generators.Fixed)],
        DisplayName = "A hit with any fixed expiration does not attempt to update the item's TTL, synchronously.")]
    public static void FixedValueDoesNotRefresh(
        IOptionsMonitor<MomentoCacheOptions> cacheOpts,
        NonNull<string> key,
        TimeProvider time,
        GetFields.Hit hit)
    {
        var cacheClient = SetUpScenario(hit);
        IDistributedCache sut = new MomentoCache(cacheClient, cacheOpts, time);

        sut.Refresh(key.Get);

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

        await sut.RefreshAsync(key.Get);

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

        sut.Refresh(key.Get);

        _ = cacheClient.Received().UpdateTtlAsync(
            cacheOpts.CurrentValue.CacheName,
            key.Get,
            Arg.Any<TimeSpan>());
    }

    [Property(
        Arbitrary = [typeof(Generators.Refreshable.LimitedSlide)],
        DisplayName = "A hit with a limited sliding expiration attempts to update the item's TTL.")]
    public static async Task LimitedSlidingValueRefreshesAsync(
        IOptionsMonitor<MomentoCacheOptions> cacheOpts,
        NonNull<string> key,
        TimeProvider time,
        GetFields.Hit hit)
    {
        var cacheClient = SetUpScenario(hit);
        IDistributedCache sut = new MomentoCache(cacheClient, cacheOpts, time);

        await sut.RefreshAsync(key.Get);

        _ = await cacheClient.Received().UpdateTtlAsync(
            cacheOpts.CurrentValue.CacheName,
            key.Get,
            Arg.Any<TimeSpan>());
    }

    [Property(
        Arbitrary = [typeof(Generators.Refreshable.LimitedSlide)],
        DisplayName = "A hit with a limited sliding expiration attempts to update the item's TTL, synchronously.")]
    public static void LimitedSlidingValueRefreshes(
        IOptionsMonitor<MomentoCacheOptions> cacheOpts,
        NonNull<string> key,
        TimeProvider time,
        GetFields.Hit hit)
    {
        var cacheClient = SetUpScenario(hit);
        IDistributedCache sut = new MomentoCache(cacheClient, cacheOpts, time);

        sut.Refresh(key.Get);

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

        var actual = await Record.ExceptionAsync(() => sut.RefreshAsync(key.Get));

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

        var actual = Record.Exception(() => sut.Refresh(key.Get));

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
