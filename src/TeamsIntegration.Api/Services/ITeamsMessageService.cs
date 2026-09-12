using TeamsIntegration.Api.DTOs;
using TeamsIntegration.Api.Models;

namespace TeamsIntegration.Api.Services;

public interface ITeamsMessageService
{
    Task<SendChannelMessageResponse> SendAsync(
        TeamsConfigurationDto configuration,
        string content,
        CancellationToken cancellationToken);
}
