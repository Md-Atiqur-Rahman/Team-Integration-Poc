using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TeamsIntegration.Api.Models;
using TeamsIntegration.Api.Services;

namespace TeamsIntegration.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/teams")]
public sealed class TeamsController(ITeamsGraphService teamsGraphService) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType<TeamsResponse>(StatusCodes.Status200OK)]
    public async Task<ActionResult<TeamsResponse>> GetTeams(CancellationToken cancellationToken)
    {
        return Ok(new TeamsResponse(await teamsGraphService.GetTeamsAsync(cancellationToken)));
    }

    [HttpGet("{teamId}/channels")]
    [ProducesResponseType<ChannelsResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ChannelsResponse>> GetChannels(
        [FromRoute] string teamId,
        CancellationToken cancellationToken)
    {
        if (!GraphInput.IsValidIdentifier(teamId))
        {
            return BadRequest(new { code = "invalid_team_id", message = "Team ID is invalid." });
        }

        return Ok(new ChannelsResponse(await teamsGraphService.GetChannelsAsync(teamId, cancellationToken)));
    }

    [HttpPost("messages")]
    [ProducesResponseType<SendChannelMessageResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<ActionResult<SendChannelMessageResponse>> SendMessage(
        SendChannelMessageRequest request,
        CancellationToken cancellationToken)
    {
        var validationError = GraphInput.Validate(request);
        if (validationError is not null)
        {
            return UnprocessableEntity(new { code = "invalid_message", message = validationError });
        }

        var result = await teamsGraphService.SendMessageAsync(request, cancellationToken);
        return Created(result.WebUrl ?? string.Empty, result);
    }
}
