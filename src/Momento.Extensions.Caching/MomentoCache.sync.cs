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
partial class MomentoCache
{
    /// <inheritdoc/>
    byte[]? IDistributedCache.Get(string key) => GetAsync(key).GetAwaiter().GetResult();

    /// <inheritdoc/>
    void IDistributedCache.Refresh(string key) => RefreshAsync(key).GetAwaiter().GetResult();

    /// <inheritdoc/>
    void IDistributedCache.Remove(string key) => RemoveAsync(key).GetAwaiter().GetResult();

    /// <inheritdoc/>
    void IDistributedCache.Set(string key, byte[] value, DistributedCacheEntryOptions options) =>
        SetAsync(key, value, options).GetAwaiter().GetResult();
}
