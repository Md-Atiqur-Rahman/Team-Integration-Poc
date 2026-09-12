using TeamsIntegration.Api.Models;
using TeamsIntegration.Api.Services;

namespace TeamsIntegration.Api.Tests;

public sealed class FakeTeamsGraphService : ITeamsGraphService
{
    public IReadOnlyList<TeamItem> Teams { get; set; } = [];

    public IReadOnlyList<ChannelItem> Channels { get; set; } = [];

    public Task<IReadOnlyList<TeamItem>> GetTeamsAsync(CancellationToken cancellationToken) =>
        Task.FromResult(Teams);

    public Task<IReadOnlyList<ChannelItem>> GetChannelsAsync(string teamId, CancellationToken cancellationToken) =>
        Task.FromResult(Channels);

    public Task<SendChannelMessageResponse> SendMessageAsync(
        SendChannelMessageRequest request,
        CancellationToken cancellationToken) =>
        throw new NotSupportedException("Not used by configuration revalidation tests.");
}
