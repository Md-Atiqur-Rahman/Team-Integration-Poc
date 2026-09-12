using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace TeamsIntegration.Api.Controllers;

[ApiController]
[Route("api/auth")]
public sealed class AuthController : ControllerBase
{
    [AllowAnonymous]
    [HttpGet("connect")]
    [ProducesResponseType(StatusCodes.Status302Found)]
    public ChallengeResult Connect()
    {
        var properties = new AuthenticationProperties
        {
            RedirectUri = "/swagger"
        };

        return Challenge(properties, OpenIdConnectDefaults.AuthenticationScheme);
    }

    [Authorize]
    [HttpGet("session")]
    [ProducesResponseType<SessionStatusResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public ActionResult<SessionStatusResponse> GetSession()
    {
        return Ok(new SessionStatusResponse(
            IsAuthenticated: true,
            DisplayName: User.Identity?.Name));
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

public sealed record SessionStatusResponse(bool IsAuthenticated, string? DisplayName);
