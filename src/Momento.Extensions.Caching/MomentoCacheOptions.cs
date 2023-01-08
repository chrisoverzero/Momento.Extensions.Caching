// <copyright file="MomentoCacheOptions.cs" company="Cimpress, Inc.">
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

/// <summary>Configuration options for <see cref="MomentoCache"/>.</summary>
public sealed class MomentoCacheOptions
    : IOptionsSnapshot<MomentoCacheOptions>
{
    /// <summary>Gets or sets the name of the cache from which to retrieve items.</summary>
    [Required]
    public string CacheName { get; set; } = null!;

    /// <summary>Gets or sets a source of system time.</summary>
    [Required]
    public ISystemClock Clock { get; set; } = new SystemClock();

    /// <summary>Gets or sets the default Time-to-Live value for items in the cache.</summary>
    public TimeSpan DefaultTtl { get; set; }

    /// <inheritdoc/>
    MomentoCacheOptions IOptions<MomentoCacheOptions>.Value => this;

    /// <inheritdoc/>
    MomentoCacheOptions IOptionsSnapshot<MomentoCacheOptions>.Get(string? name) => this;
}
