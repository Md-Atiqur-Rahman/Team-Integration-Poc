using TeamsIntegration.Api.Models;
using TeamsIntegration.Api.Services;

namespace TeamsIntegration.Api.Tests;

public sealed class FakeTeamsGraphService : ITeamsGraphService
{
    public IReadOnlyList<TeamItem> Teams { get; set; } = [];

    public IReadOnlyList<ChannelItem> Channels { get; set; } = [];

    public SendChannelMessageResponse? SendMessageResponse { get; set; }

    public Exception? ExceptionToThrow { get; set; }

    public int GetTeamsCallCount { get; private set; }

    public Task<IReadOnlyList<TeamItem>> GetTeamsAsync(CancellationToken cancellationToken)
    {
        GetTeamsCallCount++;
        return ExceptionToThrow is not null
            ? Task.FromException<IReadOnlyList<TeamItem>>(ExceptionToThrow)
            : Task.FromResult(Teams);
    }

    public Task<IReadOnlyList<ChannelItem>> GetChannelsAsync(string teamId, CancellationToken cancellationToken) =>
        ExceptionToThrow is not null
            ? Task.FromException<IReadOnlyList<ChannelItem>>(ExceptionToThrow)
            : Task.FromResult(Channels);

    public Task<SendChannelMessageResponse> SendMessageAsync(
        SendChannelMessageRequest request,
        CancellationToken cancellationToken) =>
        ExceptionToThrow is not null
            ? Task.FromException<SendChannelMessageResponse>(ExceptionToThrow)
            : Task.FromResult(SendMessageResponse ?? throw new NotSupportedException(
                "Set SendMessageResponse before calling SendMessageAsync in a test."));
}
