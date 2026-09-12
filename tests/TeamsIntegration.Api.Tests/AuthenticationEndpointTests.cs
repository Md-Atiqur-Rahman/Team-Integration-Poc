using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;

namespace TeamsIntegration.Api.Tests;

public sealed class AuthenticationEndpointTests(WebApplicationFactory<Program> factory)
    : IClassFixture<WebApplicationFactory<Program>>
{
    [Fact]
    public async Task Session_returns_not_authenticated_for_anonymous_users()
    {
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            BaseAddress = new Uri("https://localhost"),
            AllowAutoRedirect = false
        });

        var response = await client.GetAsync("/api/auth/session");

        Assert.Equal(System.Net.HttpStatusCode.OK, response.StatusCode);
        var session = await response.Content.ReadFromJsonAsync<SessionStatus>();
        Assert.NotNull(session);
        Assert.False(session!.IsAuthenticated);
        Assert.False(session.IsTeamsConnected);
        Assert.Null(session.DisplayName);
    }

    private sealed record SessionStatus(bool IsAuthenticated, bool IsTeamsConnected, string? DisplayName);
}
