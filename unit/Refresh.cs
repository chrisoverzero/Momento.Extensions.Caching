// <copyright file="Refresh.cs" company="Cimpress, Inc.">
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
public static class Refresh
{
    [Property(DisplayName = "A miss returns silently.")]
    public static async Task MissSilent(
        MomentoCacheOptions cacheOpts,
        NonNull<string> key,
        GetFields.Miss miss)
    {
        var simpleCacheClient = SetupScenario(miss);
        IDistributedCache sut = new MomentoCache(simpleCacheClient.Object, cacheOpts);

        await sut.RefreshAsync(key.Get);

        simpleCacheClient.Verify(c => c.DictionaryGetFieldsAsync(
            cacheOpts.CacheName,
            key.Get,
            It.IsNotNull<IEnumerable<string>>()));
        simpleCacheClient.Verify(
            static c => c.DictionaryIncrementAsync(
                It.IsNotNull<string>(),
                It.IsNotNull<string>(),
                It.IsNotNull<string>(),
                It.IsAny<long>(),
                It.IsAny<CollectionTtl>()),
            Times.Never);
    }

    [Property(DisplayName = "A miss returns silently, synchronously.")]
    public static void MissSilent_sync(
        MomentoCacheOptions cacheOpts,
        NonNull<string> key,
        GetFields.Miss miss)
    {
        var simpleCacheClient = SetupScenario(miss);
        IDistributedCache sut = new MomentoCache(simpleCacheClient.Object, cacheOpts);

        sut.Refresh(key.Get);

        simpleCacheClient.Verify(c => c.DictionaryGetFieldsAsync(
            cacheOpts.CacheName,
            key.Get,
            It.IsNotNull<IEnumerable<string>>()));
        simpleCacheClient.Verify(
            static c => c.DictionaryIncrementAsync(
                It.IsNotNull<string>(),
                It.IsNotNull<string>(),
                It.IsNotNull<string>(),
                It.IsAny<long>(),
                It.IsAny<CollectionTtl>()),
            Times.Never);
    }

    [Property(DisplayName = "An error initially getting the fields throws.")]
    public static async Task GetFieldsErrorThrows(
        MomentoCacheOptions cacheOpts,
        NonNull<string> key,
        GetFields.Error err)
    {
        var simpleCacheClient = SetupScenario(err);
        IDistributedCache sut = new MomentoCache(simpleCacheClient.Object, cacheOpts);

        var actual = await Record.ExceptionAsync(() => sut.RefreshAsync(key.Get));

        var se = Assert.IsAssignableFrom<SdkException>(actual);
        Assert.Equal(err.Exception, se);
        simpleCacheClient.Verify(c => c.DictionaryGetFieldsAsync(
            cacheOpts.CacheName,
            key.Get,
            It.IsNotNull<IEnumerable<string>>()));
        simpleCacheClient.Verify(
            static c => c.DictionaryIncrementAsync(
                It.IsNotNull<string>(),
                It.IsNotNull<string>(),
                It.IsNotNull<string>(),
                It.IsAny<long>(),
                It.IsAny<CollectionTtl>()),
            Times.Never);
    }

    [Property(DisplayName = "An error initially getting the fields throws, synchronously.")]
    public static void GetFieldsErrorThrows_sync(
        MomentoCacheOptions cacheOpts,
        NonNull<string> key,
        GetFields.Error err)
    {
        var simpleCacheClient = SetupScenario(err);
        IDistributedCache sut = new MomentoCache(simpleCacheClient.Object, cacheOpts);

        var actual = Record.Exception(() => sut.Refresh(key.Get));

        // note(cosborn) The important part of this test is that this _not_ be an `AggregateException`.
        var se = Assert.IsAssignableFrom<SdkException>(actual);
        Assert.Equal(err.Exception, se);
        simpleCacheClient.Verify(c => c.DictionaryGetFieldsAsync(
            cacheOpts.CacheName,
            key.Get,
            It.IsNotNull<IEnumerable<string>>()));
        simpleCacheClient.Verify(
            static c => c.DictionaryIncrementAsync(
                It.IsNotNull<string>(),
                It.IsNotNull<string>(),
                It.IsNotNull<string>(),
                It.IsAny<long>(),
                It.IsAny<CollectionTtl>()),
            Times.Never);
    }

    [Property(DisplayName = "A hit on a value with no slide does not refresh.")]
    public static async Task FixedValueDoesNotRefresh(
        MomentoCacheOptions cacheOpts,
        NonNull<string> key,
        FixedValueGetFieldsHit hit)
    {
        var simpleCacheClient = SetupScenario(hit);
        IDistributedCache sut = new MomentoCache(simpleCacheClient.Object, cacheOpts);

        await sut.RefreshAsync(key.Get);

        simpleCacheClient.Verify(c => c.DictionaryGetFieldsAsync(
            cacheOpts.CacheName,
            key.Get,
            It.IsNotNull<IEnumerable<string>>()));
        simpleCacheClient.Verify(
            static c => c.DictionaryIncrementAsync(
                It.IsNotNull<string>(),
                It.IsNotNull<string>(),
                It.IsNotNull<string>(),
                It.IsAny<long>(),
                It.IsAny<CollectionTtl>()),
            Times.Never);
    }

    [Property(DisplayName = "A hit on a value with no slide does not refresh, synchronously.")]
    public static void FixedValueDoesNotRefresh_sync(
        MomentoCacheOptions cacheOpts,
        NonNull<string> key,
        FixedValueGetFieldsHit hit)
    {
        var simpleCacheClient = SetupScenario(hit);
        IDistributedCache sut = new MomentoCache(simpleCacheClient.Object, cacheOpts);

        sut.Refresh(key.Get);

        simpleCacheClient.Verify(c => c.DictionaryGetFieldsAsync(
            cacheOpts.CacheName,
            key.Get,
            It.IsNotNull<IEnumerable<string>>()));
        simpleCacheClient.Verify(
            static c => c.DictionaryIncrementAsync(
                It.IsNotNull<string>(),
                It.IsNotNull<string>(),
                It.IsNotNull<string>(),
                It.IsAny<long>(),
                It.IsAny<CollectionTtl>()),
            Times.Never);
    }

    [Property(DisplayName = "An error on updating the TTL throws.")]
    public static async Task IncrementErrorThrows(
        MomentoCacheOptions cacheOpts,
        NonNull<string> key,
        SlidingValueGetFieldsHit hit,
        Increment.Error err)
    {
        var simpleCacheClient = SetupScenario(hit, err);
        IDistributedCache sut = new MomentoCache(simpleCacheClient.Object, cacheOpts);

        var actual = await Record.ExceptionAsync(() => sut.RefreshAsync(key.Get));

        var se = Assert.IsAssignableFrom<SdkException>(actual);
        Assert.Equal(se, err.Exception);
        simpleCacheClient.Verify(c => c.DictionaryGetFieldsAsync(
            cacheOpts.CacheName,
            key.Get,
            It.IsNotNull<IEnumerable<string>>()));
        simpleCacheClient.Verify(c => c.DictionaryIncrementAsync(
            cacheOpts.CacheName,
            key.Get,
            RefreshTtlKey,
            It.IsAny<long>(),
            It.IsAny<CollectionTtl>()));
    }

    [Property(DisplayName = "An error on updating the TTL throws, synchronously.")]
    public static void IncrementErrorThrows_sync(
        MomentoCacheOptions cacheOpts,
        NonNull<string> key,
        SlidingValueGetFieldsHit hit,
        Increment.Error err)
    {
        var simpleCacheClient = SetupScenario(hit, err);
        IDistributedCache sut = new MomentoCache(simpleCacheClient.Object, cacheOpts);

        var actual = Record.Exception(() => sut.Refresh(key.Get));

        // note(cosborn) The important part of this test is that this _not_ be an `AggregateException`.
        var se = Assert.IsAssignableFrom<SdkException>(actual);
        Assert.Equal(se, err.Exception);
        simpleCacheClient.Verify(c => c.DictionaryGetFieldsAsync(
            cacheOpts.CacheName,
            key.Get,
            It.IsNotNull<IEnumerable<string>>()));
        simpleCacheClient.Verify(c => c.DictionaryIncrementAsync(
            cacheOpts.CacheName,
            key.Get,
            RefreshTtlKey,
            It.IsAny<long>(),
            It.IsAny<CollectionTtl>()));
    }

    static Mock<ISimpleCacheClient> SetupScenario<TGetFields>(TGetFields getFieldsResponse)
        where TGetFields : GetFields
    {
        var simpleCacheClient = new Mock<ISimpleCacheClient>();
        _ = simpleCacheClient
            .Setup(static c => c.DictionaryGetFieldsAsync(
                It.IsNotNull<string>(),
                It.IsNotNull<string>(),
                It.IsNotNull<IEnumerable<string>>()))
            .ReturnsAsync(getFieldsResponse);
        return simpleCacheClient;
    }

    static Mock<ISimpleCacheClient> SetupScenario<TGetFields, TIncrement>(
        TGetFields getFieldsResponse,
        TIncrement incrementResponse)
        where TGetFields : GetFields
        where TIncrement : Increment
    {
        var simpleCacheClient = new Mock<ISimpleCacheClient>();
        var sequence = new MockSequence();
        _ = simpleCacheClient
            .InSequence(sequence)
            .Setup(static c => c.DictionaryGetFieldsAsync(
                It.IsNotNull<string>(),
                It.IsNotNull<string>(),
                It.IsNotNull<IEnumerable<string>>()))
            .ReturnsAsync(getFieldsResponse);
        _ = simpleCacheClient
            .InSequence(sequence)
            .Setup(static c => c.DictionaryIncrementAsync(
                It.IsNotNull<string>(),
                It.IsNotNull<string>(),
                It.IsNotNull<string>(),
                It.IsAny<long>(),
                It.IsAny<CollectionTtl>()))
            .ReturnsAsync(incrementResponse);

        return simpleCacheClient;
    }
}
