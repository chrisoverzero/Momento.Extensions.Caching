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

static class Generators
{
    static readonly
#if NET9_0_OR_GREATER
    FrozenSet<ByteString>
#else
    HashSet<ByteString>
#endif
    s_fields =
    [
        ByteString.CopyFromUtf8(ValueKey),
        ByteString.CopyFromUtf8(SlidingExpirationKey),
        ByteString.CopyFromUtf8(AbsoluteExpirationKey),
    ];

    static readonly Arbitrary<SdkException> s_sdkException = Default.ArbFor<NonEmptyString>()
        .Convert(static s => s.Get, NonEmptyString.NewNonEmptyString)
        .Convert(static msg => (SdkException)new UnknownException(msg), static e => e.Message);

    static readonly Arbitrary<FieldValue> s_fieldValue = Default.ArbFor<NonEmptyArray<byte>>()
        .Convert(static bb => bb.Get, NonEmptyArray<byte>.NewNonEmptyArray)
        .Convert(ByteString.CopyFrom, static bs => bs.ToByteArray())
        .Convert(
            static bs => new FieldValue
            {
                CacheBody = bs,
                Result = Hit,
            },
            static fv => fv.CacheBody);

    public static Arbitrary<Ttl> Ttl { get; } = Gen.Choose(1, (int)TimeSpan.FromDays(365).TotalSeconds)
        .Select(static s => TimeSpan.FromSeconds(s))
        .Select(static t => new Ttl(t))
        .ToArbitrary();

    public static Arbitrary<MomentoCacheOptions> MomentoCacheOptions { get; } = Default.GeneratorFor<NonEmptyString>()
        .Zip(Ttl.Generator, (cacheName, defaultTtl) => new MomentoCacheOptions
        {
            CacheName = cacheName.Get,
            DefaultTtl = defaultTtl,
        })
        .ToArbitrary();

    public static Arbitrary<DistributedCacheEntryOptions> DistributedCacheEntryOptions { get; } = Gen.OneOf(
            Fixed.DistributedCacheEntryOptions.Generator,
            Refreshable.DistributedCacheEntryOptions.Generator)
        .ToArbitrary();

    public static Arbitrary<GetFields.Hit> GetFieldsHit { get; } = Gen.OneOf(
            Fixed.GetFieldsHit.Generator,
            Refreshable.GetFieldsHit.Generator)
        .ToArbitrary();

    public static Arbitrary<Delete.Error> DeleteError { get; } = Error(static ex => new Delete.Error(ex));

    public static Arbitrary<GetFields.Error> GetFieldsError { get; } = Error(static ex => new GetFields.Error(ex));

    public static Arbitrary<UpdateTtl.Error> UpdateTtlError { get; } = Error(static ex => new UpdateTtl.Error(ex));

    public static Arbitrary<SetFields.Error> SetFieldsError { get; } = Error(static ex => new SetFields.Error(ex));

    static FieldValue GetFieldMiss => new()
    {
        Result = Miss,
    };

    public static Arbitrary<IOptionsMonitor<T>> OptionsMonitorOf<T>()
        where T : class => Default.ArbFor<T>().Convert(OptionsMonitor.Create, static os => os.CurrentValue);

    static Arbitrary<TError> Error<TError>(Func<SdkException, TError> create)
        where TError : IError => s_sdkException.Convert(create, static e => e.InnerException);

    static Arbitrary<DistributedCacheEntryOptions> FromTtl(Func<Ttl, DistributedCacheEntryOptions> create) =>
        Ttl.Generator.Select(create).ToArbitrary();

    static _DictionaryGetResponse FromFields(FieldValue value, TimeSpan? sliding = null, DateTimeOffset? absolute = null)
    {
        return new()
        {
            Found = new()
            {
                Items =
                {
                    value,
                    sliding is { } s ? SlidingField(s) : GetFieldMiss,
                    absolute is { } a ? AbsoluteField(a) : GetFieldMiss,
                },
            },
        };

        static FieldValue SlidingField(TimeSpan slide) => new()
        {
            CacheBody = ByteString.CopyFrom(Marshal.To(slide)),
            Result = Hit,
        };

        static FieldValue AbsoluteField(DateTimeOffset absolute) => new()
        {
            CacheBody = ByteString.CopyFrom(Marshal.To(absolute)),
            Result = Hit,
        };
    }

    public static class Fixed
    {
        public static Arbitrary<DistributedCacheEntryOptions> DistributedCacheEntryOptions { get; } = Gen.OneOf(
                Default.DistributedCacheEntryOptions.Generator,
                Absolute.DistributedCacheEntryOptions.Generator,
                BlockedSlide.DistributedCacheEntryOptions.Generator)
            .ToArbitrary();

        public static Arbitrary<GetFields.Hit> GetFieldsHit { get; } = Arb.From(
            from value in s_fieldValue.Generator
            select FromFields(value) into responses
            select new GetFields.Hit(s_fields, responses));

        public static class Default
        {
            public static Arbitrary<DistributedCacheEntryOptions> DistributedCacheEntryOptions { get; } =
                Gen.Fresh(() => new DistributedCacheEntryOptions()).ToArbitrary();
        }

        public static class Absolute
        {
            public static Arbitrary<DistributedCacheEntryOptions> DistributedCacheEntryOptions { get; } =
                FromTtl(static ttl => new DistributedCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = ttl,
                });
        }

        public static class BlockedSlide
        {
            public static Arbitrary<DistributedCacheEntryOptions> DistributedCacheEntryOptions { get; } =
                FromTtl(static ttl => new DistributedCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = ttl - TimeSpan.FromSeconds(1),
                    SlidingExpiration = ttl,
                });
        }
    }

    public static class Refreshable
    {
        public static Arbitrary<DistributedCacheEntryOptions> DistributedCacheEntryOptions { get; } = Gen.OneOf(
                UnlimitedSlide.DistributedCacheEntryOptions.Generator,
                LimitedSlide.DistributedCacheEntryOptions.Generator)
            .ToArbitrary();

        public static Arbitrary<GetFields.Hit> GetFieldsHit { get; } = Gen.OneOf(
                UnlimitedSlide.GetFieldsHit.Generator,
                LimitedSlide.GetFieldsHit.Generator)
            .ToArbitrary();

        public static class UnlimitedSlide
        {
            public static Arbitrary<DistributedCacheEntryOptions> DistributedCacheEntryOptions { get; } =
                FromTtl(static ttl => new DistributedCacheEntryOptions
                {
                    SlidingExpiration = ttl,
                });

            public static Arbitrary<GetFields.Hit> GetFieldsHit { get; } = Arb.From(
                from value in s_fieldValue.Generator
                from slide in Ttl.Generator
                select FromFields(value, slide) into responses
                select new GetFields.Hit(s_fields, responses));
        }

        public static class LimitedSlide
        {
            public static Arbitrary<DistributedCacheEntryOptions> DistributedCacheEntryOptions { get; } =
                FromTtl(static ttl => new DistributedCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = ttl + TimeSpan.FromSeconds(1),
                    SlidingExpiration = ttl,
                });

            public static Arbitrary<GetFields.Hit> GetFieldsHit { get; } = Arb.From(
                from value in s_fieldValue.Generator
                from slide in Ttl.Generator
                from absolute in Default.GeneratorFor<DateTimeOffset>()
                select FromFields(value, slide, absolute) into responses
                select new GetFields.Hit(s_fields, responses));
        }
    }
}
