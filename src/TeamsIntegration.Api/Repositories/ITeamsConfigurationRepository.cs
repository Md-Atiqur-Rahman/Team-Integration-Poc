using TeamsIntegration.Api.Models;

namespace TeamsIntegration.Api.Repositories;

public interface ITeamsConfigurationRepository
{
    Task<TeamsConfiguration?> GetActiveAsync(
        string organizationId,
        string projectId,
        string applicationId,
        CancellationToken cancellationToken);

    Task<TeamsConfiguration> UpsertAsync(TeamsConfiguration configuration, CancellationToken cancellationToken);
}
