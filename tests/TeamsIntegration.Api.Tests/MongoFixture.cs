using Mongo2Go;
using MongoDB.Driver;
using TeamsIntegration.Api.Models;

namespace TeamsIntegration.Api.Tests;

public sealed class MongoFixture : IDisposable
{
    private readonly MongoDbRunner _runner;

    public MongoFixture()
    {
        _runner = MongoDbRunner.Start(singleNodeReplSet: false);
        Client = new MongoClient(_runner.ConnectionString);
    }

    public MongoClient Client { get; }

    public IMongoCollection<TeamsConfiguration> CreateEmptyCollection()
    {
        var database = Client.GetDatabase("teams_integration_tests");
        return database.GetCollection<TeamsConfiguration>(Guid.NewGuid().ToString("N"));
    }

    public void Dispose() => _runner.Dispose();
}

[CollectionDefinition(nameof(MongoTestCollection))]
public sealed class MongoTestCollection : ICollectionFixture<MongoFixture>;
