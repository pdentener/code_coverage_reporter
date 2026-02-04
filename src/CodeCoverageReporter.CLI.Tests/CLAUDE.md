# CodeCoverageReporter.CLI.Tests

Unit and integration tests for the Code Coverage Reporter CLI.

## Directory Structure

```
├── Commands/
│   ├── DefaultCommandTests.cs    # Unit tests for DefaultCommand
│   └── ReportCommandTests.cs     # Unit tests for ReportCommand
├── Infrastructure/
│   └── ConsoleServiceTests.cs    # Unit tests for ConsoleService
├── IntegrationTests.cs           # Full CLI wiring tests
└── SmokeTests.cs                 # Basic sanity checks
```

## Test Categories

| Category | Purpose | Mocking |
|----------|---------|---------|
| Unit Tests | Test individual classes in isolation | NSubstitute mocks |
| Integration Tests | Test full CLI command execution | TestConsole (real Spectre) |

## Conventions

### Test Naming
`MethodName_Scenario_ExpectedResult` or descriptive `[Fact]` names

### Pattern
AAA (Arrange/Act/Assert) with comments:
```csharp
// Arrange
var mock = Substitute.For<IConsoleService>();
var command = new DefaultCommand(mock);

// Act
var result = command.Execute(null!, settings);

// Assert
Assert.Equal(0, result);
```

### Mocking with NSubstitute
- `Substitute.For<T>()` - create mock
- `mock.Received(1).Method()` - verify call
- `mock.DidNotReceive().Method()` - verify no call
- `Arg.Is<T>(predicate)` - argument matching

### Integration Test Setup
Use `TestConsole` from Spectre.Console.Testing with real DI wiring:
```csharp
var testConsole = new TestConsole();
var services = new ServiceCollection();
services.AddSingleton<IAnsiConsole>(testConsole);
```

## Dependencies

| Package | Purpose |
|---------|---------|
| xunit | Test framework |
| xunit.runner.visualstudio | VS/CLI test discovery |
| Microsoft.NET.Test.Sdk | Test SDK |
| NSubstitute | Mocking framework |
| coverlet.collector | Code coverage |

## Running Tests

```bash
dotnet test                                    # Run all tests
dotnet test --collect:"XPlat Code Coverage"   # With coverage
```
