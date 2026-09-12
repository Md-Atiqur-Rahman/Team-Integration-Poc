using MongoDB.Driver;
using TeamsIntegration.Api.Models;

namespace TeamsIntegration.Api.Services;

/// <summary>
/// Demo-only login used to carry a fixed {organizationId, projectId, applicationId} into the
/// dashboard for manual multi-user demos. Not part of the real Autom integration or Task 1's
/// future host-context contract — plain-text password compare is intentional, not an oversight.
/// </summary>
public sealed class DemoAuthService(IMongoCollection<DemoUser> collection) : IDemoAuthService
{
    private static readonly DemoUser[] SeedUsers =
    [
        new()
        {
            Email = "himel@demo.autom",
            Password = "Demo@123",
            DisplayName = "Himel",
            OrganizationId = "IGB2B",
            ProjectId = "Ecohub",
            ApplicationId = "EcohubApp",
        },
        new()
        {
            Email = "ratan@demo.autom",
            Password = "Demo@123",
            DisplayName = "Ratan",
            OrganizationId = "IGB2B",
            ProjectId = "Ecohub",
            ApplicationId = "EcohubApp",
        },
        new()
        {
            Email = "sabbir@demo.autom",
            Password = "Demo@123",
            DisplayName = "Sabbir",
            OrganizationId = "IGB2B",
            ProjectId = "Ecap",
            ApplicationId = "EcapApp",
        },
        new()
        {
            Email = "anik@demo.autom",
            Password = "Demo@123",
            DisplayName = "Anik",
            OrganizationId = "IGB2B",
            ProjectId = "Ecap",
            ApplicationId = "EcapApp",
        },
        new()
        {
            Email = "jim@demo.autom",
            Password = "Demo@123",
            DisplayName = "Jim",
            OrganizationId = "SwissLife",
            ProjectId = "SwissLifeProject",
            ApplicationId = "SwissLifeApp",
        },
    ];

    public async Task<DemoUser?> ValidateCredentialsAsync(
        string email,
        string password,
        CancellationToken cancellationToken)
    {
        await EnsureSeededAsync(cancellationToken);

        var user = await collection.Find(u => u.Email == email).FirstOrDefaultAsync(cancellationToken);
        return user is not null && user.Password == password ? user : null;
    }

    private async Task EnsureSeededAsync(CancellationToken cancellationToken)
    {
        foreach (var user in SeedUsers)
        {
            var update = Builders<DemoUser>.Update
                .Set(u => u.Email, user.Email)
                .Set(u => u.Password, user.Password)
                .Set(u => u.DisplayName, user.DisplayName)
                .Set(u => u.OrganizationId, user.OrganizationId)
                .Set(u => u.ProjectId, user.ProjectId)
                .Set(u => u.ApplicationId, user.ApplicationId);

            // Field-by-field $set (not a whole-document ReplaceOneAsync) so an insert lets
            // MongoDB generate _id itself — a replacement document here would carry the
            // default ObjectId.Empty for every seed user and collide on the second insert.
            await collection.UpdateOneAsync(
                u => u.Email == user.Email,
                update,
                new UpdateOptions { IsUpsert = true },
                cancellationToken);
        }
    }
}
