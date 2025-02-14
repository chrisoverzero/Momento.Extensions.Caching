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

namespace Momento.Caching.Extensions;

/// <summary>Constants for the DynamoDB caching extension.</summary>
static class Constants
{
    /// <summary>The name of the attribute in which the cached value is stored.</summary>
    public const string ValueKey = "v";

    /// <summary>The name of the attribute in which the sliding expiration value is stored.</summary>
    public const string SlidingExpirationKey = "s";

    /// <summary>The name of the attribute in which the absolute expiration value is stored.</summary>
    public const string AbsoluteExpirationKey = "a";
}
