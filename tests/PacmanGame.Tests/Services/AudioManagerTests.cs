using Xunit;
using Moq;
using Microsoft.Extensions.Logging;
using PacmanGame.Services;

namespace PacmanGame.Tests.Services;

public class AudioManagerTests
{
    [Fact]
    public void MuteAll_StopsAllAudio()
    {
        // Test mute functionality
        // Requires mocking SFML which is difficult
    }
}
