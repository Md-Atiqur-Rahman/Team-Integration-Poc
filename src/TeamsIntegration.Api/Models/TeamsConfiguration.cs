using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace TeamsIntegration.Api.Models;

public sealed class TeamsConfiguration
{
    [BsonId]
    public ObjectId Id { get; set; }

    [BsonElement("organizationId")]
    public required string OrganizationId { get; set; }

    [BsonElement("projectId")]
    public required string ProjectId { get; set; }

    [BsonElement("applicationId")]
    public required string ApplicationId { get; set; }

    [BsonElement("tenantId")]
    public required string TenantId { get; set; }

    [BsonElement("userObjectId")]
    public required string UserObjectId { get; set; }

    [BsonElement("teamId")]
    public required string TeamId { get; set; }

    [BsonElement("teamName")]
    public string? TeamName { get; set; }

    [BsonElement("channelId")]
    public required string ChannelId { get; set; }

    [BsonElement("channelName")]
    public string? ChannelName { get; set; }

    [BsonElement("connectionStatus")]
    public required string ConnectionStatus { get; set; }

    [BsonElement("connectionFailureCode")]
    public string? ConnectionFailureCode { get; set; }

    [BsonElement("connectionFailureDetectedAtUtc")]
    public DateTime? ConnectionFailureDetectedAtUtc { get; set; }

    [BsonElement("connectionAlertedAtUtc")]
    public DateTime? ConnectionAlertedAtUtc { get; set; }

    [BsonElement("createdAtUtc")]
    public DateTime CreatedAtUtc { get; set; }

    [BsonElement("updatedAtUtc")]
    public DateTime UpdatedAtUtc { get; set; }
}

public static class TeamsConfigurationStatus
{
    public const string Active = "active";
    public const string NeedsReconnect = "needsReconnect";
}
