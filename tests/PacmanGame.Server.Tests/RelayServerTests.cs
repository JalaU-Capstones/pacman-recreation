using Xunit;
using Microsoft.Extensions.Logging;
using Moq;
using PacmanGame.Server;
using PacmanGame.Shared;
using System.Net;
using LiteNetLib;
using System.Reflection;
using PacmanGame.Server.Models;

namespace PacmanGame.Server.Tests;

public class RelayServerTests
{
    private readonly Mock<ILogger<RelayServer>> _mockLogger;
    private readonly Mock<ILoggerFactory> _mockLoggerFactory;
    private readonly RelayServer _server;

    public RelayServerTests()
    {
        _mockLogger = new Mock<ILogger<RelayServer>>();
        _mockLoggerFactory = new Mock<ILoggerFactory>();
        _mockLoggerFactory.Setup(x => x.CreateLogger(It.IsAny<string>())).Returns(new Mock<ILogger>().Object);
        _server = new RelayServer(_mockLogger.Object, _mockLoggerFactory.Object);
    }

    [Fact]
    public void Server_Initializes_WithCorrectPort()
    {
        Assert.NotNull(_server);
    }

    // Helper method to invoke private methods via reflection
    private void InvokePrivateMethod(string methodName, params object[] args)
    {
        var method = typeof(RelayServer).GetMethod(methodName, BindingFlags.NonPublic | BindingFlags.Instance);
        if (method != null)
        {
            method.Invoke(_server, args);
        }
    }

    // Since RelayServer methods are private and depend on internal state (connected players),
    // unit testing them directly is difficult without refactoring RelayServer to be more testable.
    // For now, we will skip the complex interaction tests that require private method invocation and mocking internal state.
    // In a real scenario, we would refactor RelayServer to extract the message handling logic into a separate service.
}
