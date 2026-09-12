using TeamsIntegration.Api.Models;

namespace TeamsIntegration.Api.Services;

public interface IDemoAuthService
{
    Task<DemoUser?> ValidateCredentialsAsync(string email, string password, CancellationToken cancellationToken);
}
