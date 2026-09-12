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
        var service = new TeamsMessageService(graphService, NewRepository());

        var result = await service.SendAsync(NewConfigurationDto("org-1", "proj-1", "app-1"), "Hello", CancellationToken.None);

        Assert.Equal("msg-1", result.Id);
        Assert.Equal(1, graphService.GetTeamsCallCount);
    }

    [Fact]
    public async Task SendAsync_blocks_immediately_when_connection_needs_reconnect()
    {
        var graphService = new FakeTeamsGraphService();
        var service = new TeamsMessageService(graphService, NewRepository());
        var configuration = NewConfigurationDto("org-2", "proj-2", "app-2") with
        {
            ConnectionStatus = TeamsConfigurationStatus.NeedsReconnect,
        };

        var exception = await Assert.ThrowsAsync<TeamsGraphException>(
            () => service.SendAsync(configuration, "Hello", CancellationToken.None));

        Assert.Equal(StatusCodes.Status401Unauthorized, exception.StatusCode);
        Assert.Equal(0, graphService.GetTeamsCallCount);
    }

    [Fact]
    public async Task SendAsync_returns_409_when_team_is_no_longer_accessible()
    {
        var repository = NewRepository();
        var seeded = NewConfiguration("org-3", "proj-3", "app-3");
        await repository.UpsertAsync(seeded, CancellationToken.None);

        var graphService = new FakeTeamsGraphService { Teams = [], Channels = [] };
        var service = new TeamsMessageService(graphService, repository);

        var exception = await Assert.ThrowsAsync<TeamsGraphException>(
            () => service.SendAsync(NewConfigurationDto("org-3", "proj-3", "app-3"), "Hello", CancellationToken.None));

        Assert.Equal(StatusCodes.Status409Conflict, exception.StatusCode);

        var unchanged = await repository.GetActiveAsync("org-3", "proj-3", "app-3", CancellationToken.None);
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
        var service = new TeamsMessageService(graphService, NewRepository());

        var exception = await Assert.ThrowsAsync<TeamsGraphException>(
            () => service.SendAsync(NewConfigurationDto("org-4", "proj-4", "app-4"), "Hello", CancellationToken.None));

        Assert.Equal(StatusCodes.Status409Conflict, exception.StatusCode);
    }

    [Fact]
    public async Task SendAsync_marks_needsReconnect_in_mongo_when_graph_requires_reauthentication()
    {
        var repository = NewRepository();
        var seeded = NewConfiguration("org-5", "proj-5", "app-5");
        await repository.UpsertAsync(seeded, CancellationToken.None);

        var graphService = new FakeTeamsGraphService
        {
            ExceptionToThrow = new TeamsGraphException(
                StatusCodes.Status401Unauthorized,
                "Microsoft Teams access needs reconnecting."),
        };
        var service = new TeamsMessageService(graphService, repository);

        var exception = await Assert.ThrowsAsync<TeamsGraphException>(
            () => service.SendAsync(NewConfigurationDto("org-5", "proj-5", "app-5"), "Hello", CancellationToken.None));

        Assert.Equal(StatusCodes.Status401Unauthorized, exception.StatusCode);

        var updated = await repository.GetActiveAsync("org-5", "proj-5", "app-5", CancellationToken.None);
        Assert.Equal(TeamsConfigurationStatus.NeedsReconnect, updated!.ConnectionStatus);
        Assert.Equal(TeamsGraphException.ReauthenticationRequiredCode, updated.ConnectionFailureCode);
    }

    private MongoTeamsConfigurationRepository NewRepository() =>
        new(mongoFixture.CreateEmptyCollection());

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
            ConnectionStatus = TeamsConfigurationStatus.Active,
        };

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
