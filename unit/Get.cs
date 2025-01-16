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
public sealed class Get
    : IDisposable
{
    static readonly TimeProvider s_time = TimeProvider.System;

    readonly ICacheClient _cacheClient = Substitute.For<ICacheClient>();

    [Property(DisplayName = "Any get actually attempts to get the value.")]
    public async Task GetValueAsync(
        IOptionsMonitor<MomentoCacheOptions> cacheOpts,
        NonNull<string> key)
    {
        var sut = new MomentoCache(_cacheClient, cacheOpts, s_time);

        _ = await sut.GetAsync(key.Get);

        _ = await _cacheClient.Received().DictionaryGetFieldsAsync(
            cacheOpts.CurrentValue.CacheName,
            key.Get,
            Arg.Any<IEnumerable<string>>());
    }

    [Property(DisplayName = "Any get actually attempts to get the value, synchronously.")]
    public void GetValue(
        IOptionsMonitor<MomentoCacheOptions> cacheOpts,
        NonNull<string> key)
    {
        IDistributedCache sut = new MomentoCache(_cacheClient, cacheOpts, s_time);

        _ = sut.Get(key.Get);

        _ = _cacheClient.Received().DictionaryGetFieldsAsync(
            cacheOpts.CurrentValue.CacheName,
            key.Get,
            Arg.Any<IEnumerable<string>>());
    }

    [Property(DisplayName = "A miss returns null.")]
    public async Task MissNullAsync(
        IOptionsMonitor<MomentoCacheOptions> cacheOpts,
        NonNull<string> key,
        GetFields.Miss miss)
    {
        _cacheClient.SetUpScenario(miss);
        var sut = new MomentoCache(_cacheClient, cacheOpts, s_time);

        var actual = await sut.GetAsync(key.Get);

        Assert.Null(actual);
    }

    [Property(DisplayName = "A miss returns null, synchronously.")]
    public void MissNull(
        IOptionsMonitor<MomentoCacheOptions> cacheOpts,
        NonNull<string> key,
        GetFields.Miss miss)
    {
        _cacheClient.SetUpScenario(miss);
        IDistributedCache sut = new MomentoCache(_cacheClient, cacheOpts, s_time);

        var actual = sut.Get(key.Get);

        Assert.Null(actual);
    }

    [Property(DisplayName = "A miss does not attempt to update an item's TTL.")]
    public async Task MissNoUpdateAsync(
        IOptionsMonitor<MomentoCacheOptions> cacheOpts,
        NonNull<string> key,
        GetFields.Miss miss)
    {
        _cacheClient.SetUpScenario(miss);
        var sut = new MomentoCache(_cacheClient, cacheOpts, s_time);

        _ = await sut.GetAsync(key.Get);

        _ = await _cacheClient.DidNotReceive().UpdateTtlAsync(
            Arg.Any<string>(),
            Arg.Any<string>(),
            Arg.Any<TimeSpan>());
    }

    [Property(DisplayName = "A miss does not attempt to update an item's TTL, synchronously.")]
    public void MissNoUpdate(
        IOptionsMonitor<MomentoCacheOptions> cacheOpts,
        NonNull<string> key,
        GetFields.Miss miss)
    {
        _cacheClient.SetUpScenario(miss);
        IDistributedCache sut = new MomentoCache(_cacheClient, cacheOpts, s_time);

        _ = sut.Get(key.Get);

        _ = _cacheClient.DidNotReceive().UpdateTtlAsync(
            Arg.Any<string>(),
            Arg.Any<string>(),
            Arg.Any<TimeSpan>());
    }

    [Property(DisplayName = "An error initially getting the value throws.")]
    public async Task InitialErrorThrowsAsync(
        IOptionsMonitor<MomentoCacheOptions> cacheOpts,
        NonNull<string> key,
        GetFields.Error error)
    {
        _cacheClient.SetUpScenario(error);
        var sut = new MomentoCache(_cacheClient, cacheOpts, s_time);

        var actual = await Record.ExceptionAsync(() => sut.GetAsync(key.Get));

        var se = Assert.IsType<SdkException>(actual, exactMatch: false);
        Assert.Equal(error.InnerException, se);
    }

    [Property(DisplayName = "An error initially getting the value throws, synchronously.")]
    public void InitialErrorThrows(
        IOptionsMonitor<MomentoCacheOptions> cacheOpts,
        NonNull<string> key,
        GetFields.Error error)
    {
        _cacheClient.SetUpScenario(error);
        IDistributedCache sut = new MomentoCache(_cacheClient, cacheOpts, s_time);

        var actual = Record.Exception(() => sut.Get(key.Get));

        // note(cosborn) The important part of this test is that this _not_ be an `AggregateException`.
        var se = Assert.IsType<SdkException>(actual, exactMatch: false);
        Assert.Equal(error.InnerException, se);
    }

    [Property(DisplayName = "An error initially getting the value does not attempt to update any item's TTL.")]
    public async Task InitialErrorNoUpdateAsync(
        IOptionsMonitor<MomentoCacheOptions> cacheOpts,
        NonNull<string> key,
        GetFields.Error error)
    {
        _cacheClient.SetUpScenario(error);
        var sut = new MomentoCache(_cacheClient, cacheOpts, s_time);

        _ = await Record.ExceptionAsync(() => sut.GetAsync(key.Get));

        _ = await _cacheClient.DidNotReceive().UpdateTtlAsync(
            Arg.Any<string>(),
            Arg.Any<string>(),
            Arg.Any<TimeSpan>());
    }

    [Property(DisplayName = "An error initially getting the value does not attempt to update any item's TTL, synchronously.")]
    public void InitialErrorNoUpdate(
        IOptionsMonitor<MomentoCacheOptions> cacheOpts,
        NonNull<string> key,
        GetFields.Error error)
    {
        _cacheClient.SetUpScenario(error);
        IDistributedCache sut = new MomentoCache(_cacheClient, cacheOpts, s_time);

        _ = Record.Exception(() => sut.Get(key.Get));

        _ = _cacheClient.DidNotReceive().UpdateTtlAsync(
            Arg.Any<string>(),
            Arg.Any<string>(),
            Arg.Any<TimeSpan>());
    }

    [Property(DisplayName = "A hit of any kind returns the cached value.")]
    public async Task HitValueAsync(
        IOptionsMonitor<MomentoCacheOptions> cacheOpts,
        NonNull<string> key,
        GetFields.Hit hit)
    {
        _cacheClient.SetUpScenario(hit);
        var sut = new MomentoCache(_cacheClient, cacheOpts, s_time);

        var actual = await sut.GetAsync(key.Get);

        // note(cosborn) `null` is the indicator of a miss, so assert it especially.
        Assert.NotNull(actual);
        Assert.Equal(hit.ValueDictionaryStringByteArray[ValueKey], actual);
    }

    [Property(DisplayName = "A hit of any kind returns the cached value, synchronously.")]
    public void HitValue(
        IOptionsMonitor<MomentoCacheOptions> cacheOpts,
        NonNull<string> key,
        GetFields.Hit hit)
    {
        _cacheClient.SetUpScenario(hit);
        IDistributedCache sut = new MomentoCache(_cacheClient, cacheOpts, s_time);

        var actual = sut.Get(key.Get);

        // note(cosborn) `null` is the indicator of a miss, so assert it especially.
        Assert.NotNull(actual);
        Assert.Equal(hit.ValueDictionaryStringByteArray[ValueKey], actual);
    }

    [Property(
        Arbitrary = [typeof(Generators.Fixed)],
        DisplayName = "A hit with any fixed expiration does not attempt to update the item's TTL.")]
    public async Task HitFixedNoUpdateAsync(
        IOptionsMonitor<MomentoCacheOptions> cacheOpts,
        NonNull<string> key,
        GetFields.Hit hit)
    {
        _cacheClient.SetUpScenario(hit);
        var sut = new MomentoCache(_cacheClient, cacheOpts, s_time);

        _ = await sut.GetAsync(key.Get);

        _ = await _cacheClient.DidNotReceive().UpdateTtlAsync(
            Arg.Any<string>(),
            Arg.Any<string>(),
            Arg.Any<TimeSpan>());
    }

    [Property(
        Arbitrary = [typeof(Generators.Fixed)],
        DisplayName = "A hit with any fixed expiration does not attempt to update the item's TTL, synchronously.")]
    public void HitFixedNoUpdate(
        IOptionsMonitor<MomentoCacheOptions> cacheOpts,
        NonNull<string> key,
        GetFields.Hit hit)
    {
        _cacheClient.SetUpScenario(hit);
        IDistributedCache sut = new MomentoCache(_cacheClient, cacheOpts, s_time);

        _ = sut.Get(key.Get);

        _ = _cacheClient.DidNotReceive().UpdateTtlAsync(
            Arg.Any<string>(),
            Arg.Any<string>(),
            Arg.Any<TimeSpan>());
    }

    [Property(
        Arbitrary = [typeof(Generators.Refreshable)],
        DisplayName = "A hit with any refreshable expiration attempts to update the item's TTL.")]
    public async Task HitRefreshableUpdateAsync(
        IOptionsMonitor<MomentoCacheOptions> cacheOpts,
        NonNull<string> key,
        GetFields.Hit hit)
    {
        _cacheClient.SetUpScenario(hit);
        var sut = new MomentoCache(_cacheClient, cacheOpts, s_time);

        _ = await sut.GetAsync(key.Get);

        _ = await _cacheClient.Received().UpdateTtlAsync(
            cacheOpts.CurrentValue.CacheName,
            key.Get,
            Arg.Any<TimeSpan>());
    }

    [Property(
        Arbitrary = [typeof(Generators.Refreshable)],
        DisplayName = "A hit with any refreshable expiration attempts to update the item's TTL, synchronously.")]
    public void HitRefreshableUpdate(
        IOptionsMonitor<MomentoCacheOptions> cacheOpts,
        NonNull<string> key,
        GetFields.Hit hit)
    {
        _cacheClient.SetUpScenario(hit);
        IDistributedCache sut = new MomentoCache(_cacheClient, cacheOpts, s_time);

        _ = sut.Get(key.Get);

        _ = _cacheClient.Received().UpdateTtlAsync(
            cacheOpts.CurrentValue.CacheName,
            key.Get,
            Arg.Any<TimeSpan>());
    }

    [Property(
        Arbitrary = [typeof(Generators.Refreshable)],
        DisplayName = "An error on updating the TTL throws.")]
    public async Task UpdateTtlErrorThrowsAsync(
        IOptionsMonitor<MomentoCacheOptions> cacheOpts,
        NonNull<string> key,
        GetFields.Hit hit,
        UpdateTtl.Error error)
    {
        _cacheClient.SetUpScenario(hit, error);
        var sut = new MomentoCache(_cacheClient, cacheOpts, s_time);

        var actual = await Record.ExceptionAsync(() => sut.GetAsync(key.Get));

        var se = Assert.IsType<SdkException>(actual, exactMatch: false);
        Assert.Equal(error.InnerException, se);
    }

    [Property(
        Arbitrary = [typeof(Generators.Refreshable)],
        DisplayName = "An error on updating the TTL throws, synchronously.")]
    public void UpdateTtlErrorThrows(
        IOptionsMonitor<MomentoCacheOptions> cacheOpts,
        NonNull<string> key,
        GetFields.Hit hit,
        UpdateTtl.Error error)
    {
        _cacheClient.SetUpScenario(hit, error);
        IDistributedCache sut = new MomentoCache(_cacheClient, cacheOpts, s_time);

        var actual = Record.Exception(() => sut.Get(key.Get));

        // note(cosborn) The important part of this test is that this _not_ be an `AggregateException`.
        var se = Assert.IsType<SdkException>(actual, exactMatch: false);
        Assert.Equal(error.InnerException, se);
    }

    public void Dispose() => _cacheClient.Dispose();
}
