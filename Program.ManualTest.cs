using Momento.Extensions.Caching;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Internal;
using Microsoft.Extensions.Logging;
using Momento.Sdk.Auth;
using Momento.Sdk.Config;
using Momento.Sdk.Incubating;

var cacheOpts = new MomentoCacheOptions
{
    CacheName = "incubating-preview",
    DefaultTtl = TimeSpan.FromSeconds(60),
};
IConfiguration configuration = Configurations.Laptop.Latest();
ICredentialProvider credentialProvider = new EnvMomentoTokenProvider("MOMENTO_TOKEN");

var momentoClient = SimpleCacheClientFactory.CreateClient(configuration, credentialProvider, cacheOpts.DefaultTtl);
IDistributedCache distributedClient = new MomentoDistributedCache(momentoClient, new SystemClock(), cacheOpts);

await distributedClient.SetStringAsync("test", "value").ConfigureAwait(false);
var value = await distributedClient.GetStringAsync("test").ConfigureAwait(false);
Console.WriteLine(value);
