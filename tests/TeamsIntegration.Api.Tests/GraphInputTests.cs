using TeamsIntegration.Api.Models;

namespace TeamsIntegration.Api.Tests;

public sealed class GraphInputTests
{
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void ValidateMessageContent_rejects_empty_content(string? content)
    {
        var result = GraphInput.ValidateMessageContent(content);

        Assert.Equal("Message content is required.", result);
    }

    [Fact]
    public void ValidateMessageContent_rejects_content_over_limit()
    {
        var result = GraphInput.ValidateMessageContent(new string('a', GraphInput.MaximumMessageLength + 1));

        Assert.Equal("Message content must not exceed 4000 characters.", result);
    }

    [Fact]
    public void ValidateMessageContent_accepts_a_bounded_message()
    {
        var result = GraphInput.ValidateMessageContent("POC message");

        Assert.Null(result);
    }
}
