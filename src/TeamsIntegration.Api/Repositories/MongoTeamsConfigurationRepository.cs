using MongoDB.Driver;
using TeamsIntegration.Api.Models;

namespace TeamsIntegration.Api.Repositories;

public sealed class MongoTeamsConfigurationRepository(IMongoCollection<TeamsConfiguration> collection)
    : ITeamsConfigurationRepository
{
    public async Task<TeamsConfiguration?> GetActiveAsync(
        string organizationId,
        string projectId,
        string applicationId,
        CancellationToken cancellationToken)
    {
        var filter = BuildKeyFilter(organizationId, projectId, applicationId);
        return await collection.Find(filter).FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<TeamsConfiguration> UpsertAsync(
        TeamsConfiguration configuration,
        CancellationToken cancellationToken)
    {
        // Index creation is idempotent (a no-op when the index already exists), so ensuring it here
        // keeps the unique constraint in force before any write without requiring a Mongo connection
        // at application startup (which would break hosts that never touch this repository).
        await MongoIndexInitializer.EnsureIndexesAsync(collection, cancellationToken);

        var filter = BuildKeyFilter(
            configuration.OrganizationId,
            configuration.ProjectId,
            configuration.ApplicationId);

        var now = DateTime.UtcNow;
        var update = Builders<TeamsConfiguration>.Update
            .SetOnInsert(c => c.CreatedAtUtc, now)
            .Set(c => c.OrganizationId, configuration.OrganizationId)
            .Set(c => c.ProjectId, configuration.ProjectId)
            .Set(c => c.ApplicationId, configuration.ApplicationId)
            .Set(c => c.TenantId, configuration.TenantId)
            .Set(c => c.UserObjectId, configuration.UserObjectId)
            .Set(c => c.TeamId, configuration.TeamId)
            .Set(c => c.TeamName, configuration.TeamName)
            .Set(c => c.ChannelId, configuration.ChannelId)
            .Set(c => c.ChannelName, configuration.ChannelName)
            .Set(c => c.ConnectionStatus, configuration.ConnectionStatus)
            .Set(c => c.ConnectionFailureCode, configuration.ConnectionFailureCode)
            .Set(c => c.ConnectionFailureDetectedAtUtc, configuration.ConnectionFailureDetectedAtUtc)
            .Set(c => c.ConnectionAlertedAtUtc, configuration.ConnectionAlertedAtUtc)
            .Set(c => c.UpdatedAtUtc, now);

        var options = new FindOneAndUpdateOptions<TeamsConfiguration>
        {
            IsUpsert = true,
            ReturnDocument = ReturnDocument.After
        };

        return await collection.FindOneAndUpdateAsync(filter, update, options, cancellationToken);
    }

    private static FilterDefinition<TeamsConfiguration> BuildKeyFilter(
        string organizationId,
        string projectId,
        string applicationId) =>
        Builders<TeamsConfiguration>.Filter.Where(c =>
            c.OrganizationId == organizationId
            && c.ProjectId == projectId
            && c.ApplicationId == applicationId);
}
