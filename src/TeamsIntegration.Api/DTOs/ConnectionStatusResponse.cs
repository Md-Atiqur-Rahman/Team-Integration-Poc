namespace TeamsIntegration.Api.DTOs;

public sealed record ConnectionStatusResponse(bool IsConnected, string ConnectionStatus, string? ConnectedAsEmail);
