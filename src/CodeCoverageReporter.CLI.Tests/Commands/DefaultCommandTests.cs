using CodeCoverageReporter.CLI.Commands;
using CodeCoverageReporter.CLI.Infrastructure;
using NSubstitute;
using Spectre.Console;

namespace CodeCoverageReporter.CLI.Tests.Commands;

public sealed class DefaultCommandTests
{
    [Fact]
    public void Execute_WithoutVersionFlag_ShouldDisplayBannerAndVersion()
    {
        // Arrange
        var consoleService = Substitute.For<IConsoleService>();
        var command = new DefaultCommand(consoleService);
        var settings = new DefaultCommand.Settings { Version = false };

        // Act
        var result = command.Execute(null!, settings);

        // Assert
        Assert.Equal(0, result);
        consoleService.Received(1).WriteFiglet("Code Coverage Reporter", Color.Green);
        consoleService.Received(1).WriteMarkup(Arg.Is<string>(s => s.Contains('v')));
    }

    [Fact]
    public void Execute_WithVersionFlag_ShouldDisplayVersionOnly()
    {
        // Arrange
        var consoleService = Substitute.For<IConsoleService>();
        var command = new DefaultCommand(consoleService);
        var settings = new DefaultCommand.Settings { Version = true };

        // Act
        var result = command.Execute(null!, settings);

        // Assert
        Assert.Equal(0, result);
        consoleService.Received(1).WriteLine(Arg.Is<string>(s => s.StartsWith('v')));
        consoleService.DidNotReceive().WriteFiglet(Arg.Any<string>(), Arg.Any<Color>());
        consoleService.DidNotReceive().WriteMarkup(Arg.Any<string>());
    }

    [Fact]
    public void Execute_ShouldAlwaysReturnZero()
    {
        // Arrange
        var consoleService = Substitute.For<IConsoleService>();
        var command = new DefaultCommand(consoleService);

        // Act & Assert - Test both code paths
        var resultWithoutVersion = command.Execute(null!, new DefaultCommand.Settings { Version = false });
        var resultWithVersion = command.Execute(null!, new DefaultCommand.Settings { Version = true });

        Assert.Equal(0, resultWithoutVersion);
        Assert.Equal(0, resultWithVersion);
    }
}
