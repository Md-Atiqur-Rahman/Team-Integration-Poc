using MongoDB.Driver;
using TeamsIntegration.Api.Models;

namespace TeamsIntegration.Api.Repositories;

public sealed class MongoOrganizationTeamsConnectionRepository(IMongoCollection<OrganizationTeamsConnection> collection)
    : IOrganizationTeamsConnectionRepository
{
    public async Task<OrganizationTeamsConnection?> GetActiveAsync(
        string organizationId,
        CancellationToken cancellationToken)
    {
        var filter = BuildKeyFilter(organizationId);
        return await collection.Find(filter).FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<OrganizationTeamsConnection> UpsertAsync(
        OrganizationTeamsConnection connection,
        CancellationToken cancellationToken)
    {
        // Index creation is idempotent, so ensuring it here (like MongoTeamsConfigurationRepository)
        // keeps the unique constraint in force before any write without requiring a Mongo connection
        // at application startup.
        await MongoIndexInitializer.EnsureIndexesAsync(collection, cancellationToken);

        var filter = BuildKeyFilter(connection.OrganizationId);
        var now = DateTime.UtcNow;
        var update = Builders<OrganizationTeamsConnection>.Update
            .SetOnInsert(c => c.CreatedAtUtc, now)
            .Set(c => c.OrganizationId, connection.OrganizationId)
            .Set(c => c.TenantId, connection.TenantId)
            .Set(c => c.UserObjectId, connection.UserObjectId)
            .Set(c => c.ConnectedAsEmail, connection.ConnectedAsEmail)
            .Set(c => c.ConnectionStatus, connection.ConnectionStatus)
            .Set(c => c.ConnectionFailureCode, connection.ConnectionFailureCode)
            .Set(c => c.ConnectionFailureDetectedAtUtc, connection.ConnectionFailureDetectedAtUtc)
            .Set(c => c.ConnectionAlertedAtUtc, connection.ConnectionAlertedAtUtc)
            .Set(c => c.UpdatedAtUtc, now);

        var options = new FindOneAndUpdateOptions<OrganizationTeamsConnection>
        {
            IsUpsert = true,
            ReturnDocument = ReturnDocument.After
        };

        return await collection.FindOneAndUpdateAsync(filter, update, options, cancellationToken);
    }

    public async Task MarkNeedsReconnectAsync(
        string organizationId,
        string failureCode,
        CancellationToken cancellationToken)
    {
        var filter = BuildKeyFilter(organizationId);
        var now = DateTime.UtcNow;
        var update = Builders<OrganizationTeamsConnection>.Update
            .Set(c => c.ConnectionStatus, TeamsConfigurationStatus.NeedsReconnect)
            .Set(c => c.ConnectionFailureCode, failureCode)
            .Set(c => c.ConnectionFailureDetectedAtUtc, now)
            .Set(c => c.UpdatedAtUtc, now);

        await collection.UpdateOneAsync(filter, update, cancellationToken: cancellationToken);
    }

    private static FilterDefinition<OrganizationTeamsConnection> BuildKeyFilter(string organizationId) =>
        Builders<OrganizationTeamsConnection>.Filter.Where(c => c.OrganizationId == organizationId);
}
