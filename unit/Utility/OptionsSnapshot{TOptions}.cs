// <copyright file="OptionsSnapshot{TOptions}.cs" company="Cimpress, Inc.">
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
/// <typeparam name="TOptions">The type of the options values.</typeparam>
/// <param name="value">The default configuration options value, named by <see cref="Options.DefaultName"/>.</param>
/// <param name="namedValues">Other configuration options values which can be retrieved by name.</param>
sealed class OptionsSnapshot<TOptions>(TOptions value, IReadOnlyDictionary<string, TOptions> namedValues)
    : IOptionsSnapshot<TOptions>
    where TOptions : class
{
    /// <inheritdoc/>
    public TOptions Value { get; } = value;

    /// <inheritdoc/>
    public TOptions Get(string? name) => name is not { } n || n == Options.DefaultName ? Value : namedValues[n];
}
