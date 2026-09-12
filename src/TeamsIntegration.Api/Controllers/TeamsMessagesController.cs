using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TeamsIntegration.Api.DTOs;
using TeamsIntegration.Api.Models;
using TeamsIntegration.Api.Services;

namespace TeamsIntegration.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/teams/messages")]
public sealed class TeamsMessagesController(
    ITeamsConfigurationService teamsConfigurationService,
    ITeamsMessageService teamsMessageService) : ControllerBase
{
    [HttpPost]
    [ProducesResponseType<SendChannelMessageResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<ActionResult<SendChannelMessageResponse>> SendMessage(
        [FromQuery] string? organizationId,
        [FromQuery] string? projectId,
        [FromQuery] string? applicationId,
        SendMessageRequest request,
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

        var validationError = GraphInput.ValidateMessageContent(request.Content);
        if (validationError is not null)
        {
            return UnprocessableEntity(new { code = "invalid_message", message = validationError });
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

        var result = await teamsMessageService.SendAsync(configuration, request.Content!, cancellationToken);
        return Created(result.WebUrl ?? string.Empty, result);
    }
}
