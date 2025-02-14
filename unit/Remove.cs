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
public sealed class Remove
    : IDisposable
{
    static readonly TimeProvider s_time = TimeProvider.System;

    readonly ICacheClient _cacheClient = Substitute.For<ICacheClient>();

    [Property(DisplayName = "Any removal attempts to delete the value.")]
    public async Task DeleteAsync(
        IOptionsMonitor<MomentoCacheOptions> cacheOpts,
        NonNull<string> key)
    {
        var sut = new MomentoCache(_cacheClient, cacheOpts, s_time);

        await sut.RemoveAsync(key.Get);

        _ = await _cacheClient.Received().DeleteAsync(cacheOpts.CurrentValue.CacheName, key.Get);
    }

    [Property(DisplayName = "Any removal attempts to delete the value, synchronously.")]
    public void Delete(
        IOptionsMonitor<MomentoCacheOptions> cacheOpts,
        NonNull<string> key)
    {
        IDistributedCache sut = new MomentoCache(_cacheClient, cacheOpts, s_time);

        _ = Record.Exception(() => sut.Remove(key.Get));

        _ = _cacheClient.Received().DeleteAsync(cacheOpts.CurrentValue.CacheName, key.Get);
    }

    [Property(DisplayName = "Any removal does not attempt to update any item's TTL.")]
    public async Task NoUpdateAsync(
        IOptionsMonitor<MomentoCacheOptions> cacheOpts,
        NonNull<string> key)
    {
        var sut = new MomentoCache(_cacheClient, cacheOpts, s_time);

        await sut.RemoveAsync(key.Get);

        _ = await _cacheClient.DidNotReceive().UpdateTtlAsync(
            Arg.Any<string>(),
            Arg.Any<string>(),
            Arg.Any<TimeSpan>());
    }

    [Property(DisplayName = "Any removal does not attempt to update any item's TTL, synchronously.")]
    public void NoUpdate(
        IOptionsMonitor<MomentoCacheOptions> cacheOpts,
        NonNull<string> key)
    {
        IDistributedCache sut = new MomentoCache(_cacheClient, cacheOpts, s_time);

        sut.Remove(key.Get);

        _ = _cacheClient.DidNotReceive().UpdateTtlAsync(
            Arg.Any<string>(),
            Arg.Any<string>(),
            Arg.Any<TimeSpan>());
    }

    [Property(DisplayName = "An error is converted to its wrapped exception.")]
    public async Task ErrorThrowsAsync(
        IOptionsMonitor<MomentoCacheOptions> cacheOpts,
        NonNull<string> key,
        Delete.Error error)
    {
        _cacheClient.SetUpScenario(error);
        var sut = new MomentoCache(_cacheClient, cacheOpts, s_time);

        var actual = await Record.ExceptionAsync(() => sut.RemoveAsync(key.Get));

        var se = Assert.IsType<SdkException>(actual, exactMatch: false);
        Assert.Equal(error.InnerException, se);
    }

    [Property(DisplayName = "An error is converted to its wrapped exception, synchronously.")]
    public void ErrorThrows(
        IOptionsMonitor<MomentoCacheOptions> cacheOpts,
        NonNull<string> key,
        Delete.Error error)
    {
        _cacheClient.SetUpScenario(error);
        IDistributedCache sut = new MomentoCache(_cacheClient, cacheOpts, s_time);

        var actual = Record.Exception(() => sut.Remove(key.Get));

        // note(cosborn) The important part of this test is that this _not_ be an `AggregateException`.
        var se = Assert.IsType<SdkException>(actual, exactMatch: false);
        Assert.Equal(error.InnerException, se);
    }

    public void Dispose() => _cacheClient.Dispose();
}
