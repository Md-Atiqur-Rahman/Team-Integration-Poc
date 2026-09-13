using Microsoft.AspNetCore.Mvc;
using TeamsIntegration.Api.DTOs;
using TeamsIntegration.Api.Models;
using TeamsIntegration.Api.Services;

namespace TeamsIntegration.Api.Controllers;

// Anonymous by design, both actions: saving a channel selection revalidates against Graph
// using the org's stored connection (TeamsConfigurationService resolves that internally), not
// the caller's own Entra session — any user in the org/project can configure a channel once the
// org is connected, without their own Microsoft sign-in.
[ApiController]
[Route("api/teams/configuration")]
public sealed class TeamsConfigurationController(ITeamsConfigurationService teamsConfigurationService)
    : ControllerBase
{
    [HttpPut]
    [ProducesResponseType<TeamsConfigurationDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<TeamsConfigurationDto>> SaveConfiguration(
        SaveTeamsConfigurationRequest request,
        CancellationToken cancellationToken)
    {
        var validationError = Validate(request);
        if (validationError is not null)
        {
            return BadRequest(new { code = validationError.Value.Code, message = validationError.Value.Message });
        }

        var configuration = await teamsConfigurationService.SaveAsync(
            request.OrganizationId!,
            request.ProjectId!,
            request.ApplicationId!,
            request.TeamId!,
            request.ChannelId!,
            cancellationToken);

        return Ok(configuration);
    }

    [HttpGet]
    [ProducesResponseType<TeamsConfigurationDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<TeamsConfigurationDto>> GetConfiguration(
        [FromQuery] string? organizationId,
        [FromQuery] string? projectId,
        [FromQuery] string? applicationId,
        CancellationToken cancellationToken)
    {
        if (!GraphInput.IsValidIdentifier(organizationId))
        {
            return BadRequest(new { code = "invalid_organization_id", message = "Organization ID is invalid." });
        }

        if (!GraphInput.IsValidIdentifier(projectId))
        {
            return BadRequest(new { code = "invalid_project_id", message = "Project ID is invalid." });
        }

        if (!GraphInput.IsValidIdentifier(applicationId))
        {
            return BadRequest(new { code = "invalid_application_id", message = "Application ID is invalid." });
        }

        var configuration = await teamsConfigurationService.GetActiveAsync(
            organizationId!,
            projectId!,
            applicationId!,
            cancellationToken);

        if (configuration is null)
        {
            return NotFound(new
            {
                code = "configuration_not_found",
                message = "No Teams configuration is saved for this organization, project, and application."
            });
        }

        return Ok(configuration);
    }

    private static (string Code, string Message)? Validate(SaveTeamsConfigurationRequest request)
    {
        if (!GraphInput.IsValidIdentifier(request.OrganizationId))
        {
            return ("invalid_organization_id", "Organization ID is required and must not exceed 512 characters.");
        }

        if (!GraphInput.IsValidIdentifier(request.ProjectId))
        {
            return ("invalid_project_id", "Project ID is required and must not exceed 512 characters.");
        }

        if (!GraphInput.IsValidIdentifier(request.ApplicationId))
        {
            return ("invalid_application_id", "Application ID is required and must not exceed 512 characters.");
        }

        if (!GraphInput.IsValidIdentifier(request.TeamId))
        {
            return ("invalid_team_id", "Team ID is required and must not exceed 512 characters.");
        }

        if (!GraphInput.IsValidIdentifier(request.ChannelId))
        {
            return ("invalid_channel_id", "Channel ID is required and must not exceed 512 characters.");
        }

        return null;
    }
}
