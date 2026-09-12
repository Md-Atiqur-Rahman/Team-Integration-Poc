namespace TeamsIntegration.Api.DTOs;

public sealed record SaveTeamsConfigurationRequest(
    string? OrganizationId,
    string? ProjectId,
    string? ApplicationId,
    string? TeamId,
    string? ChannelId);
