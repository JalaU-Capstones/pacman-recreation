using Xunit;
using Moq;
using Microsoft.Extensions.Logging;
using PacmanGame.Services;

namespace PacmanGame.Tests.Services;

public class SpriteManagerTests
{
    [Fact]
    public void GetSprite_WithInvalidName_ThrowsException()
    {
        // Test error handling
        // Requires actual sprite files or mocking
    }
}
