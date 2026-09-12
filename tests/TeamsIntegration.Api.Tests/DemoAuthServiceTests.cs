using TeamsIntegration.Api.Services;

namespace TeamsIntegration.Api.Tests;

[Collection(nameof(MongoTestCollection))]
public sealed class DemoAuthServiceTests(MongoFixture mongoFixture)
{
    public static IEnumerable<object[]> SeededUsers()
    {
        yield return ["himel@demo.autom", "Himel", "IGB2B", "Ecohub", "EcohubApp"];
        yield return ["ratan@demo.autom", "Ratan", "IGB2B", "Ecohub", "EcohubApp"];
        yield return ["sabbir@demo.autom", "Sabbir", "IGB2B", "Ecap", "EcapApp"];
        yield return ["anik@demo.autom", "Anik", "IGB2B", "Ecap", "EcapApp"];
        yield return ["jim@demo.autom", "Jim", "SwissLife", "SwissLifeProject", "SwissLifeApp"];
    }

    [Theory]
    [MemberData(nameof(SeededUsers))]
    public async Task ValidateCredentialsAsync_resolves_each_seeded_user(
        string email,
        string displayName,
        string organizationId,
        string projectId,
        string applicationId)
    {
        var service = new DemoAuthService(mongoFixture.CreateEmptyDemoUsersCollection());

        var user = await service.ValidateCredentialsAsync(email, "Demo@123", CancellationToken.None);

        Assert.NotNull(user);
        Assert.Equal(displayName, user!.DisplayName);
        Assert.Equal(organizationId, user.OrganizationId);
        Assert.Equal(projectId, user.ProjectId);
        Assert.Equal(applicationId, user.ApplicationId);
    }

    [Fact]
    public async Task ValidateCredentialsAsync_returns_null_for_a_wrong_password()
    {
        var service = new DemoAuthService(mongoFixture.CreateEmptyDemoUsersCollection());

        var user = await service.ValidateCredentialsAsync("himel@demo.autom", "wrong-password", CancellationToken.None);

        Assert.Null(user);
    }

    [Fact]
    public async Task ValidateCredentialsAsync_returns_null_for_an_unknown_email()
    {
        var service = new DemoAuthService(mongoFixture.CreateEmptyDemoUsersCollection());

        var user = await service.ValidateCredentialsAsync("nobody@demo.autom", "Demo@123", CancellationToken.None);

        Assert.Null(user);
    }
}
