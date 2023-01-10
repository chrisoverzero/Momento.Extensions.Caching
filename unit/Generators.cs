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

namespace Momento.Extensions.Caching.Unit;

static class Generators
{
    static readonly Gen<UnknownException> s_sdkException = Default.GeneratorFor<NonEmptyString>()
        .Select(msg => new UnknownException(msg.Get));

    public static Arbitrary<MomentoCacheOptions> MomentoCacheOptions { get; } = Arb.From(
        from cacheName in Default.GeneratorFor<NonEmptyString>()
        from defaultTtl in Default.GeneratorFor<TimeSpan>().Where(ts => ts != TimeSpan.Zero).Select(ts => ts.Duration())
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
}
