// <copyright file="Remove.cs" company="Cimpress, Inc.">
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
public static class Remove
{
    [Property(DisplayName = "A success returns silently.")]
    public static async Task SuccessDoesNotThrow(MomentoCacheOptions cacheOpts, NonNull<string> key)
    {
        var simpleCacheClient = SetupDelete(new Delete.Success());
        IDistributedCache sut = new MomentoCache(simpleCacheClient.Object, cacheOpts);

        var result = await Record.ExceptionAsync(() => sut.RemoveAsync(key.Get));

        Assert.Null(result);
        simpleCacheClient.Verify(c => c.DeleteAsync(cacheOpts.CacheName, key.Get));
    }

    [Property(DisplayName = "A success returns silently, synchronously.")]
    public static void SuccessDoesNotThrow_sync(MomentoCacheOptions cacheOpts, NonNull<string> key)
    {
        var simpleCacheClient = SetupDelete(new Delete.Success());
        IDistributedCache sut = new MomentoCache(simpleCacheClient.Object, cacheOpts);

        var result = Record.Exception(() => sut.Remove(key.Get));

        Assert.Null(result);
        simpleCacheClient.Verify(c => c.DeleteAsync(cacheOpts.CacheName, key.Get));
    }

    [Property(DisplayName = "An error is converted to its wrapped exception.")]
    public static async Task ErrorThrows(MomentoCacheOptions cacheOpts, NonNull<string> key, Delete.Error err)
    {
        var simpleCacheClient = SetupDelete(err);
        IDistributedCache sut = new MomentoCache(simpleCacheClient.Object, cacheOpts);

        var result = await Record.ExceptionAsync(() => sut.RemoveAsync(key.Get));

        var se = Assert.IsAssignableFrom<SdkException>(result);
        Assert.Equal(se, err.InnerException);
        simpleCacheClient.Verify(c => c.DeleteAsync(cacheOpts.CacheName, key.Get));
    }

    [Property(DisplayName = "An error is converted to its wrapped exception, synchronously.")]
    public static void ErrorThrows_sync(MomentoCacheOptions cacheOpts, NonNull<string> key, Delete.Error err)
    {
        var simpleCacheClient = SetupDelete(err);
        IDistributedCache sut = new MomentoCache(simpleCacheClient.Object, cacheOpts);

        var result = Record.Exception(() => sut.Remove(key.Get));

        // note(cosborn) The important part of this test is that this _not_ be an `AggregateException`.
        var se = Assert.IsAssignableFrom<SdkException>(result);
        Assert.Equal(se, err.InnerException);
        simpleCacheClient.Verify(c => c.DeleteAsync(cacheOpts.CacheName, key.Get));
    }

    static Mock<ISimpleCacheClient> SetupDelete<TDelete>(TDelete response)
        where TDelete : Delete
    {
        var simpleCacheClient = new Mock<ISimpleCacheClient>();
        _ = simpleCacheClient
            .Setup(c => c.DeleteAsync(It.IsNotNull<string>(), It.IsNotNull<string>()))
            .ReturnsAsync(response);
        return simpleCacheClient;
    }
}
