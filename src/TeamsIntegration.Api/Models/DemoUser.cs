using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace TeamsIntegration.Api.Models;

public sealed class DemoUser
{
    [BsonId]
    public ObjectId Id { get; set; }

    [BsonElement("email")]
    public required string Email { get; set; }

    [BsonElement("password")]
    public required string Password { get; set; }

    [BsonElement("displayName")]
    public required string DisplayName { get; set; }

    [BsonElement("organizationId")]
    public required string OrganizationId { get; set; }

    [BsonElement("projectId")]
    public required string ProjectId { get; set; }

    [BsonElement("applicationId")]
    public required string ApplicationId { get; set; }
}
