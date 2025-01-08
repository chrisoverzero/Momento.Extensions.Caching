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

namespace Momento.Extensions.Caching;

/// <summary>A distributed cache implementation backed by Momento's <see cref="ICacheClient"/>.</summary>
/// <param name="cacheClient">The inner cache client.</param>
/// <param name="cacheOpts">The configuration options for the cache.</param>
/// <param name="timeProvider">A source of time.</param>
sealed partial class MomentoCache(ICacheClient cacheClient, IOptionsMonitor<MomentoCacheOptions> cacheOpts, TimeProvider timeProvider)
    : IDistributedCache
{
    /// <summary>The key to the field in which the cached value is stored.</summary>
    internal const string ValueKey = "v";

    /// <summary>The key to the field in which the sliding expiration value is stored.</summary>
    internal const string SlidingExpirationKey = "s";

    /// <summary>The key to the field in which the absolute expiration value is stored.</summary>
    internal const string AbsoluteExpirationKey = "a";

    static readonly FrozenSet<string> s_refreshFields = FrozenSet.ToFrozenSet([SlidingExpirationKey, AbsoluteExpirationKey]);
    static readonly FrozenSet<string> s_getFields = FrozenSet.ToFrozenSet([ValueKey, SlidingExpirationKey, AbsoluteExpirationKey]);

    /// <inheritdoc/>
    public Task<byte[]?> GetAsync(string key, CancellationToken token = default) => GetAndRefreshAsync(key, s_getFields, token);

    /// <inheritdoc/>
    public Task RefreshAsync(string key, CancellationToken token = default) => GetAndRefreshAsync(key, s_refreshFields, token);

    /// <inheritdoc/>
    public async Task RemoveAsync(string key, CancellationToken token = default)
    {
        var result = await cacheClient.DeleteAsync(cacheOpts.CurrentValue.CacheName, key).ConfigureAwait(false);
        if (result is Delete.Error { InnerException: { } ie })
        {
            throw ie;
        }
    }

    /// <inheritdoc/>
    public async Task SetAsync(string key, byte[] value, DistributedCacheEntryOptions options, CancellationToken token = default)
    {
        ArgumentNullException.ThrowIfNull(options);

        var now = timeProvider.GetUtcNow();
        var items = new Dictionary<string, byte[]>
        {
            [ValueKey] = value,
        };

        CollectionTtl ttl;
        switch ((RelativeExpiration: GetRelativeExpiration(now, options), options.SlidingExpiration))
        {
            case ({ } relative1, { } sliding1) when relative1 > sliding1:
                /* note(cosborn)
                 * When both a relative and a sliding expiration have been provided and the
                 * relative expiration is longer than the slide, the value can slide only as
                 * long as the provided relative expiration. The length of a slide and the
                 * calculated absolute expiration (which functions here as a slide limit) are
                 * stored next to the value so they can be read in `GetAndRefresh` for a TTL update.
                 */
                items[SlidingExpirationKey] = Marshal(sliding1);
                items[AbsoluteExpirationKey] = Marshal(now + relative1);
                ttl = CollectionTtl.Of(sliding1);
                break;
            case ({ } relative2, _):
                /* note(cosborn)
                 * When both a relative and a sliding expiration have been provided and the
                 * relative expiration is shorter than (or equal to) the slide, that's effectively
                 * identical to providing only a relative expiration. The sliding expiration
                 * is ignored and no other data are added.
                 */
                ttl = CollectionTtl.Of(relative2);
                break;
            case (_, { } sliding3):
                /* note(cosborn)
                 * When only a sliding expiration is provided, the value can slide effectively
                 * forever. The length of a slide is stored next to the value so it can be read
                 * in `GetAndRefresh` for a TTL update.
                 */
                items[SlidingExpirationKey] = Marshal(sliding3);
                ttl = CollectionTtl.Of(sliding3);
                break;
            default:
                /* note(cosborn)
                 * When no expiration is provided, the default TTL as provided when constructing
                 * the implementation of `ICacheClient` will be used as configured by the caller.
                 * This is _not necessarily_ the value of `cacheOpts.CurrentValue.DefaultTtl`, since an
                 * instance of a custom implementation of `ICacheClient` could have been resolved
                 * from DI which ignores this value. (Untidy, but not impossible.) Nicely,
                 * `CollectionTtl.FromCacheTtl()` means exactly this.
                 */
                ttl = CollectionTtl.FromCacheTtl();
                break;
        }

        token.ThrowIfCancellationRequested();

        var result = await cacheClient
            .DictionarySetFieldsAsync(cacheOpts.CurrentValue.CacheName, key, items, ttl)
            .ConfigureAwait(false);
        if (result is SetFields.Error { InnerException: { } ie })
        {
            throw ie;
        }

        [SuppressMessage("Microsoft.Style", "IDE0046", Justification = "It would look terrible.")]
        static TimeSpan? GetRelativeExpiration(DateTimeOffset now, DistributedCacheEntryOptions options)
        {
            if (options is { AbsoluteExpiration: { } ae } && ae <= now)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(options),
                    ae,
                    "The absolute expiration value must be in the future.");
            }

            return options.AbsoluteExpirationRelativeToNow ?? (options.AbsoluteExpiration - now);
        }
    }

    static byte[] Marshal(DateTimeOffset absolute) => Timestamp.FromDateTimeOffset(absolute).ToByteArray();

    static byte[] Marshal(TimeSpan sliding) => Duration.FromTimeSpan(sliding).ToByteArray();

    static DateTimeOffset ToAbsolute(byte[] absolute) => Timestamp.Parser.ParseFrom(absolute).ToDateTimeOffset();

    static TimeSpan ToSliding(byte[] sliding) => Duration.Parser.ParseFrom(sliding).ToTimeSpan();

    async Task<byte[]?> GetAndRefreshAsync<TFields>(string key, TFields fields, CancellationToken token)
        where TFields : IEnumerable<string> => await cacheClient
        .DictionaryGetFieldsAsync(cacheOpts.CurrentValue.CacheName, key, fields)
        .ConfigureAwait(false) switch
        {
            GetFields.Hit { ValueDictionaryStringByteArray: { } vd } => await GetAndRefreshCoreAsync(key, vd, token).ConfigureAwait(false),
            GetFields.Error { InnerException: { } ie } => throw ie,
            GetFields.Miss or _ => null,
        };

    async Task<byte[]?> GetAndRefreshCoreAsync<TDictionary>(string key, TDictionary dict, CancellationToken token)
        where TDictionary : IDictionary<string, byte[]>
    {
        if (dict.TryGetValue(SlidingExpirationKey, out var sliding))
        {
            /* note(cosborn)
             * The presence of a sliding expiration upon set means that
             * the TTL of the value should be refreshed upon read.
             * In the absence of other information, it's refreshed to a
             * constant value which was stored next to the cached value.
             */
            var ttl = ToSliding(sliding);
            if (dict.TryGetValue(AbsoluteExpirationKey, out var absoluteBytes))
            {
                /* note(cosborn)
                 * The presence of both a sliding and an absolute expiration upon set means that
                 * the absolute expiration acts as an upper limit on sliding. For refresh, the
                 * TTL should be updated to the _earlier_ of:
                 * - the next slide
                 * - the absolute expiration
                 */
                var absolute = ToAbsolute(absoluteBytes);
                ttl = Min(ttl, absolute - timeProvider.GetUtcNow());
            }

            token.ThrowIfCancellationRequested();

            var result = await cacheClient
                .UpdateTtlAsync(cacheOpts.CurrentValue.CacheName, key, ttl)
                .ConfigureAwait(false);
            if (result is UpdateTtl.Error { InnerException: { } ie })
            {
                throw ie;
            }
        }

        /* note(cosborn)
         * If this was a pure refresh operation, the lack of value key will return `null`,
         * which will then be discarded. Great. If this was a get operation and the
         * value key is mysteriously missing, returning `null` at least gives the caller
         * this impression that this was a miss (which it kind of was, I guess?) so they
         * can regenerate the value and set it into the cache.
         */
        return dict.TryGetValue(ValueKey, out var value) ? value : null;

        static TimeSpan Min(TimeSpan a, TimeSpan b) => a > b ? b : a;
    }
}
