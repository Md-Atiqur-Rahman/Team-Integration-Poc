using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Identity.Web;
using TeamsIntegration.Api.Models;

namespace TeamsIntegration.Api.Services;

public interface ITeamsGraphService
{
    Task<IReadOnlyList<TeamItem>> GetTeamsAsync(CancellationToken cancellationToken);

    Task<IReadOnlyList<ChannelItem>> GetChannelsAsync(string teamId, CancellationToken cancellationToken);

    Task<SendChannelMessageResponse> SendMessageAsync(
        SendChannelMessageRequest request,
        CancellationToken cancellationToken);
}

public sealed class TeamsGraphService(
    HttpClient httpClient,
    ITokenAcquisition tokenAcquisition) : ITeamsGraphService
{
    public static readonly string[] RequiredScopes =
    [
        "Team.ReadBasic.All",
        "Channel.ReadBasic.All",
        "ChannelMessage.Send"
    ];

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public async Task<IReadOnlyList<TeamItem>> GetTeamsAsync(CancellationToken cancellationToken)
    {
        var response = await SendAsync(HttpMethod.Get, "me/joinedTeams", null, cancellationToken);
        var page = await ReadJsonAsync<GraphPage<GraphTeam>>(response, cancellationToken);

        return page.Value
            .Where(team => !string.IsNullOrWhiteSpace(team.Id))
            .Select(team => new TeamItem(team.Id!, team.DisplayName, team.Description, team.IsArchived))
            .ToArray();
    }

    public async Task<IReadOnlyList<ChannelItem>> GetChannelsAsync(string teamId, CancellationToken cancellationToken)
    {
        var channels = new List<ChannelItem>();
        var relativeUrl = $"teams/{Uri.EscapeDataString(teamId)}/channels"
            + "?$select=id,displayName,description,membershipType,isArchived";

        while (relativeUrl is not null)
        {
            var response = await SendAsync(HttpMethod.Get, relativeUrl, null, cancellationToken);
            var page = await ReadJsonAsync<GraphPage<GraphChannel>>(response, cancellationToken);

            channels.AddRange(page.Value
                .Where(channel => !string.IsNullOrWhiteSpace(channel.Id))
                .Select(channel => new ChannelItem(
                    channel.Id!,
                    channel.DisplayName,
                    channel.Description,
                    channel.MembershipType,
                    channel.IsArchived)));

            relativeUrl = page.NextLink;
        }

        return channels;
    }

    public async Task<SendChannelMessageResponse> SendMessageAsync(
        SendChannelMessageRequest request,
        CancellationToken cancellationToken)
    {
        var path = $"teams/{Uri.EscapeDataString(request.TeamId!)}/channels/"
            + $"{Uri.EscapeDataString(request.ChannelId!)}/messages";
        var payload = new
        {
            body = new
            {
                contentType = "text",
                content = request.Content
            }
        };

        var response = await SendAsync(HttpMethod.Post, path, payload, cancellationToken);
        if (response.StatusCode != HttpStatusCode.Created)
        {
            throw new TeamsGraphException(
                StatusCodes.Status502BadGateway,
                "Microsoft Graph did not confirm message delivery.");
        }

        var message = await ReadJsonAsync<GraphMessage>(response, cancellationToken);
        return new SendChannelMessageResponse(message.Id!, message.CreatedDateTime, message.WebUrl);
    }

    private async Task<HttpResponseMessage> SendAsync(
        HttpMethod method,
        string path,
        object? payload,
        CancellationToken cancellationToken)
    {
        string token;
        try
        {
            token = await tokenAcquisition.GetAccessTokenForUserAsync(RequiredScopes);
        }
        catch (MicrosoftIdentityWebChallengeUserException)
        {
            throw new TeamsGraphException(
                StatusCodes.Status401Unauthorized,
                "Microsoft Teams access needs reconnecting.");
        }

        using var request = new HttpRequestMessage(method, path);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        if (payload is not null)
        {
            request.Content = JsonContent.Create(payload, options: JsonOptions);
        }

        var response = await httpClient.SendAsync(request, cancellationToken);
        if (response.IsSuccessStatusCode)
        {
            return response;
        }

        var statusCode = (int)response.StatusCode;
        response.Dispose();
        throw new TeamsGraphException(
            statusCode switch
            {
                StatusCodes.Status401Unauthorized => StatusCodes.Status401Unauthorized,
                StatusCodes.Status403Forbidden => StatusCodes.Status403Forbidden,
                StatusCodes.Status404NotFound => StatusCodes.Status404NotFound,
                StatusCodes.Status429TooManyRequests => StatusCodes.Status429TooManyRequests,
                _ when statusCode >= 500 => StatusCodes.Status503ServiceUnavailable,
                _ => StatusCodes.Status502BadGateway
            },
            "Microsoft Graph could not complete the request.");
    }

    private static async Task<T> ReadJsonAsync<T>(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        using (response)
        {
            var result = await response.Content.ReadFromJsonAsync<T>(JsonOptions, cancellationToken);
            return result ?? throw new TeamsGraphException(
                StatusCodes.Status502BadGateway,
                "Microsoft Graph returned an invalid response.");
        }
    }

    private sealed class GraphPage<T>
    {
        public List<T> Value { get; init; } = [];

        [JsonPropertyName("@odata.nextLink")]
        public string? NextLink { get; init; }
    }

    private sealed class GraphTeam
    {
        public string? Id { get; init; }
        public string? DisplayName { get; init; }
        public string? Description { get; init; }
        public bool? IsArchived { get; init; }
    }

    private sealed class GraphChannel
    {
        public string? Id { get; init; }
        public string? DisplayName { get; init; }
        public string? Description { get; init; }
        public string? MembershipType { get; init; }
        public bool? IsArchived { get; init; }
    }

    private sealed class GraphMessage
    {
        public string? Id { get; init; }
        public DateTimeOffset? CreatedDateTime { get; init; }
        public string? WebUrl { get; init; }
    }
}
