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
    public static Arbitrary<MomentoCacheOptions> MomentoCacheOptions { get; } = Arb.From(
        from cacheName in Default.GeneratorFor<NonEmptyString>()
        from defaultTtl in Default.GeneratorFor<TimeSpan>().Where(ts => ts != TimeSpan.Zero).Select(ts => ts.Duration())
        select new MomentoCacheOptions
        {
            CacheName = cacheName.Get,
            DefaultTtl = defaultTtl,
        });

    public static Arbitrary<SdkException> SdkException { get; } = Default
        .GeneratorFor<NonEmptyString>()
        .Select(msg => (SdkException)new ImmaterialException(msg.Get))
        .ToArbitrary();

    public static Arbitrary<Delete.Error> CacheDeleteResponseError { get; } = SdkException.Generator
        .Select(ex => new Delete.Error(ex))
        .ToArbitrary();

    sealed class ImmaterialException
        : SdkException
    {
        public ImmaterialException()
            : this(string.Empty)
        {
        }

        public ImmaterialException(string message)
            : this(0, message)
        {
        }

        public ImmaterialException(string message, Exception innerException)
            : this(0, message, e: innerException)
        {
        }

        public ImmaterialException(MomentoErrorCode errorCode, string message, MomentoErrorTransportDetails? transportDetails = null, Exception? e = null)
            : base(errorCode, message, transportDetails, e)
        {
        }
    }
}
