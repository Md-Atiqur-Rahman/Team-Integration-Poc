using TeamsIntegration.Api.DTOs;
using TeamsIntegration.Api.Models;
using TeamsIntegration.Api.Repositories;

namespace TeamsIntegration.Api.Services;

public sealed class TeamsConfigurationService(
    ITeamsGraphService teamsGraphService,
    ITeamsConfigurationRepository repository) : ITeamsConfigurationService
{
    public async Task<TeamsConfigurationDto> SaveAsync(
        string organizationId,
        string projectId,
        string applicationId,
        string teamId,
        string channelId,
        string tenantId,
        string userObjectId,
        CancellationToken cancellationToken)
    {
        IReadOnlyList<TeamItem> teams;
        try
        {
            teams = await teamsGraphService.GetTeamsAsync(cancellationToken);
        }
        catch (TeamsGraphException ex) when (ex.StatusCode == StatusCodes.Status401Unauthorized)
        {
            await MarkNeedsReconnectAsync(organizationId, projectId, applicationId, cancellationToken);
            throw;
        }

        var matchedTeam = teams.FirstOrDefault(team => team.Id == teamId);
        if (matchedTeam is null)
        {
            throw new TeamsGraphException(
                StatusCodes.Status404NotFound,
                "The selected Team is no longer available or accessible.");
        }

        IReadOnlyList<ChannelItem> channels;
        try
        {
            channels = await teamsGraphService.GetChannelsAsync(teamId, cancellationToken);
        }
        catch (TeamsGraphException ex) when (ex.StatusCode == StatusCodes.Status401Unauthorized)
        {
            await MarkNeedsReconnectAsync(organizationId, projectId, applicationId, cancellationToken);
            throw;
        }

        var matchedChannel = channels.FirstOrDefault(channel => channel.Id == channelId);
        if (matchedChannel is null)
        {
            throw new TeamsGraphException(
                StatusCodes.Status404NotFound,
                "The selected channel is no longer available or accessible.");
        }

        var configuration = new TeamsConfiguration
        {
            OrganizationId = organizationId,
            ProjectId = projectId,
            ApplicationId = applicationId,
            TenantId = tenantId,
            UserObjectId = userObjectId,
            TeamId = matchedTeam.Id,
            TeamName = matchedTeam.DisplayName,
            ChannelId = matchedChannel.Id,
            ChannelName = matchedChannel.DisplayName,
            ConnectionStatus = TeamsConfigurationStatus.Active,
            ConnectionFailureCode = null,
            ConnectionFailureDetectedAtUtc = null,
            ConnectionAlertedAtUtc = null
        };

        var saved = await repository.UpsertAsync(configuration, cancellationToken);
        return ToDto(saved);
    }

    public async Task<TeamsConfigurationDto?> GetActiveAsync(
        string organizationId,
        string projectId,
        string applicationId,
        CancellationToken cancellationToken)
    {
        var configuration = await repository.GetActiveAsync(
            organizationId,
            projectId,
            applicationId,
            cancellationToken);

        return configuration is null ? null : ToDto(configuration);
    }

    private Task MarkNeedsReconnectAsync(
        string organizationId,
        string projectId,
        string applicationId,
        CancellationToken cancellationToken) =>
        repository.MarkNeedsReconnectAsync(
            organizationId,
            projectId,
            applicationId,
            TeamsGraphException.ReauthenticationRequiredCode,
            cancellationToken);

    private static TeamsConfigurationDto ToDto(TeamsConfiguration configuration) => new(
        configuration.OrganizationId,
        configuration.ProjectId,
        configuration.ApplicationId,
        configuration.TenantId,
        configuration.UserObjectId,
        configuration.TeamId,
        configuration.TeamName,
        configuration.ChannelId,
        configuration.ChannelName,
        configuration.ConnectionStatus,
        configuration.ConnectionFailureCode,
        configuration.ConnectionFailureDetectedAtUtc,
        configuration.ConnectionAlertedAtUtc,
        configuration.CreatedAtUtc,
        configuration.UpdatedAtUtc);
}
