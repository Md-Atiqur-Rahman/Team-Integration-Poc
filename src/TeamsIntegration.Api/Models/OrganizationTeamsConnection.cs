using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace TeamsIntegration.Api.Models;

public sealed class OrganizationTeamsConnection
{
    [BsonId]
    public ObjectId Id { get; set; }

    [BsonElement("organizationId")]
    public required string OrganizationId { get; set; }

    [BsonElement("tenantId")]
    public required string TenantId { get; set; }

    [BsonElement("userObjectId")]
    public required string UserObjectId { get; set; }

    [BsonElement("connectedAsEmail")]
    public string? ConnectedAsEmail { get; set; }

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
