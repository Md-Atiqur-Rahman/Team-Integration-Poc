namespace TeamsIntegration.Api.DTOs;

public sealed record TeamsConfigurationDto(
    string OrganizationId,
    string ProjectId,
    string ApplicationId,
    string TenantId,
    string UserObjectId,
    string TeamId,
    string? TeamName,
    string ChannelId,
    string? ChannelName,
    string ConnectionStatus,
    string? ConnectionFailureCode,
    DateTime? ConnectionFailureDetectedAtUtc,
    DateTime? ConnectionAlertedAtUtc,
    DateTime CreatedAtUtc,
    DateTime UpdatedAtUtc);
