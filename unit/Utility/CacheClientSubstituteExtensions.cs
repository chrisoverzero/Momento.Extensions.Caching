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

static class CacheClientSubstituteExtensions
{
    public static void SetUpScenario(this ICacheClient cacheClient, GetFields getFieldsResponse) => cacheClient
        .DictionaryGetFieldsAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<IEnumerable<string>>())
        .Returns(getFieldsResponse);

    public static void SetUpScenario(this ICacheClient cacheClient, SetFields response) => cacheClient
        .DictionarySetFieldsAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<Items>(), Arg.Any<CollectionTtl>())
        .Returns(response);

    public static void SetUpScenario(this ICacheClient cacheClient, GetFields getFieldsResponse, UpdateTtl updateTtlResponse)
    {
        _ = cacheClient
            .DictionaryGetFieldsAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<IEnumerable<string>>())
            .Returns(getFieldsResponse);
        _ = cacheClient
            .UpdateTtlAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<TimeSpan>())
            .Returns(updateTtlResponse);
    }

    public static void SetUpScenario(this ICacheClient cacheClient, Delete response) =>
        cacheClient.DeleteAsync(Arg.Any<string>(), Arg.Any<string>()).Returns(response);
}
