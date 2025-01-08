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

/// <summary>A test wrapper for options values which implements <see cref="IOptionsMonitor{TOptions}"/>.</summary>
static class OptionsMonitor
{
    /// <summary>Creates an options monitor from the provided default and named options values.</summary>
    /// <typeparam name="TOptions">The type of the configuration options values.</typeparam>
    /// <param name="value">The default configuration options value, named by <see cref="Options.DefaultName"/>.</param>
    /// <returns>A snapshot of the provided configuration options.</returns>
    public static IOptionsMonitor<TOptions> Create<TOptions>(TOptions value)
        where TOptions : class => new KoalaOptionsMonitor<TOptions>(value);
}
