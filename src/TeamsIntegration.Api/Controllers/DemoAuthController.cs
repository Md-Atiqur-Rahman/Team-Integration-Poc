using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TeamsIntegration.Api.DTOs;
using TeamsIntegration.Api.Services;

namespace TeamsIntegration.Api.Controllers;

/// <summary>
/// Demo-only login carrying a fixed {organizationId, projectId, applicationId} into the
/// dashboard for manual multi-user demos. Not part of the real Autom integration.
/// </summary>
[ApiController]
[AllowAnonymous]
[Route("api/demo")]
public sealed class DemoAuthController(IDemoAuthService demoAuthService) : ControllerBase
{
    [HttpPost("login")]
    [ProducesResponseType<DemoLoginResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<DemoLoginResponse>> Login(
        DemoLoginRequest request,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Password))
        {
            return BadRequest(new { code = "invalid_request", message = "Email and password are required." });
        }

        var user = await demoAuthService.ValidateCredentialsAsync(request.Email, request.Password, cancellationToken);
        if (user is null)
        {
            return Unauthorized(new { code = "invalid_credentials", message = "Invalid email or password." });
        }

        return Ok(new DemoLoginResponse(user.DisplayName, user.OrganizationId, user.ProjectId, user.ApplicationId));
    }
}
