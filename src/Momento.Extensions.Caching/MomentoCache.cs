// <copyright file="MomentoCache.cs" company="Cimpress, Inc.">
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

namespace Momento.Extensions.Caching;

/* todo(cosborn)
 * Since we're going through the trouble of minimizing bytes sent and recieved in a few places
 * (short keys, eliding the refresh value, &c.), it would make sense to make the representations
 * of the sliding and absolute expirations as short as possible, as well. Minimizing bytes is
 * important for minimizing cost. Here are a few ideas for doing that.
 *
 * First, values could be stored with only millisecond precision. That saves… four bytes apiece,
 * when present, since the round-trippy formats currently used go out to seven decimal places,
 * and milliseconds use only three.
 *
 * ```
 * 2023-01-08T01:58:46.2383808+00:00
 * |-------------------------------| 33 bytes
 *
 * 2023-01-08T01:58:46.238+00:00
 * |---------------------------| 29 bytes
 * ```
 *
 * Additionally, the absolute expiration could be time-zoned to "Z". Since it's generated as a
 * UTC value, it will serialize in the round-trip format with an offset "+00:00". That's a
 * savings of another 5 bytes, when present.
 *
 * ```
 * 2023-01-08T01:58:46.238Z
 * |----------------------| 24 bytes
 * ```
 *
 * Second, the expirations could be stored as raw milliseconds. For a timestamp, that's a savings
 * of 20 bytes, when present.
 *
 * ```
 * 2023-01-08T01:58:46.2383808+00:00
 * |-------------------------------| 33 bytes
 *
 * 1673143126238
 * |-----------| 13 bytes
 * ```
 *
 * For a timespan, it's variable. When a timespan lacks a fractional number of seconds, it's
 * elided from the string representation. This is a small savings (dependent on the dutation)
 * when present. When fractional seconds are present, the savings are extremely variable, but
 * about 8 bytes for expected TTLs. As an example, the savings for a TTL of 30 seconds is three
 * bytes:
 *
 * ```
 * 00:00:30
 * |------| 8 bytes
 *
 * 30000
 * |---| 5 bytes
 * ```
 *
 * …but the savings for a TTL of 30.1 seconds is eleven bytes!
 *
 * ```
 * 00:00:30.1000000
 * |--------------| 16 bytes
 *
 * 30100
 * |---| 5 bytes
 * ```
 *
 * Third, those milliseconds could be stored in a binary serialization. This would involve conversion
 * to milliseconds and a call to `BitConverter.GetBytes`. For a timestamp, this is a constant eight
 * bytes – a savings of 25 bytes from the round-trip format and a savings of five bytes from the
 * millisecond string. For a timespan, the savings are negative until TTLs go above ca. 3 hours.
 * This strategy is troublesome, though, since we run into little- vs. big-endianness problems.
 * Nothing says that an application cluster is uniformly endian (except the prevalence of little-
 * endianness, I guess), and reversing that array seems _expensive_.
 *
 * The second proposition seems to be the best balance.
 *
 * But I'm writing tests first.
 */

/// <summary>A distributed cache implementation backed by Momento's <see cref="ISimpleCacheClient"/>.</summary>
public sealed class MomentoCache
    : IDistributedCache
{
    /// <summary>The key to the field in which the cached value is stored.</summary>
    internal const string ValueKey = "v";

    /// <summary>The key to the field in which the sliding expiration value is stored.</summary>
    internal const string SlidingExpirationKey = "s";

    /// <summary>The key to the field in which the absolute expiration value is stored.</summary>
    internal const string AbsoluteExpirationKey = "a";

    /// <summary>The key to the field in which the refresh value is stored.</summary>
    internal const string RefreshTtlKey = "r";

    static readonly ImmutableArray<string> s_fields = ImmutableArray.Create(ValueKey, SlidingExpirationKey, AbsoluteExpirationKey);
    static readonly Encoding s_utf8 = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false);

    readonly ISimpleCacheClient _cacheClient;
    readonly MomentoCacheOptions _cacheOpts;

    /// <summary>Initializes a new instance of the <see cref="MomentoCache"/> class.</summary>
    /// <param name="cacheClient">The inner cache client.</param>
    /// <param name="cacheOpts">The configuration options for the cache.</param>
    public MomentoCache(ISimpleCacheClient cacheClient, IOptionsSnapshot<MomentoCacheOptions> cacheOpts)
    {
#if NETSTANDARD2_1
        if (cacheOpts is null)
        {
            throw new ArgumentNullException(nameof(cacheOpts));
        }
#else
        ArgumentNullException.ThrowIfNull(cacheOpts);
#endif

        _cacheClient = cacheClient;
        _cacheOpts = cacheOpts.Value;
    }

    /// <inheritdoc/>
    public byte[]? Get(string key) => GetAsync(key).GetAwaiter().GetResult();

    /// <inheritdoc/>
    public Task<byte[]?> GetAsync(string key, CancellationToken token = default) => GetAndRefreshAsync(key, token);

    /// <inheritdoc/>
    public void Refresh(string key) => RefreshAsync(key).GetAwaiter().GetResult();

    /// <inheritdoc/>
    public Task RefreshAsync(string key, CancellationToken token = default) => GetAndRefreshAsync(key, token);

    /// <inheritdoc/>
    public void Remove(string key) => RemoveAsync(key).GetAwaiter().GetResult();

    /// <inheritdoc/>
    public async Task RemoveAsync(string key, CancellationToken token = default)
    {
        var result = await _cacheClient.DeleteAsync(_cacheOpts.CacheName, key).ConfigureAwait(false);
        if (result is Delete.Error { InnerException: { } ie })
        {
            throw ie;
        }
    }

    /// <inheritdoc/>
    public void Set(string key, byte[] value, DistributedCacheEntryOptions options) => SetAsync(key, value, options).GetAwaiter().GetResult();

    /// <inheritdoc/>
    public async Task SetAsync(string key, byte[] value, DistributedCacheEntryOptions options, CancellationToken token = default)
    {
#if NETSTANDARD2_1
        if (options is null)
        {
            throw new ArgumentNullException(nameof(options));
        }
#else
        ArgumentNullException.ThrowIfNull(options);
#endif

        var items = new Dictionary<string, byte[]>
        {
            [ValueKey] = value,
        };

        var now = _cacheOpts.Clock.UtcNow;
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
                items[SlidingExpirationKey] = s_utf8.GetBytes(sliding1.ToString("c", InvariantCulture));
                items[AbsoluteExpirationKey] = s_utf8.GetBytes((now + relative1).ToString("O", InvariantCulture));
                ttl = CollectionTtl.Of(sliding1);
                break;
            case ({ } relative2, _):
                /* note(cosborn)
                 * When both a relative and a sliding expiration have been provided and the
                 * relative expiration is shorter than slide, that's effectively
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
                items[SlidingExpirationKey] = s_utf8.GetBytes(sliding3.ToString("c", InvariantCulture));
                ttl = CollectionTtl.Of(sliding3);
                break;
            default:
                /* note(cosborn)
                 * When no expiration is provided, the default TTL as provided when constructing
                 * the implementation of `ISimpleCacheClient` will be used as configured by the caller.
                 * This is _not necessarily_ the value of `_cacheOpts.DefaultTtl`, since an instance
                 * of a custom implementation of `ISimpleCacheClient` could have been resolved from DI
                 * which ignores this value. (Untidy, but not impossible.) Nicely,
                 * `CollectionTtl.FromCacheTtl()` means exactly this.
                 */
                ttl = CollectionTtl.FromCacheTtl();
                break;
        }

        token.ThrowIfCancellationRequested();

        var result = await _cacheClient.DictionarySetFieldsAsync(_cacheOpts.CacheName, key, items, ttl).ConfigureAwait(false);
        if (result is SetFields.Error { Exception: { } e })
        {
            throw e;
        }

        [SuppressMessage("CSharp.Style", "IDE0046", Justification = "It would look terrible.")]
        static TimeSpan? GetRelativeExpiration(DateTimeOffset now, DistributedCacheEntryOptions options)
        {
            if (options is { AbsoluteExpiration: { } ae } && ae <= now)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(DistributedCacheEntryOptions.AbsoluteExpiration),
                    ae,
                    "The absolute expiration value must be in the future.");
            }

            return options.AbsoluteExpirationRelativeToNow ?? (options.AbsoluteExpiration - now);
        }
    }

    async Task<byte[]?> GetAndRefreshAsync(string key, CancellationToken token) => await _cacheClient
        .DictionaryGetFieldsAsync(_cacheOpts.CacheName, key, s_fields)
        .ConfigureAwait(false) switch
    {
        /* note(cosborn)
         * A `CacheDictionaryGetFieldsResponse` already has dictionaries we can use which elide misses and
         * decode values and the like, but it's awfully convienent to work with the cached value in bytes
         * (because that's the eventual return type) and the sidecar data in strings. It's more syntax, but
         * patterns make it nice.
         */
        GetFields.Hit { Responses: { } rs } => await GetAndRefreshCoreAsync(
            key,
            s_fields.Zip(rs, KeyValuePair.Create).ToImmutableDictionary(),
            token).ConfigureAwait(false),
        GetFields.Error { Exception: { } e } => throw e,
        GetFields.Miss or _ => null,
    };

    async Task<byte[]?> GetAndRefreshCoreAsync<TDictionary>(string key, TDictionary dict, CancellationToken token)
        where TDictionary : IReadOnlyDictionary<string, GetField>
    {
        if (dict.TryGetValue(SlidingExpirationKey, out var slidingResp) && slidingResp is GetField.Hit { ValueString: { } slidingString })
        {
            /* note(cosborn)
             * The presence of a sliding expiration upon set means that
             * the TTL of the value should be refreshed upon read.
             * In the absence of other information, it's refreshed to a
             * constant value which was stored next to the cached value.
             */
            var ttl = TimeSpan.ParseExact(slidingString, "c", InvariantCulture);
            if (dict.TryGetValue(AbsoluteExpirationKey, out var absoluteResp) && absoluteResp is GetField.Hit { ValueString: { } absoluteString })
            {
                /* note(cosborn)
                 * The presence of both a sliding and an absolute expiration upon set means that
                 * the absolute expiration acts as an upper limit on sliding. For refresh, the
                 * TTL should be updated to the _earlier_ of:
                 * - the next slide
                 * - the absolute expiration
                 */
                var absolute = DateTimeOffset.ParseExact(absoluteString, "O", InvariantCulture);
                ttl = Min(ttl, absolute - _cacheOpts.Clock.UtcNow);
            }

            token.ThrowIfCancellationRequested();

            // note(cosborn) The refresh TTL field is used solely to refresh the TTL. It is otherwise not read.
            var result = await _cacheClient
                .DictionaryIncrementAsync(_cacheOpts.CacheName, key, RefreshTtlKey, ttl: CollectionTtl.Of(ttl))
                .ConfigureAwait(false);
            if (result is Increment.Error { Exception: { } e })
            {
                throw e;
            }
        }

        /* note(cosborn)
         * Lacking or missing a value field would be an indicator of a bug. Somehow.
         * I'm not sure I want to write something assertion-y (like a cast to `Hit`) in here, though.
         */
        return dict[ValueKey] is GetField.Hit { ValueByteArray: { } value } ? value : null;

        static TimeSpan Min(TimeSpan a, TimeSpan b) => a > b ? b : a;
    }
}
