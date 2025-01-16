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

/// <summary>Provides methods for marshalling data to and from Momento.</summary>
static class Marshal
{
    /// <summary>Converts a <see cref="DateTimeOffset"/> to a Momento timestamp.</summary>
    /// <param name="absolute">The absolute time to convert.</param>
    /// <returns>The Momento timestamp.</returns>
    public static byte[] To(DateTimeOffset absolute) => Timestamp.FromDateTimeOffset(absolute).ToByteArray();

    /// <summary>Converts a <see cref="TimeSpan"/> to a Momento duration.</summary>
    /// <param name="sliding">The sliding expiration to convert.</param>
    /// <returns>The Momento duration.</returns>
    public static byte[] To(TimeSpan sliding) => Duration.FromTimeSpan(sliding).ToByteArray();

    /// <summary>Converts a Momento timestamp to a <see cref="DateTimeOffset"/>.</summary>
    /// <param name="absolute">The Momento timestamp to convert.</param>
    /// <returns>The <see cref="DateTimeOffset"/>.</returns>
    public static DateTimeOffset ToAbsolute(ReadOnlySpan<byte> absolute) => Timestamp.Parser.ParseFrom(absolute).ToDateTimeOffset();

    /// <summary>Converts a Momento duration to a <see cref="TimeSpan"/>.</summary>
    /// <param name="sliding">The Momento duration to convert.</param>
    /// <returns>The <see cref="TimeSpan"/>.</returns>
    public static TimeSpan ToSliding(ReadOnlySpan<byte> sliding) => Duration.Parser.ParseFrom(sliding).ToTimeSpan();
}
