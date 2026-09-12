using Testcontainers.Redis;

namespace TeamsIntegration.Api.Tests;

public sealed class RedisFixture : IAsyncLifetime
{
    private readonly RedisContainer _container = new RedisBuilder("redis:7.4").Build();

    public string ConnectionString => _container.GetConnectionString();

    public Task InitializeAsync() => _container.StartAsync();

    public Task DisposeAsync() => _container.DisposeAsync().AsTask();
}

[CollectionDefinition(nameof(RedisTestCollection))]
public sealed class RedisTestCollection : ICollectionFixture<RedisFixture>;
