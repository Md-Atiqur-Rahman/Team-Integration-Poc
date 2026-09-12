namespace TeamsIntegration.Api.DTOs;

public sealed record DemoLoginResponse(
    string DisplayName,
    string OrganizationId,
    string ProjectId,
    string ApplicationId);
