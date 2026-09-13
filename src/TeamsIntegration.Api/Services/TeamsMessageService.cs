using TeamsIntegration.Api.DTOs;
using TeamsIntegration.Api.Models;
using TeamsIntegration.Api.Repositories;

namespace TeamsIntegration.Api.Services;

public sealed class TeamsMessageService(
    ITeamsGraphService teamsGraphService,
    IOrganizationTeamsConnectionRepository connectionRepository) : ITeamsMessageService
{
    public async Task<SendChannelMessageResponse> SendAsync(
        TeamsConfigurationDto configuration,
        string content,
        CancellationToken cancellationToken)
    {
        var connection = await connectionRepository.GetActiveAsync(configuration.OrganizationId, cancellationToken);
        if (connection is null || connection.ConnectionStatus == TeamsConfigurationStatus.NeedsReconnect)
        {
            throw new TeamsGraphException(
                StatusCodes.Status401Unauthorized,
                "Microsoft Teams access needs reconnecting.");
        }

        var identity = new GraphIdentity(connection.TenantId, connection.UserObjectId);

        IReadOnlyList<TeamItem> teams;
        try
        {
            teams = await teamsGraphService.GetTeamsAsync(identity, cancellationToken);
        }
        catch (TeamsGraphException ex) when (ex.StatusCode == StatusCodes.Status401Unauthorized)
        {
            await MarkNeedsReconnectAsync(configuration.OrganizationId, cancellationToken);
            throw;
        }

        if (teams.All(team => team.Id != configuration.TeamId))
        {
            throw new TeamsGraphException(
                StatusCodes.Status409Conflict,
                "Your saved channel is no longer available. Configure the application again.");
        }

        IReadOnlyList<ChannelItem> channels;
        try
        {
            channels = await teamsGraphService.GetChannelsAsync(identity, configuration.TeamId, cancellationToken);
        }
        catch (TeamsGraphException ex) when (ex.StatusCode == StatusCodes.Status401Unauthorized)
        {
            await MarkNeedsReconnectAsync(configuration.OrganizationId, cancellationToken);
            throw;
        }

        if (channels.All(channel => channel.Id != configuration.ChannelId))
        {
            throw new TeamsGraphException(
                StatusCodes.Status409Conflict,
                "Your saved channel is no longer available. Configure the application again.");
        }

        try
        {
            return await teamsGraphService.SendMessageAsync(
                identity,
                new SendChannelMessageRequest(configuration.TeamId, configuration.ChannelId, content),
                cancellationToken);
        }
        catch (TeamsGraphException ex) when (ex.StatusCode == StatusCodes.Status401Unauthorized)
        {
            await MarkNeedsReconnectAsync(configuration.OrganizationId, cancellationToken);
            throw;
        }
    }

    private Task MarkNeedsReconnectAsync(string organizationId, CancellationToken cancellationToken) =>
        connectionRepository.MarkNeedsReconnectAsync(
            organizationId,
            TeamsGraphException.ReauthenticationRequiredCode,
            cancellationToken);
}
