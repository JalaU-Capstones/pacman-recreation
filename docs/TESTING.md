# Testing Documentation

## Overview

This project maintains a comprehensive unit test suite to ensure the reliability of core game logic and services. The testing strategy focuses on business logic, algorithms, and data management, with less emphasis on UI components which are better tested manually or via integration tests.

## Testing Stack

- **Framework:** [xUnit](https://xunit.net/) - The industry standard unit testing framework for .NET.
- **Mocking:** [Moq](https://github.com/moq/moq4) - Used to mock dependencies (interfaces) for isolated unit testing.
- **Assertions:** [FluentAssertions](https://fluentassertions.com/) - Provides a fluent syntax for writing readable assertions.
- **Coverage:** [coverlet](https://github.com/coverlet-coverage/coverlet) - Collects code coverage information.

## Test Project Structure

The test project is located in `tests/PacmanGame.Tests/`.

```
tests/PacmanGame.Tests/
├── GameEngineTests.cs          # Tests for the main game loop and state
├── CollisionDetectorTests.cs   # Tests for collision logic
├── MapLoaderTests.cs           # Tests for map parsing
├── AStarPathfinderTests.cs     # Tests for pathfinding algorithm
├── BlinkyAITests.cs            # Tests for Blinky's AI
├── PinkyAITests.cs             # Tests for Pinky's AI
├── InkyAITests.cs              # Tests for Inky's AI
├── ClydeAITests.cs             # Tests for Clyde's AI
├── PacmanTests.cs              # Tests for Pac-Man entity logic
├── GhostTests.cs               # Tests for Ghost entity logic
└── ProfileManagerTests.cs      # Tests for database operations
```

## Running Tests

To run all tests from the command line:

```bash
dotnet test
```

To run tests with coverage reporting:

```bash
dotnet test /p:CollectCoverage=true /p:CoverletOutputFormat=opencover
```

## Coverage Goals

The project aims for **70%+ code coverage** on core business logic.

### Key Areas & Targets

| Component | Target Coverage | Description |
|-----------|----------------|-------------|
| **GameEngine** | 70% | Main game loop, state transitions, level loading |
| **CollisionDetector** | 80% | Entity-entity and entity-wall collisions |
| **MapLoader** | 70% | Parsing map files and spawning entities |
| **Ghost AI** | 60% | Target calculation and movement logic for all ghosts |
| **Pathfinding** | 65% | A* algorithm correctness |
| **Entities** | 60% | State management (Pac-Man, Ghosts) |
| **ProfileManager** | 50% | Database CRUD operations |

## Writing New Tests

When adding new features, follow these guidelines for writing tests:

1. **Arrange-Act-Assert:** Structure tests clearly using the AAA pattern.
2. **Isolation:** Mock all external dependencies (interfaces) to test the unit in isolation.
3. **Naming:** Use descriptive names like `MethodName_State_ExpectedBehavior`.
4. **Edge Cases:** Test boundary conditions and error states, not just the happy path.

Example:

```csharp
[Fact]
public void CalculateScore_ShouldReturnCorrectValue_WhenGhostEaten()
{
    // Arrange
    var scorer = new ScoreCalculator();
    
    // Act
    int score = scorer.CalculateGhostScore(combo: 2);
    
    // Assert
    score.Should().Be(400);
}
```
