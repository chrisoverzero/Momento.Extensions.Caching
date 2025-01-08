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
public static class Remove
{
    [Property(DisplayName = "Any removal attempts to delete the value.")]
    public static async Task DeleteAsync(
        IOptionsMonitor<MomentoCacheOptions> cacheOpts,
        NonNull<string> key,
        TimeProvider time)
    {
        var cacheClient = Substitute.For<ICacheClient>();
        IDistributedCache sut = new MomentoCache(cacheClient, cacheOpts, time);

        await sut.RemoveAsync(key.Get);

        _ = await cacheClient.Received().DeleteAsync(cacheOpts.CurrentValue.CacheName, key.Get);
    }

    [Property(DisplayName = "Any removal attempts to delete the value, synchronously.")]
    public static void Delete(
        IOptionsMonitor<MomentoCacheOptions> cacheOpts,
        NonNull<string> key,
        TimeProvider time)
    {
        var cacheClient = Substitute.For<ICacheClient>();
        IDistributedCache sut = new MomentoCache(cacheClient, cacheOpts, time);

        _ = Record.Exception(() => sut.Remove(key.Get));

        _ = cacheClient.Received().DeleteAsync(cacheOpts.CurrentValue.CacheName, key.Get);
    }

    [Property(DisplayName = "Any removal does not attempt to update any item's TTL.")]
    public static async Task NoUpdateAsync(
        IOptionsMonitor<MomentoCacheOptions> cacheOpts,
        NonNull<string> key,
        TimeProvider time)
    {
        var cacheClient = Substitute.For<ICacheClient>();
        IDistributedCache sut = new MomentoCache(cacheClient, cacheOpts, time);

        await sut.RemoveAsync(key.Get);

        _ = await cacheClient.DidNotReceive().UpdateTtlAsync(
            Arg.Any<string>(),
            Arg.Any<string>(),
            Arg.Any<TimeSpan>());
    }

    [Property(DisplayName = "Any removal does not attempt to update any item's TTL, synchronously.")]
    public static void NoUpdate(
        IOptionsMonitor<MomentoCacheOptions> cacheOpts,
        NonNull<string> key,
        TimeProvider time)
    {
        var cacheClient = Substitute.For<ICacheClient>();
        IDistributedCache sut = new MomentoCache(cacheClient, cacheOpts, time);

        sut.Remove(key.Get);

        _ = cacheClient.DidNotReceive().UpdateTtlAsync(
            Arg.Any<string>(),
            Arg.Any<string>(),
            Arg.Any<TimeSpan>());
    }

    [Property(DisplayName = "An error is converted to its wrapped exception.")]
    public static async Task ErrorThrowsAsync(
        IOptionsMonitor<MomentoCacheOptions> cacheOpts,
        NonNull<string> key,
        TimeProvider time,
        Delete.Error error)
    {
        var cacheClient = SetUpScenario(error);
        IDistributedCache sut = new MomentoCache(cacheClient, cacheOpts, time);

        var actual = await Record.ExceptionAsync(() => sut.RemoveAsync(key.Get));

        var se = Assert.IsAssignableFrom<SdkException>(actual);
        Assert.Equal(error.InnerException, se);
    }

    [Property(DisplayName = "An error is converted to its wrapped exception, synchronously.")]
    public static void ErrorThrows(
        IOptionsMonitor<MomentoCacheOptions> cacheOpts,
        NonNull<string> key,
        TimeProvider time,
        Delete.Error error)
    {
        var cacheClient = SetUpScenario(error);
        IDistributedCache sut = new MomentoCache(cacheClient, cacheOpts, time);

        var actual = Record.Exception(() => sut.Remove(key.Get));

        // note(cosborn) The important part of this test is that this _not_ be an `AggregateException`.
        var se = Assert.IsAssignableFrom<SdkException>(actual);
        Assert.Equal(error.InnerException, se);
    }

    static ICacheClient SetUpScenario<TDelete>(TDelete response)
        where TDelete : Delete
    {
        var cacheClient = Substitute.For<ICacheClient>();
        _ = cacheClient.DeleteAsync(Arg.Any<string>(), Arg.Any<string>()).Returns(response);
        return cacheClient;
    }
}
