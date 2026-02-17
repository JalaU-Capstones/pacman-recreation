using Xunit;
using Moq;
using Microsoft.Extensions.Logging;
using PacmanGame.Services;
using PacmanGame.Shared;

namespace PacmanGame.Tests.Services;

public class NetworkServiceTests
{
    private readonly Mock<ILogger<NetworkService>> _mockLogger;
    private readonly NetworkService _networkService;

    public NetworkServiceTests()
    {
        _mockLogger = new Mock<ILogger<NetworkService>>();
        _networkService = new NetworkService(_mockLogger.Object);
    }

    [Fact]
    public void Disconnect_StopsPollingLoop()
    {
        // Test clean disconnection
        _networkService.Stop();
        Assert.False(_networkService.IsConnected);
    }
}
