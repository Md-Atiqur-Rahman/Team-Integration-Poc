namespace TeamsIntegration.Api.Models;

public sealed record TeamItem(string Id, string? DisplayName, string? Description, bool? IsArchived);

public sealed record ChannelItem(
    string Id,
    string? DisplayName,
    string? Description,
    string? MembershipType,
    bool? IsArchived);

public sealed record TeamsResponse(IReadOnlyList<TeamItem> Items);

public sealed record ChannelsResponse(IReadOnlyList<ChannelItem> Items);

public sealed record SendChannelMessageRequest(string? TeamId, string? ChannelId, string? Content);

public sealed record SendChannelMessageResponse(string Id, DateTimeOffset? CreatedDateTime, string? WebUrl);

public static class GraphInput
{
    public const int MaximumIdentifierLength = 512;
    public const int MaximumMessageLength = 4_000;

    public static bool IsValidIdentifier(string? value) =>
        !string.IsNullOrWhiteSpace(value) && value.Length <= MaximumIdentifierLength;

    public static string? Validate(SendChannelMessageRequest request)
    {
        if (!IsValidIdentifier(request.TeamId))
        {
            return "Team ID is required and must not exceed 512 characters.";
        }

        if (!IsValidIdentifier(request.ChannelId))
        {
            return "Channel ID is required and must not exceed 512 characters.";
        }

        if (string.IsNullOrWhiteSpace(request.Content))
        {
            return "Message content is required.";
        }

        return request.Content.Length > MaximumMessageLength
            ? "Message content must not exceed 4000 characters."
            : null;
    }
}
