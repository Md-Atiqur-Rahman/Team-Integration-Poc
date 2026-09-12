using TeamsIntegration.Api.DTOs;
using TeamsIntegration.Api.Models;
using TeamsIntegration.Api.Repositories;

namespace TeamsIntegration.Api.Services;

public sealed class TeamsMessageService(
    ITeamsGraphService teamsGraphService,
    ITeamsConfigurationRepository repository) : ITeamsMessageService
{
    public async Task<SendChannelMessageResponse> SendAsync(
        TeamsConfigurationDto configuration,
        string content,
        CancellationToken cancellationToken)
    {
        if (configuration.ConnectionStatus == TeamsConfigurationStatus.NeedsReconnect)
        {
            throw new TeamsGraphException(
                StatusCodes.Status401Unauthorized,
                "Microsoft Teams access needs reconnecting.");
        }

        IReadOnlyList<TeamItem> teams;
        try
        {
            teams = await teamsGraphService.GetTeamsAsync(cancellationToken);
        }
        catch (TeamsGraphException ex) when (ex.StatusCode == StatusCodes.Status401Unauthorized)
        {
            await MarkNeedsReconnectAsync(configuration, cancellationToken);
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
            channels = await teamsGraphService.GetChannelsAsync(configuration.TeamId, cancellationToken);
        }
        catch (TeamsGraphException ex) when (ex.StatusCode == StatusCodes.Status401Unauthorized)
        {
            await MarkNeedsReconnectAsync(configuration, cancellationToken);
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
                new SendChannelMessageRequest(configuration.TeamId, configuration.ChannelId, content),
                cancellationToken);
        }
        catch (TeamsGraphException ex) when (ex.StatusCode == StatusCodes.Status401Unauthorized)
        {
            await MarkNeedsReconnectAsync(configuration, cancellationToken);
            throw;
        }
    }

    private Task MarkNeedsReconnectAsync(TeamsConfigurationDto configuration, CancellationToken cancellationToken) =>
        repository.MarkNeedsReconnectAsync(
            configuration.OrganizationId,
            configuration.ProjectId,
            configuration.ApplicationId,
            TeamsGraphException.ReauthenticationRequiredCode,
            cancellationToken);
}
