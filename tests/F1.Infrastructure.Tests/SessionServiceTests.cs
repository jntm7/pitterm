using F1.Core.Services;
using F1.Infrastructure.Services;
using Xunit;

namespace F1.Infrastructure.Tests;

public sealed class SessionServiceTests
{
    [Fact]
    public async Task GetSessionsByRaceAsync_ReturnsExpectedSessionTypes()
    {
        ISessionService service = new SessionService();

        var sessions = await service.GetSessionsByRaceAsync(2025, 1);

        Assert.Equal(5, sessions.Count);
        Assert.Equal("Practice 1", sessions[0].SessionName);
        Assert.Equal("Qualifying", sessions[3].SessionName);
        Assert.Equal("Race", sessions[4].SessionName);
    }
}
