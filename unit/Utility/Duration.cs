// <copyright file="Duration.cs" company="Cimpress, Inc.">
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

/// <summary>An explicitly positive span of time.</summary>
/// <param name="Value">The wrapped value.</param>
public readonly record struct Duration(TimeSpan Value)
{
    public static implicit operator TimeSpan(Duration duration) => duration.ToTimeSpan();

    public TimeSpan ToTimeSpan() => Value;
}
