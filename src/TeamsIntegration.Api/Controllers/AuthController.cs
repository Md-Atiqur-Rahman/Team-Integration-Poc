using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Options;
using Microsoft.Identity.Web;
using TeamsIntegration.Api.Configuration;
using TeamsIntegration.Api.DTOs;
using TeamsIntegration.Api.Models;
using TeamsIntegration.Api.Repositories;

namespace TeamsIntegration.Api.Controllers;

[ApiController]
[Route("api/auth")]
public sealed class AuthController(
    IOptions<FrontendOptions> frontendOptions,
    IOrganizationTeamsConnectionRepository connectionRepository) : ControllerBase
{
    [AllowAnonymous]
    [HttpGet("connect")]
    [ProducesResponseType(StatusCodes.Status302Found)]
    public ChallengeResult Connect([FromQuery] string? returnUrl)
    {
        var baseUrl = frontendOptions.Value.BaseUrl;
        var validatedReturnUrl = IsAllowedReturnUrl(returnUrl, baseUrl) ? returnUrl! : baseUrl;
        var organizationId = ExtractOrganizationId(validatedReturnUrl);

        // Redirect through our own connect-complete action first (not straight to Angular) so we
        // have an authenticated HttpContext to read oid/tid from and persist the org connection —
        // those claims don't exist yet at this point, before the OAuth round trip completes.
        var completeUrl = Url.Action(
            action: nameof(ConnectComplete),
            controller: null,
            values: new { organizationId, returnUrl = validatedReturnUrl },
            protocol: Request.Scheme,
            host: Request.Host.Value)!;

        var properties = new AuthenticationProperties
        {
            RedirectUri = completeUrl
        };

        return Challenge(properties, OpenIdConnectDefaults.AuthenticationScheme);
    }

    [Authorize]
    [HttpGet("connect-complete")]
    [ProducesResponseType(StatusCodes.Status302Found)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ConnectComplete(
        [FromQuery] string? organizationId,
        [FromQuery] string? returnUrl,
        CancellationToken cancellationToken)
    {
        if (!GraphInput.IsValidIdentifier(organizationId))
        {
            return BadRequest(new { code = "invalid_organization_id", message = "Organization ID is invalid." });
        }

        var baseUrl = frontendOptions.Value.BaseUrl;
        var validatedReturnUrl = IsAllowedReturnUrl(returnUrl, baseUrl) ? returnUrl! : baseUrl;

        var tenantId = User.FindFirst(ClaimConstants.TenantId)?.Value;
        var userObjectId = User.FindFirst(ClaimConstants.ObjectId)?.Value;
        if (string.IsNullOrWhiteSpace(tenantId) || string.IsNullOrWhiteSpace(userObjectId))
        {
            return BadRequest(new
            {
                code = "microsoft_identity_missing",
                message = "The connected Microsoft identity could not be determined."
            });
        }

        await connectionRepository.UpsertAsync(
            new OrganizationTeamsConnection
            {
                OrganizationId = organizationId!,
                TenantId = tenantId,
                UserObjectId = userObjectId,
                ConnectedAsEmail = User.Identity?.Name,
                ConnectionStatus = TeamsConfigurationStatus.Active,
                ConnectionFailureCode = null,
                ConnectionFailureDetectedAtUtc = null,
                ConnectionAlertedAtUtc = null
            },
            cancellationToken);

        return Redirect(validatedReturnUrl);
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

    [AllowAnonymous]
    [HttpGet("connection-status")]
    [ProducesResponseType<ConnectionStatusResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ConnectionStatusResponse>> GetConnectionStatus(
        [FromQuery] string? organizationId,
        CancellationToken cancellationToken)
    {
        if (!GraphInput.IsValidIdentifier(organizationId))
        {
            return BadRequest(new { code = "invalid_organization_id", message = "Organization ID is invalid." });
        }

        var connection = await connectionRepository.GetActiveAsync(organizationId!, cancellationToken);
        return Ok(connection is null
            ? new ConnectionStatusResponse(false, "none", null)
            : new ConnectionStatusResponse(true, connection.ConnectionStatus, connection.ConnectedAsEmail));
    }

    [Authorize]
    [HttpPost("logout")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return NoContent();
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

    private static string? ExtractOrganizationId(string url)
    {
        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri))
        {
            return null;
        }

        var query = QueryHelpers.ParseQuery(uri.Query);
        return query.TryGetValue("organizationId", out var value) ? value.ToString() : null;
    }
}

public sealed record SessionStatusResponse(bool IsAuthenticated, bool IsTeamsConnected, string? DisplayName);
