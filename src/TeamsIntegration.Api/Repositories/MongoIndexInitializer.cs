using MongoDB.Driver;
using TeamsIntegration.Api.Models;

namespace TeamsIntegration.Api.Repositories;

public static class MongoIndexInitializer
{
    public static async Task EnsureIndexesAsync(
        IMongoCollection<TeamsConfiguration> collection,
        CancellationToken cancellationToken = default)
    {
        var keys = Builders<TeamsConfiguration>.IndexKeys
            .Ascending(c => c.OrganizationId)
            .Ascending(c => c.ProjectId)
            .Ascending(c => c.ApplicationId);

        var model = new CreateIndexModel<TeamsConfiguration>(
            keys,
            new CreateIndexOptions { Unique = true, Name = "organizationId_projectId_applicationId_unique" });

        await collection.Indexes.CreateOneAsync(model, cancellationToken: cancellationToken);
    }

    public static async Task EnsureIndexesAsync(
        IMongoCollection<OrganizationTeamsConnection> collection,
        CancellationToken cancellationToken = default)
    {
        var keys = Builders<OrganizationTeamsConnection>.IndexKeys.Ascending(c => c.OrganizationId);

        var model = new CreateIndexModel<OrganizationTeamsConnection>(
            keys,
            new CreateIndexOptions { Unique = true, Name = "organizationId_unique" });

        await collection.Indexes.CreateOneAsync(model, cancellationToken: cancellationToken);
    }
}
