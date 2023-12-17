// <copyright file="OptionsSnapshot.cs" company="Cimpress, Inc.">
// Copyright 2023 Cimpress, Inc.
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
// </copyright>

namespace Momento.Extensions.Caching.Unit;

/// <summary>A test wrapper for options values which implements <see cref="IOptionsSnapshot{TOptions}"/>.</summary>
static class OptionsSnapshot
{
    /// <summary>Creates an options snapshot from the provided default and named options values.</summary>
    /// <typeparam name="TOptions">The type of the configuration options values.</typeparam>
    /// <param name="value">The default configuration options value, named by <see cref="Options.DefaultName"/>.</param>
    /// <returns>A snapshot of the provided configuration options.</returns>
    public static IOptionsSnapshot<TOptions> Create<TOptions>(TOptions value)
        where TOptions : class => Create(value, ImmutableDictionary<string, TOptions>.Empty);

    /// <summary>Creates an options snapshot from the provided default and named options values.</summary>
    /// <typeparam name="TOptions">The type of the configuration options values.</typeparam>
    /// <param name="value">The default configuration options value, named by <see cref="Options.DefaultName"/>.</param>
    /// <param name="namedValues">Other configuration options values which can be retrieved by name.</param>
    /// <returns>A snapshot of the provided configuration options.</returns>
    public static IOptionsSnapshot<TOptions> Create<TOptions>(TOptions value, IReadOnlyDictionary<string, TOptions>? namedValues = null)
        where TOptions : class => new OptionsSnapshot<TOptions>(value, namedValues ?? ImmutableDictionary<string, TOptions>.Empty);
}
