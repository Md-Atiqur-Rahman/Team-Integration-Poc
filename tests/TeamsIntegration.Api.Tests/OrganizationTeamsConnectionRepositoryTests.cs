using MongoDB.Driver;
using TeamsIntegration.Api.Models;
using TeamsIntegration.Api.Repositories;

namespace TeamsIntegration.Api.Tests;

[Collection(nameof(MongoTestCollection))]
public sealed class OrganizationTeamsConnectionRepositoryTests(MongoFixture mongoFixture)
{
    [Fact]
    public async Task UpsertAsync_saves_and_GetActiveAsync_returns_it()
    {
        var collection = mongoFixture.CreateEmptyOrganizationConnectionsCollection();
        var repository = new MongoOrganizationTeamsConnectionRepository(collection);

        var saved = await repository.UpsertAsync(NewConnection("org-1"), CancellationToken.None);

        Assert.NotEqual(default, saved.CreatedAtUtc);
        Assert.Equal(saved.CreatedAtUtc, saved.UpdatedAtUtc);

        var fetched = await repository.GetActiveAsync("org-1", CancellationToken.None);

        Assert.NotNull(fetched);
        Assert.Equal("tenant-1", fetched!.TenantId);
        Assert.Equal("user-1", fetched.UserObjectId);
        Assert.Equal("connector@example.com", fetched.ConnectedAsEmail);
    }

    [Fact]
    public async Task UpsertAsync_replaces_the_existing_connection_for_the_same_organization()
    {
        var collection = mongoFixture.CreateEmptyOrganizationConnectionsCollection();
        var repository = new MongoOrganizationTeamsConnectionRepository(collection);

        await repository.UpsertAsync(NewConnection("org-replace"), CancellationToken.None);

        var replacement = NewConnection("org-replace");
        replacement.UserObjectId = "user-2";
        await repository.UpsertAsync(replacement, CancellationToken.None);

        var fetched = await repository.GetActiveAsync("org-replace", CancellationToken.None);
        Assert.NotNull(fetched);
        Assert.Equal("user-2", fetched!.UserObjectId);
        Assert.Equal(1, await collection.CountDocumentsAsync(FilterDefinition<OrganizationTeamsConnection>.Empty));
    }

    [Fact]
    public async Task GetActiveAsync_returns_null_when_no_connection_exists()
    {
        var collection = mongoFixture.CreateEmptyOrganizationConnectionsCollection();
        var repository = new MongoOrganizationTeamsConnectionRepository(collection);

        var result = await repository.GetActiveAsync("missing-org", CancellationToken.None);

        Assert.Null(result);
    }

    [Fact]
    public async Task UniqueIndex_rejects_a_second_document_with_the_same_organizationId()
    {
        var collection = mongoFixture.CreateEmptyOrganizationConnectionsCollection();
        await MongoIndexInitializer.EnsureIndexesAsync(collection, CancellationToken.None);

        await collection.InsertOneAsync(NewConnection("org-dup"), cancellationToken: CancellationToken.None);

        var duplicate = NewConnection("org-dup");
        duplicate.UserObjectId = "different-user";

        var exception = await Assert.ThrowsAsync<MongoWriteException>(
            () => collection.InsertOneAsync(duplicate, cancellationToken: CancellationToken.None));

        Assert.Equal(ServerErrorCategory.DuplicateKey, exception.WriteError.Category);
    }

    [Fact]
    public async Task MarkNeedsReconnectAsync_is_a_safe_no_op_when_no_connection_exists()
    {
        var collection = mongoFixture.CreateEmptyOrganizationConnectionsCollection();
        var repository = new MongoOrganizationTeamsConnectionRepository(collection);

        await repository.MarkNeedsReconnectAsync(
            "missing-org", "reauthentication_required", CancellationToken.None);

        Assert.Equal(0, await collection.CountDocumentsAsync(FilterDefinition<OrganizationTeamsConnection>.Empty));
    }

    [Fact]
    public async Task MarkNeedsReconnectAsync_updates_status_and_failure_fields()
    {
        var collection = mongoFixture.CreateEmptyOrganizationConnectionsCollection();
        var repository = new MongoOrganizationTeamsConnectionRepository(collection);
        await repository.UpsertAsync(NewConnection("org-fail"), CancellationToken.None);

        await repository.MarkNeedsReconnectAsync("org-fail", "reauthentication_required", CancellationToken.None);

        var fetched = await repository.GetActiveAsync("org-fail", CancellationToken.None);
        Assert.NotNull(fetched);
        Assert.Equal(TeamsConfigurationStatus.NeedsReconnect, fetched!.ConnectionStatus);
        Assert.Equal("reauthentication_required", fetched.ConnectionFailureCode);
        Assert.NotNull(fetched.ConnectionFailureDetectedAtUtc);
    }

    private static OrganizationTeamsConnection NewConnection(string organizationId) =>
        new()
        {
            OrganizationId = organizationId,
            TenantId = "tenant-1",
            UserObjectId = "user-1",
            ConnectedAsEmail = "connector@example.com",
            ConnectionStatus = TeamsConfigurationStatus.Active
        };
}
