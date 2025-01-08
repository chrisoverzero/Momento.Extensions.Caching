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

/// <summary>Extensions to the functionality of the <see cref="IServiceCollection"/> interface.</summary>
public static class ServiceCollectionExtensions
{
    /// <summary>Adds Momento distributed caching services to the specified <see cref="IServiceCollection" />.</summary>
    /// <param name="services">The <see cref="IServiceCollection" /> to add services to.</param>
    /// <param name="setupAction">An optional <see cref="Action{MomentoCacheOptions}"/> to configure <see cref="MomentoCacheOptions"/>.</param>
    /// <returns>The <see cref="IServiceCollection"/> so that additional calls can be chained.</returns>
    public static IServiceCollection AddMomentoDistributedCache(this IServiceCollection services, Action<MomentoCacheOptions>? setupAction = null)
    {
        ArgumentNullException.ThrowIfNull(services);

        var optionsBuilder = services
            .AddSingleton<IValidateOptions<MomentoCacheOptions>, MomentoCacheOptions.Validator>()
            .AddOptions<MomentoCacheOptions>();
        if (setupAction is { } sa)
        {
            _ = optionsBuilder.Configure(sa);
        }

        services.TryAddSingleton(TimeProvider.System);
        services.TryAddScoped<ICacheClient>(static p =>
        {
            var config = p.GetRequiredService<IConfiguration>();
            var authProvider = p.GetRequiredService<ICredentialProvider>();
            var cacheOpts = p.GetRequiredService<IOptionsMonitor<MomentoCacheOptions>>();

            return new CacheClient(config, authProvider, cacheOpts.CurrentValue.DefaultTtl);
        });
        return services.AddScoped<IDistributedCache, MomentoCache>();
    }
}
