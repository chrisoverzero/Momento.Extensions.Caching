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

/// <summary>
/// A test wrapper for options values which implements <see cref="IOptionsMonitor{TOptions}"/>.
/// It doesn't actually monitor anything.
/// </summary>
/// <typeparam name="TOptions">The type of the options values.</typeparam>
/// <param name="currentValue">The default configuration options value, named by <see cref="Options.DefaultName"/>.</param>
sealed class KoalaOptionsMonitor<TOptions>(TOptions currentValue)
    : IOptionsMonitor<TOptions>
    where TOptions : class
{
    /// <inheritdoc/>
    public TOptions CurrentValue => currentValue;

    /// <inheritdoc/>
    public TOptions Get(string? name) => CurrentValue;

    public IDisposable? OnChange(Action<TOptions, string?> listener) => null;
}
