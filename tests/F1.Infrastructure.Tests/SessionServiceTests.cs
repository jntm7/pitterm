using F1.Core.Services;
using F1.Infrastructure.Services;
using Xunit;

namespace F1.Infrastructure.Tests;

public sealed class SessionServiceTests
{
    [Fact]
    public async Task GetSessionsByRaceAsync_WithoutClient_ReturnsNoRows()
    {
        ISessionService service = new SessionService();

        var sessions = await service.GetSessionsByRaceAsync(2025, 1);

        Assert.Empty(sessions);
    }
}
