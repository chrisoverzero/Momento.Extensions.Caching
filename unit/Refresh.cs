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

using Google.Protobuf;
using static Momento.Protos.CacheClient.ECacheResult;
using FieldValue = Momento.Protos.CacheClient._DictionaryGetResponse.Types._DictionaryGetResponsePart;

namespace Momento.Extensions.Caching.Unit;

/// <summary>Tests of the Remove operation, async and sync.</summary>
[Properties(Arbitrary = new[] { typeof(Generators) }, QuietOnSuccess = true)]
public static class Refresh
{
    static readonly ByteString s_valueKey = ByteString.CopyFromUtf8("v");
    static readonly ByteString s_slidingExpirationKey = ByteString.CopyFromUtf8("s");
    static readonly ByteString s_absoluteExpirationKey = ByteString.CopyFromUtf8("a");

    static readonly ByteString[] s_fields = { s_valueKey, s_slidingExpirationKey, s_absoluteExpirationKey };

    static FieldValue GetFieldMiss => new()
    {
        Result = Miss,
    };

    [Property(DisplayName = "A miss returns silently.")]
    public static async Task MissDoesNotThrow(MomentoCacheOptions cacheOpts, NonNull<string> key)
    {
        var simpleCacheClient = SetupScenario(new GetFields.Miss());
        IDistributedCache sut = new MomentoCache(simpleCacheClient.Object, cacheOpts);

        await sut.RefreshAsync(key.Get);

        simpleCacheClient.Verify(c => c.DictionaryGetFieldsAsync(
            cacheOpts.CacheName,
            key.Get,
            It.Is<IEnumerable<string>>(static fs => fs.All(static f => f != null))));
        simpleCacheClient.VerifyNoOtherCalls();
    }

    [Property(DisplayName = "A miss returns silently, synchronously.")]
    public static void MissDoesNotThrow_sync(MomentoCacheOptions cacheOpts, NonNull<string> key)
    {
        var simpleCacheClient = SetupScenario(new GetFields.Miss());
        IDistributedCache sut = new MomentoCache(simpleCacheClient.Object, cacheOpts);

        sut.Refresh(key.Get);

        simpleCacheClient.Verify(c => c.DictionaryGetFieldsAsync(
            cacheOpts.CacheName,
            key.Get,
            It.Is<IEnumerable<string>>(static fs => fs.All(static f => f != null))));
        simpleCacheClient.VerifyNoOtherCalls();
    }

    [Property(DisplayName = "An error initially getting the fields throws.")]
    public static async Task GetFieldsErrorThrows(MomentoCacheOptions cacheOpts, NonNull<string> key, GetFields.Error err)
    {
        var simpleCacheClient = SetupScenario(err);
        IDistributedCache sut = new MomentoCache(simpleCacheClient.Object, cacheOpts);

        var result = await Record.ExceptionAsync(() => sut.RefreshAsync(key.Get));

        var se = Assert.IsAssignableFrom<SdkException>(result);
        Assert.Equal(err.Exception, se);
        simpleCacheClient.Verify(c => c.DictionaryGetFieldsAsync(
            cacheOpts.CacheName,
            key.Get,
            It.Is<IEnumerable<string>>(static fs => fs.All(static f => f != null))));
        simpleCacheClient.VerifyNoOtherCalls();
    }

    [Property(DisplayName = "An error initially getting the fields throws, synchronously.")]
    public static void GetFieldsErrorThrows_sync(MomentoCacheOptions cacheOpts, NonNull<string> key, GetFields.Error err)
    {
        var simpleCacheClient = SetupScenario(err);
        IDistributedCache sut = new MomentoCache(simpleCacheClient.Object, cacheOpts);

        var result = Record.Exception(() => sut.Refresh(key.Get));

        // note(cosborn) The important part of this test is that this _not_ be an `AggregateException`.
        var se = Assert.IsAssignableFrom<SdkException>(result);
        Assert.Equal(err.Exception, se);
        simpleCacheClient.Verify(c => c.DictionaryGetFieldsAsync(
            cacheOpts.CacheName,
            key.Get,
            It.Is<IEnumerable<string>>(static fs => fs.All(static f => f != null))));
        simpleCacheClient.VerifyNoOtherCalls();
    }

    [Property(DisplayName = "A hit on a value with no slide does not refresh.")]
    public static async Task FixedValueDoesNotRefresh(MomentoCacheOptions cacheOpts, NonNull<string> key, NonEmptyArray<byte> value)
    {
        var hit = GetFieldsHit(GetFieldHit(value.Get), GetFieldMiss, GetFieldMiss);
        var simpleCacheClient = SetupScenario(hit);
        IDistributedCache sut = new MomentoCache(simpleCacheClient.Object, cacheOpts);

        await sut.RefreshAsync(key.Get);
        simpleCacheClient.Verify(c => c.DictionaryGetFieldsAsync(
            cacheOpts.CacheName,
            key.Get,
            It.Is<IEnumerable<string>>(static fs => fs.All(static f => f != null))));
        simpleCacheClient.VerifyNoOtherCalls();
    }

    [Property(DisplayName = "A hit on a value with no slide does not refresh.")]
    public static void FixedValueDoesNotRefresh_sync(MomentoCacheOptions cacheOpts, NonNull<string> key, NonEmptyArray<byte> value)
    {
        var hit = GetFieldsHit(GetFieldHit(value.Get), GetFieldMiss, GetFieldMiss);
        var simpleCacheClient = SetupScenario(hit);
        IDistributedCache sut = new MomentoCache(simpleCacheClient.Object, cacheOpts);

        sut.Refresh(key.Get);
        simpleCacheClient.Verify(c => c.DictionaryGetFieldsAsync(
            cacheOpts.CacheName,
            key.Get,
            It.Is<IEnumerable<string>>(static fs => fs.All(static f => f != null))));
        simpleCacheClient.VerifyNoOtherCalls();
    }

    static Mock<ISimpleCacheClient> SetupScenario<TGetFields>(TGetFields getFieldsResponse)
        where TGetFields : GetFields
    {
        var simpleCacheClient = new Mock<ISimpleCacheClient>();
        _ = simpleCacheClient
            .Setup(static c => c.DictionaryGetFieldsAsync(
                It.IsNotNull<string>(),
                It.IsNotNull<string>(),
                It.Is<IEnumerable<string>>(static fs => fs.All(static f => f != null))))
            .ReturnsAsync(getFieldsResponse);
        return simpleCacheClient;
    }

    static FieldValue GetFieldHit(ReadOnlySpan<byte> bytes) => new()
    {
        CacheBody = ByteString.CopyFrom(bytes),
        Result = Hit,
    };

    static GetFields.Hit GetFieldsHit(FieldValue value, FieldValue sliding, FieldValue absolute) => new(s_fields, new()
    {
        Found = new()
        {
            Items = { value, sliding, absolute },
        },
    });
}
