using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using TeamsIntegration.Api.Configuration;

namespace TeamsIntegration.Api.Controllers;

[ApiController]
[Route("api/auth")]
public sealed class AuthController(IOptions<FrontendOptions> frontendOptions) : ControllerBase
{
    [AllowAnonymous]
    [HttpGet("connect")]
    [ProducesResponseType(StatusCodes.Status302Found)]
    public ChallengeResult Connect([FromQuery] string? returnUrl)
    {
        var baseUrl = frontendOptions.Value.BaseUrl;
        var properties = new AuthenticationProperties
        {
            RedirectUri = IsAllowedReturnUrl(returnUrl, baseUrl) ? returnUrl! : baseUrl
        };

        return Challenge(properties, OpenIdConnectDefaults.AuthenticationScheme);
    }

    [AllowAnonymous]
    [HttpGet("session")]
    [ProducesResponseType<SessionStatusResponse>(StatusCodes.Status200OK)]
    public ActionResult<SessionStatusResponse> GetSession()
    {
        var isAuthenticated = User.Identity?.IsAuthenticated ?? false;
        return Ok(new SessionStatusResponse(
            IsAuthenticated: isAuthenticated,
            IsTeamsConnected: isAuthenticated,
            DisplayName: isAuthenticated ? User.Identity?.Name : null));
    }

    private static bool IsAllowedReturnUrl(string? returnUrl, string baseUrl)
    {
        if (string.IsNullOrWhiteSpace(returnUrl))
        {
            return false;
        }

        if (!Uri.TryCreate(returnUrl, UriKind.Absolute, out var returnUri)
            || !Uri.TryCreate(baseUrl, UriKind.Absolute, out var baseUri))
        {
            return false;
        }

        return returnUri.Scheme == baseUri.Scheme
            && returnUri.Host == baseUri.Host
            && returnUri.Port == baseUri.Port;
    }

    [Authorize]
    [HttpPost("logout")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return NoContent();
    }
}

public sealed record SessionStatusResponse(bool IsAuthenticated, bool IsTeamsConnected, string? DisplayName);
