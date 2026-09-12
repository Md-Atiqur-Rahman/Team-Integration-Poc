using Microsoft.AspNetCore.Http;
using MongoDB.Driver;
using TeamsIntegration.Api.Models;
using TeamsIntegration.Api.Repositories;
using TeamsIntegration.Api.Services;

namespace TeamsIntegration.Api.Tests;

[Collection(nameof(MongoTestCollection))]
public sealed class TeamsConfigurationServiceTests(MongoFixture mongoFixture)
{
    [Fact]
    public async Task SaveAsync_persists_when_team_and_channel_are_accessible()
    {
        var collection = mongoFixture.CreateEmptyCollection();
        var repository = new MongoTeamsConfigurationRepository(collection);
        var graphService = new FakeTeamsGraphService
        {
            Teams = [new TeamItem("team-1", "Team One", null, false)],
            Channels = [new ChannelItem("channel-1", "General", null, "standard", false)]
        };
        var service = new TeamsConfigurationService(graphService, repository);

        var dto = await service.SaveAsync(
            "org-1", "proj-1", "app-1", "team-1", "channel-1", "tenant-1", "user-1", CancellationToken.None);

        Assert.Equal("Team One", dto.TeamName);
        Assert.Equal("General", dto.ChannelName);
        Assert.Equal(TeamsConfigurationStatus.Active, dto.ConnectionStatus);

        var persistedCount = await collection.CountDocumentsAsync(c => c.OrganizationId == "org-1");
        Assert.Equal(1, persistedCount);
    }

    [Fact]
    public async Task SaveAsync_throws_and_saves_nothing_when_team_is_not_accessible()
    {
        var collection = mongoFixture.CreateEmptyCollection();
        var repository = new MongoTeamsConfigurationRepository(collection);
        var graphService = new FakeTeamsGraphService { Teams = [], Channels = [] };
        var service = new TeamsConfigurationService(graphService, repository);

        var exception = await Assert.ThrowsAsync<TeamsGraphException>(() => service.SaveAsync(
            "org-2", "proj-2", "app-2", "missing-team", "missing-channel", "tenant-1", "user-1", CancellationToken.None));

        Assert.Equal(StatusCodes.Status404NotFound, exception.StatusCode);
        Assert.Equal(0, await collection.CountDocumentsAsync(FilterDefinition<TeamsConfiguration>.Empty));
    }

    [Fact]
    public async Task SaveAsync_throws_and_saves_nothing_when_channel_is_not_accessible()
    {
        var collection = mongoFixture.CreateEmptyCollection();
        var repository = new MongoTeamsConfigurationRepository(collection);
        var graphService = new FakeTeamsGraphService
        {
            Teams = [new TeamItem("team-1", "Team One", null, false)],
            Channels = []
        };
        var service = new TeamsConfigurationService(graphService, repository);

        var exception = await Assert.ThrowsAsync<TeamsGraphException>(() => service.SaveAsync(
            "org-3", "proj-3", "app-3", "team-1", "missing-channel", "tenant-1", "user-1", CancellationToken.None));

        Assert.Equal(StatusCodes.Status404NotFound, exception.StatusCode);
        Assert.Equal(0, await collection.CountDocumentsAsync(FilterDefinition<TeamsConfiguration>.Empty));
    }
}
