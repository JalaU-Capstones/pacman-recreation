# Testing Guide

This document describes the testing strategy and how to run tests.

## Test Coverage Goals

- Minimum 70% code coverage for production code
- 100% coverage for critical paths (game logic, multiplayer, data persistence)
- All public APIs tested
- Integration tests for key workflows

## Test Categories

### Unit Tests

**Client Tests (`tests/PacmanGame.Tests/`):**
- Game engine logic (collision, movement, scoring)
- Ghost AI (pathfinding, behavior modes)
- ViewModels (navigation, commands, state management)
- Services (audio, sprites, profiles, networking)

**Server Tests (`tests/PacmanGame.Server.Tests/`):**
- Relay server (room management, message handling)
- Game simulation (entity movement, collision, state updates)
- Room manager (player management, role assignment)

### Integration Tests

- Complete multiplayer flows (create → join → play → leave)
- Client-server communication
- Database persistence
- Cross-platform compatibility

## Running Tests

### Run All Tests

```bash
dotnet test
```

### Run Specific Test Project

```bash
dotnet test tests/PacmanGame.Tests
dotnet test tests/PacmanGame.Server.Tests
```

### Run Specific Test Class

```bash
dotnet test --filter FullyQualifiedName~GameEngineTests
```

### Generate Coverage Report

```bash
dotnet test /p:CollectCoverage=true /p:CoverletOutputFormat=opencover
```

## Test Structure

### Naming Conventions

- Test files: `{ClassUnderTest}Tests.cs`
- Test methods: `{MethodUnderTest}_{Scenario}_{ExpectedResult}`

Example:
```csharp
[Fact]
public void CreateRoom_WithDuplicateName_Fails()
```

### Arrange-Act-Assert Pattern

All tests follow the AAA pattern:

```csharp
[Fact]
public void Example_Test()
{
    // Arrange - Set up test data and mocks
    var service = new MyService();
    var input = "test";
    
    // Act - Execute the method under test
    var result = service.Process(input);
    
    // Assert - Verify the result
    Assert.Equal("expected", result);
}
```

## Mocking

We use Moq for mocking dependencies:

```csharp
var mockLogger = new Mock<ILogger<MyClass>>();
var mockService = new Mock<IMyService>();
mockService.Setup(s => s.DoSomething()).Returns(true);
```

## CI/CD Integration

Tests run automatically on:
- Every push to main branch
- Every pull request
- Before creating releases

Failed tests block merges and deployments.

## Writing New Tests

When adding new functionality:
1. Write tests first (TDD approach recommended)
2. Ensure tests cover happy path and error cases
3. Mock external dependencies
4. Keep tests fast (< 100ms per test)
5. Make tests deterministic (no random behavior)

## Test Data

Test data files are located in:
- `tests/TestData/` - Shared test resources
- Each test project can have its own `TestData/` folder

## Known Limitations

- Audio tests are limited (SFML requires audio device)
- Rendering tests are not implemented (Avalonia UI tests complex)
- Network tests require mock server or local server instance
