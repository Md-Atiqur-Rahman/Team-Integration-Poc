using MongoDB.Driver;
using TeamsIntegration.Api.Models;
using TeamsIntegration.Api.Repositories;

namespace TeamsIntegration.Api.Tests;

[Collection(nameof(MongoTestCollection))]
public sealed class TeamsConfigurationRepositoryTests(MongoFixture mongoFixture)
{
    [Fact]
    public async Task UpsertAsync_saves_and_GetActiveAsync_returns_it()
    {
        var collection = mongoFixture.CreateEmptyCollection();
        var repository = new MongoTeamsConfigurationRepository(collection);

        var saved = await repository.UpsertAsync(
            NewConfiguration("org-1", "proj-1", "app-1"),
            CancellationToken.None);

        Assert.NotEqual(default, saved.CreatedAtUtc);
        Assert.Equal(saved.CreatedAtUtc, saved.UpdatedAtUtc);

        var fetched = await repository.GetActiveAsync("org-1", "proj-1", "app-1", CancellationToken.None);

        Assert.NotNull(fetched);
        Assert.Equal("team-1", fetched!.TeamId);
        Assert.Equal("channel-1", fetched.ChannelId);
    }

    [Fact]
    public async Task GetActiveAsync_returns_null_when_no_configuration_exists()
    {
        var collection = mongoFixture.CreateEmptyCollection();
        var repository = new MongoTeamsConfigurationRepository(collection);

        var result = await repository.GetActiveAsync(
            "missing-org",
            "missing-proj",
            "missing-app",
            CancellationToken.None);

        Assert.Null(result);
    }

    [Fact]
    public async Task UniqueIndex_rejects_a_second_document_with_the_same_composite_key()
    {
        var collection = mongoFixture.CreateEmptyCollection();
        await MongoIndexInitializer.EnsureIndexesAsync(collection, CancellationToken.None);

        await collection.InsertOneAsync(
            NewConfiguration("org-dup", "proj-dup", "app-dup"),
            cancellationToken: CancellationToken.None);

        var duplicate = NewConfiguration("org-dup", "proj-dup", "app-dup");
        duplicate.TeamId = "different-team";

        var exception = await Assert.ThrowsAsync<MongoWriteException>(
            () => collection.InsertOneAsync(duplicate, cancellationToken: CancellationToken.None));

        Assert.Equal(ServerErrorCategory.DuplicateKey, exception.WriteError.Category);
    }

    private static TeamsConfiguration NewConfiguration(string organizationId, string projectId, string applicationId) =>
        new()
        {
            OrganizationId = organizationId,
            ProjectId = projectId,
            ApplicationId = applicationId,
            TenantId = "tenant-1",
            UserObjectId = "user-1",
            TeamId = "team-1",
            TeamName = "Team One",
            ChannelId = "channel-1",
            ChannelName = "General",
            ConnectionStatus = TeamsConfigurationStatus.Active
        };
}
