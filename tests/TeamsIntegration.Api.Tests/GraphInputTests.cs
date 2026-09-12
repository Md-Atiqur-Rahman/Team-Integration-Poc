using TeamsIntegration.Api.Models;

namespace TeamsIntegration.Api.Tests;

public sealed class GraphInputTests
{
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Validate_rejects_empty_message_content(string? content)
    {
        var result = GraphInput.Validate(new SendChannelMessageRequest("team", "channel", content));

        Assert.Equal("Message content is required.", result);
    }

    [Fact]
    public void Validate_rejects_message_content_over_limit()
    {
        var result = GraphInput.Validate(new SendChannelMessageRequest(
            "team",
            "channel",
            new string('a', GraphInput.MaximumMessageLength + 1)));

        Assert.Equal("Message content must not exceed 4000 characters.", result);
    }

    [Fact]
    public void Validate_accepts_a_bounded_message_with_graph_identifiers()
    {
        var result = GraphInput.Validate(new SendChannelMessageRequest(
            "19:team@thread.tacv2",
            "19:channel@thread.tacv2",
            "POC message"));

        Assert.Null(result);
    }
}
