using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.DependencyInjection;

namespace TeamsIntegration.Api.Tests;

[Collection(nameof(RedisTestCollection))]
public sealed class RedisDistributedCacheTests(RedisFixture redisFixture)
{
    [Fact]
    public async Task AddStackExchangeRedisCache_round_trips_a_value_through_a_real_redis()
    {
        var services = new ServiceCollection();
        services.AddStackExchangeRedisCache(options =>
        {
            options.Configuration = redisFixture.ConnectionString;
            options.InstanceName = "TeamsIntegrationPoc.Tests:";
        });

        await using var provider = services.BuildServiceProvider();
        var cache = provider.GetRequiredService<IDistributedCache>();

        var key = $"test-key-{Guid.NewGuid():N}";
        await cache.SetStringAsync(key, "cached-value", CancellationToken.None);

        var result = await cache.GetStringAsync(key, CancellationToken.None);

        Assert.Equal("cached-value", result);
    }

    [Fact]
    public async Task AddStackExchangeRedisCache_returns_null_for_a_missing_key()
    {
        var services = new ServiceCollection();
        services.AddStackExchangeRedisCache(options =>
        {
            options.Configuration = redisFixture.ConnectionString;
            options.InstanceName = "TeamsIntegrationPoc.Tests:";
        });

        await using var provider = services.BuildServiceProvider();
        var cache = provider.GetRequiredService<IDistributedCache>();

        var result = await cache.GetStringAsync($"missing-key-{Guid.NewGuid():N}", CancellationToken.None);

        Assert.Null(result);
    }
}
