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
        var connectionRepository = NewConnectedRepository("org-1");
        var graphService = new FakeTeamsGraphService
        {
            Teams = [new TeamItem("team-1", "Team One", null, false)],
            Channels = [new ChannelItem("channel-1", "General", null, "standard", false)]
        };
        var service = new TeamsConfigurationService(graphService, repository, connectionRepository);

        var dto = await service.SaveAsync(
            "org-1", "proj-1", "app-1", "team-1", "channel-1", CancellationToken.None);

        Assert.Equal("Team One", dto.TeamName);
        Assert.Equal("General", dto.ChannelName);
        Assert.Equal(TeamsConfigurationStatus.Active, dto.ConnectionStatus);

        // Saving is anonymous — the persisted tenant/user identity comes from the org's stored
        // connection (seeded as tenant-1/user-1 in NewConnectedRepository), never from a caller
        // identity, since SaveAsync no longer takes one.
        Assert.Equal("tenant-1", dto.TenantId);
        Assert.Equal("user-1", dto.UserObjectId);

        var persistedCount = await collection.CountDocumentsAsync(c => c.OrganizationId == "org-1");
        Assert.Equal(1, persistedCount);
    }

    [Fact]
    public async Task SaveAsync_throws_when_the_organization_has_no_teams_connection()
    {
        var collection = mongoFixture.CreateEmptyCollection();
        var repository = new MongoTeamsConfigurationRepository(collection);
        var connectionRepository = new MongoOrganizationTeamsConnectionRepository(
            mongoFixture.CreateEmptyOrganizationConnectionsCollection());
        var graphService = new FakeTeamsGraphService();
        var service = new TeamsConfigurationService(graphService, repository, connectionRepository);

        var exception = await Assert.ThrowsAsync<TeamsGraphException>(() => service.SaveAsync(
            "org-unconnected", "proj-1", "app-1", "team-1", "channel-1", CancellationToken.None));

        Assert.Equal(StatusCodes.Status404NotFound, exception.StatusCode);
        Assert.Equal(0, graphService.GetTeamsCallCount);
    }

    [Fact]
    public async Task SaveAsync_throws_and_saves_nothing_when_team_is_not_accessible()
    {
        var collection = mongoFixture.CreateEmptyCollection();
        var repository = new MongoTeamsConfigurationRepository(collection);
        var connectionRepository = NewConnectedRepository("org-2");
        var graphService = new FakeTeamsGraphService { Teams = [], Channels = [] };
        var service = new TeamsConfigurationService(graphService, repository, connectionRepository);

        var exception = await Assert.ThrowsAsync<TeamsGraphException>(() => service.SaveAsync(
            "org-2", "proj-2", "app-2", "missing-team", "missing-channel", CancellationToken.None));

        Assert.Equal(StatusCodes.Status404NotFound, exception.StatusCode);
        Assert.Equal(0, await collection.CountDocumentsAsync(FilterDefinition<TeamsConfiguration>.Empty));
    }

    [Fact]
    public async Task SaveAsync_throws_and_saves_nothing_when_channel_is_not_accessible()
    {
        var collection = mongoFixture.CreateEmptyCollection();
        var repository = new MongoTeamsConfigurationRepository(collection);
        var connectionRepository = NewConnectedRepository("org-3");
        var graphService = new FakeTeamsGraphService
        {
            Teams = [new TeamItem("team-1", "Team One", null, false)],
            Channels = []
        };
        var service = new TeamsConfigurationService(graphService, repository, connectionRepository);

        var exception = await Assert.ThrowsAsync<TeamsGraphException>(() => service.SaveAsync(
            "org-3", "proj-3", "app-3", "team-1", "missing-channel", CancellationToken.None));

        Assert.Equal(StatusCodes.Status404NotFound, exception.StatusCode);
        Assert.Equal(0, await collection.CountDocumentsAsync(FilterDefinition<TeamsConfiguration>.Empty));
    }

    [Fact]
    public async Task SaveAsync_marks_the_organization_connection_needsReconnect_when_graph_requires_reauthentication()
    {
        var collection = mongoFixture.CreateEmptyCollection();
        var repository = new MongoTeamsConfigurationRepository(collection);
        var connectionRepository = NewConnectedRepository("org-4");
        var graphService = new FakeTeamsGraphService
        {
            Teams = [new TeamItem("team-1", "Team One", null, false)],
            Channels = [new ChannelItem("channel-1", "General", null, "standard", false)]
        };
        var service = new TeamsConfigurationService(graphService, repository, connectionRepository);

        await service.SaveAsync(
            "org-4", "proj-4", "app-4", "team-1", "channel-1", CancellationToken.None);

        graphService.ExceptionToThrow = new TeamsGraphException(
            StatusCodes.Status401Unauthorized,
            "Microsoft Teams access needs reconnecting.");

        var exception = await Assert.ThrowsAsync<TeamsGraphException>(() => service.SaveAsync(
            "org-4", "proj-4", "app-4", "team-1", "channel-1", CancellationToken.None));

        Assert.Equal(StatusCodes.Status401Unauthorized, exception.StatusCode);

        var connection = await connectionRepository.GetActiveAsync("org-4", CancellationToken.None);
        Assert.NotNull(connection);
        Assert.Equal(TeamsConfigurationStatus.NeedsReconnect, connection!.ConnectionStatus);
        Assert.Equal(TeamsGraphException.ReauthenticationRequiredCode, connection.ConnectionFailureCode);
        Assert.NotNull(connection.ConnectionFailureDetectedAtUtc);

        // The per-configuration status is no longer the reconnect signal (that's org-level now) —
        // it stays whatever it was set to at last successful save.
        var configuration = await repository.GetActiveAsync("org-4", "proj-4", "app-4", CancellationToken.None);
        Assert.Equal(TeamsConfigurationStatus.Active, configuration!.ConnectionStatus);
    }

    [Fact]
    public async Task SaveAsync_saves_nothing_when_graph_requires_reauthentication_on_a_first_time_save()
    {
        var collection = mongoFixture.CreateEmptyCollection();
        var repository = new MongoTeamsConfigurationRepository(collection);
        var connectionRepository = NewConnectedRepository("org-5");
        var graphService = new FakeTeamsGraphService
        {
            ExceptionToThrow = new TeamsGraphException(
                StatusCodes.Status401Unauthorized,
                "Microsoft Teams access needs reconnecting.")
        };
        var service = new TeamsConfigurationService(graphService, repository, connectionRepository);

        var exception = await Assert.ThrowsAsync<TeamsGraphException>(() => service.SaveAsync(
            "org-5", "proj-5", "app-5", "team-1", "channel-1", CancellationToken.None));

        Assert.Equal(StatusCodes.Status401Unauthorized, exception.StatusCode);
        Assert.Equal(0, await collection.CountDocumentsAsync(FilterDefinition<TeamsConfiguration>.Empty));
    }

    private MongoOrganizationTeamsConnectionRepository NewConnectedRepository(string organizationId)
    {
        var repository = new MongoOrganizationTeamsConnectionRepository(
            mongoFixture.CreateEmptyOrganizationConnectionsCollection());
        repository.UpsertAsync(
            new OrganizationTeamsConnection
            {
                OrganizationId = organizationId,
                TenantId = "tenant-1",
                UserObjectId = "user-1",
                ConnectedAsEmail = "connector@example.com",
                ConnectionStatus = TeamsConfigurationStatus.Active,
            },
            CancellationToken.None).GetAwaiter().GetResult();
        return repository;
    }
}
