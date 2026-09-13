using Microsoft.AspNetCore.Http;
using TeamsIntegration.Api.DTOs;
using TeamsIntegration.Api.Models;
using TeamsIntegration.Api.Repositories;
using TeamsIntegration.Api.Services;

namespace TeamsIntegration.Api.Tests;

[Collection(nameof(MongoTestCollection))]
public sealed class TeamsMessageServiceTests(MongoFixture mongoFixture)
{
    [Fact]
    public async Task SendAsync_succeeds_when_team_and_channel_are_still_accessible()
    {
        var graphService = new FakeTeamsGraphService
        {
            Teams = [new TeamItem("team-1", "Team One", null, false)],
            Channels = [new ChannelItem("channel-1", "General", null, "standard", false)],
            SendMessageResponse = new SendChannelMessageResponse("msg-1", DateTimeOffset.UtcNow, "https://teams.example/msg-1"),
        };
        var service = new TeamsMessageService(graphService, NewConnectedRepository("org-1"));

        var result = await service.SendAsync(NewConfigurationDto("org-1", "proj-1", "app-1"), "Hello", CancellationToken.None);

        Assert.Equal("msg-1", result.Id);
        Assert.Equal(1, graphService.GetTeamsCallCount);
    }

    [Fact]
    public async Task SendAsync_blocks_immediately_when_no_organization_connection_exists()
    {
        var graphService = new FakeTeamsGraphService();
        var connectionRepository = new MongoOrganizationTeamsConnectionRepository(
            mongoFixture.CreateEmptyOrganizationConnectionsCollection());
        var service = new TeamsMessageService(graphService, connectionRepository);

        var exception = await Assert.ThrowsAsync<TeamsGraphException>(
            () => service.SendAsync(NewConfigurationDto("org-unconnected", "proj-1", "app-1"), "Hello", CancellationToken.None));

        Assert.Equal(StatusCodes.Status401Unauthorized, exception.StatusCode);
        Assert.Equal(0, graphService.GetTeamsCallCount);
    }

    [Fact]
    public async Task SendAsync_blocks_immediately_when_connection_needs_reconnect()
    {
        var graphService = new FakeTeamsGraphService();
        var connectionRepository = NewConnectedRepository("org-2");
        await connectionRepository.MarkNeedsReconnectAsync(
            "org-2", TeamsGraphException.ReauthenticationRequiredCode, CancellationToken.None);
        var service = new TeamsMessageService(graphService, connectionRepository);

        var exception = await Assert.ThrowsAsync<TeamsGraphException>(
            () => service.SendAsync(NewConfigurationDto("org-2", "proj-2", "app-2"), "Hello", CancellationToken.None));

        Assert.Equal(StatusCodes.Status401Unauthorized, exception.StatusCode);
        Assert.Equal(0, graphService.GetTeamsCallCount);
    }

    [Fact]
    public async Task SendAsync_returns_409_when_team_is_no_longer_accessible()
    {
        var connectionRepository = NewConnectedRepository("org-3");

        var graphService = new FakeTeamsGraphService { Teams = [], Channels = [] };
        var service = new TeamsMessageService(graphService, connectionRepository);

        var exception = await Assert.ThrowsAsync<TeamsGraphException>(
            () => service.SendAsync(NewConfigurationDto("org-3", "proj-3", "app-3"), "Hello", CancellationToken.None));

        Assert.Equal(StatusCodes.Status409Conflict, exception.StatusCode);

        var unchanged = await connectionRepository.GetActiveAsync("org-3", CancellationToken.None);
        Assert.Equal(TeamsConfigurationStatus.Active, unchanged!.ConnectionStatus);
    }

    [Fact]
    public async Task SendAsync_returns_409_when_channel_is_no_longer_accessible()
    {
        var graphService = new FakeTeamsGraphService
        {
            Teams = [new TeamItem("team-1", "Team One", null, false)],
            Channels = [],
        };
        var service = new TeamsMessageService(graphService, NewConnectedRepository("org-4"));

        var exception = await Assert.ThrowsAsync<TeamsGraphException>(
            () => service.SendAsync(NewConfigurationDto("org-4", "proj-4", "app-4"), "Hello", CancellationToken.None));

        Assert.Equal(StatusCodes.Status409Conflict, exception.StatusCode);
    }

    [Fact]
    public async Task SendAsync_marks_the_organization_connection_needsReconnect_when_graph_requires_reauthentication()
    {
        var connectionRepository = NewConnectedRepository("org-5");

        var graphService = new FakeTeamsGraphService
        {
            ExceptionToThrow = new TeamsGraphException(
                StatusCodes.Status401Unauthorized,
                "Microsoft Teams access needs reconnecting."),
        };
        var service = new TeamsMessageService(graphService, connectionRepository);

        var exception = await Assert.ThrowsAsync<TeamsGraphException>(
            () => service.SendAsync(NewConfigurationDto("org-5", "proj-5", "app-5"), "Hello", CancellationToken.None));

        Assert.Equal(StatusCodes.Status401Unauthorized, exception.StatusCode);

        var updated = await connectionRepository.GetActiveAsync("org-5", CancellationToken.None);
        Assert.Equal(TeamsConfigurationStatus.NeedsReconnect, updated!.ConnectionStatus);
        Assert.Equal(TeamsGraphException.ReauthenticationRequiredCode, updated.ConnectionFailureCode);
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

    private static TeamsConfigurationDto NewConfigurationDto(
        string organizationId,
        string projectId,
        string applicationId) =>
        new(
            organizationId,
            projectId,
            applicationId,
            "tenant-1",
            "user-1",
            "team-1",
            "Team One",
            "channel-1",
            "General",
            TeamsConfigurationStatus.Active,
            null,
            null,
            null,
            DateTime.UtcNow,
            DateTime.UtcNow);
}
