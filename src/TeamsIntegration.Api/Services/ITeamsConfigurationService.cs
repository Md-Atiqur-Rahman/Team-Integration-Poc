using TeamsIntegration.Api.DTOs;

namespace TeamsIntegration.Api.Services;

public interface ITeamsConfigurationService
{
    Task<TeamsConfigurationDto> SaveAsync(
        string organizationId,
        string projectId,
        string applicationId,
        string teamId,
        string channelId,
        string tenantId,
        string userObjectId,
        CancellationToken cancellationToken);

    Task<TeamsConfigurationDto?> GetActiveAsync(
        string organizationId,
        string projectId,
        string applicationId,
        CancellationToken cancellationToken);
}
