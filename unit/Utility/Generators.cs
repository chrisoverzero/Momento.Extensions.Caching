// <copyright file="Generators.cs" company="Cimpress, Inc.">
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
using Momento.Protos.CacheClient;
using static Momento.Protos.CacheClient.ECacheResult;
using FieldValue = Momento.Protos.CacheClient._DictionaryGetResponse.Types._DictionaryGetResponsePart;

namespace Momento.Extensions.Caching.Unit;

static class Generators
{
    public static readonly ByteString[] Fields =
    {
        ByteString.CopyFromUtf8(ValueKey),
        ByteString.CopyFromUtf8(SlidingExpirationKey),
        ByteString.CopyFromUtf8(AbsoluteExpirationKey),
    };

    static readonly Gen<UnknownException> s_sdkException = Default.GeneratorFor<NonEmptyString>()
        .Select(msg => new UnknownException(msg.Get));

    static readonly Gen<FieldValue> s_value =
        from value in Default.GeneratorFor<NonEmptyArray<byte>>()
        select ByteString.CopyFrom(value.Get) into cacheBody
        select new FieldValue()
        {
            CacheBody = cacheBody,
            Result = Hit,
        };

    static readonly Gen<TimeSpan> s_ttl =
        from ts in Default.GeneratorFor<TimeSpan>()
        where ts != TimeSpan.Zero
        select ts.Duration();

    static readonly Gen<FieldValue> s_slidingExpiration =
        from slide in s_ttl
        select slide.ToString("c", InvariantCulture) into ttlString
        select ByteString.CopyFromUtf8(ttlString) into cacheBody
        select new FieldValue()
        {
            CacheBody = cacheBody,
            Result = Hit,
        };

    public static Arbitrary<Duration> Ttl { get; } = s_ttl
        .Select(t => new Duration(t))
        .ToArbitrary();

    public static Arbitrary<MomentoCacheOptions> MomentoCacheOptions { get; } = Arb.From(
        from cacheName in Default.GeneratorFor<NonEmptyString>()
        from defaultTtl in s_ttl
        select new MomentoCacheOptions
        {
            CacheName = cacheName.Get,
            DefaultTtl = defaultTtl,
        });

    public static Arbitrary<Delete.Error> CacheDeleteResponseError { get; } = s_sdkException
        .Select(static ex => new Delete.Error(ex))
        .ToArbitrary();

    public static Arbitrary<GetFields.Error> CacheDictionaryGetFieldsResponseError { get; } = s_sdkException
        .Select(static ex => new GetFields.Error(ex))
        .ToArbitrary();

    public static Arbitrary<Increment.Error> CacheDictionaryIncrementResponse { get; } = s_sdkException
        .Select(static ex => new Increment.Error(ex))
        .ToArbitrary();

    public static Arbitrary<SetFields.Error> CacheDictionarySetFieldsResponse { get; } = s_sdkException
        .Select(static ex => new SetFields.Error(ex))
        .ToArbitrary();

    public static Arbitrary<FixedValueGetFieldsHit> FixedValueGetFieldsHit { get; } = Arb.From(
        from value in s_value
        select new _DictionaryGetResponse
        {
            Found = new()
            {
                Items = { value, GetFieldMiss, GetFieldMiss },
            },
        }
        into responses
        select new FixedValueGetFieldsHit(responses));

    public static Arbitrary<SlidingValueGetFieldsHit> SlidingValueGetFieldsHit { get; } = Arb.From(
        from value in s_value
        from sliding in s_slidingExpiration
        select new _DictionaryGetResponse
        {
            Found = new()
            {
                Items = { value, sliding, GetFieldMiss },
            },
        }
        into responses
        select new SlidingValueGetFieldsHit(responses));

    static FieldValue GetFieldMiss => new()
    {
        Result = Miss,
    };
}
