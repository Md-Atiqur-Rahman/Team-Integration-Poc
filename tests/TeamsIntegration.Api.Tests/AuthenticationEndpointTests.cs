using Microsoft.AspNetCore.Mvc.Testing;

namespace TeamsIntegration.Api.Tests;

public sealed class AuthenticationEndpointTests(WebApplicationFactory<Program> factory)
    : IClassFixture<WebApplicationFactory<Program>>
{
    [Fact]
    public async Task Session_redirects_anonymous_users_to_sign_in()
    {
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            BaseAddress = new Uri("https://localhost"),
            AllowAutoRedirect = false
        });

        var response = await client.GetAsync("/api/auth/session");

        Assert.Equal(System.Net.HttpStatusCode.Found, response.StatusCode);
        Assert.NotNull(response.Headers.Location);
    }
}
