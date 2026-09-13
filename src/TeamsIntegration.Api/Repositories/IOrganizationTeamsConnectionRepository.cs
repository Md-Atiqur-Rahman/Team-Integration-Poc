using TeamsIntegration.Api.Models;

namespace TeamsIntegration.Api.Repositories;

public interface IOrganizationTeamsConnectionRepository
{
    Task<OrganizationTeamsConnection?> GetActiveAsync(string organizationId, CancellationToken cancellationToken);

    Task<OrganizationTeamsConnection> UpsertAsync(
        OrganizationTeamsConnection connection,
        CancellationToken cancellationToken);

    Task MarkNeedsReconnectAsync(string organizationId, string failureCode, CancellationToken cancellationToken);
}
