using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TeamsIntegration.Api.Models;
using TeamsIntegration.Api.Repositories;
using TeamsIntegration.Api.Services;

namespace TeamsIntegration.Api.Controllers;

// Anonymous by design: access is governed by the organization's stored Teams connection
// (ResolveIdentityAsync below), not the caller's own Entra session — any user in the org,
// with or without a personal sign-in, reaches these through the org-shared connection.
[ApiController]
[AllowAnonymous]
[Route("api/teams")]
public sealed class TeamsController(
    ITeamsGraphService teamsGraphService,
    IOrganizationTeamsConnectionRepository connectionRepository) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType<TeamsResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<TeamsResponse>> GetTeams(
        [FromQuery] string? organizationId,
        CancellationToken cancellationToken)
    {
        var identity = await ResolveIdentityAsync(organizationId, cancellationToken);
        if (identity is null)
        {
            return IdentityResolutionError(organizationId);
        }

        return Ok(new TeamsResponse(await teamsGraphService.GetTeamsAsync(identity, cancellationToken)));
    }

    [HttpGet("{teamId}/channels")]
    [ProducesResponseType<ChannelsResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ChannelsResponse>> GetChannels(
        [FromRoute] string teamId,
        [FromQuery] string? organizationId,
        CancellationToken cancellationToken)
    {
        if (!GraphInput.IsValidIdentifier(teamId))
        {
            return BadRequest(new { code = "invalid_team_id", message = "Team ID is invalid." });
        }

        var identity = await ResolveIdentityAsync(organizationId, cancellationToken);
        if (identity is null)
        {
            return IdentityResolutionError(organizationId);
        }

        return Ok(new ChannelsResponse(
            await teamsGraphService.GetChannelsAsync(identity, teamId, cancellationToken)));
    }

    private async Task<GraphIdentity?> ResolveIdentityAsync(string? organizationId, CancellationToken cancellationToken)
    {
        if (!GraphInput.IsValidIdentifier(organizationId))
        {
            return null;
        }

        var connection = await connectionRepository.GetActiveAsync(organizationId!, cancellationToken);
        return connection is null ? null : new GraphIdentity(connection.TenantId, connection.UserObjectId);
    }

    private ActionResult IdentityResolutionError(string? organizationId) =>
        !GraphInput.IsValidIdentifier(organizationId)
            ? BadRequest(new { code = "invalid_organization_id", message = "Organization ID is invalid." })
            : NotFound(new
            {
                code = "organization_not_connected",
                message = "This organization has no Microsoft Teams connection yet."
            });
}
