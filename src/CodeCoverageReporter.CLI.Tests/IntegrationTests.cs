using CodeCoverageReporter.CLI.Commands;
using CodeCoverageReporter.CLI.Infrastructure;
using CodeCoverageReporter.Cobertura.ExtensionMethods;
using Microsoft.Extensions.DependencyInjection;
using Spectre.Console;
using Spectre.Console.Cli;
using Spectre.Console.Testing;

namespace CodeCoverageReporter.CLI.Tests;

/// <summary>
/// Integration tests that verify the CLI application wiring and command execution.
/// Output verification is handled by unit tests; these tests focus on return codes
/// and ensuring commands execute without exceptions.
/// </summary>
public sealed class IntegrationTests
{
    private static CommandApp<DefaultCommand> CreateApp()
    {
        var testConsole = new TestConsole();
        var services = new ServiceCollection();
        services.AddSingleton<IAnsiConsole>(testConsole);
        services.AddSingleton<IConsoleService, ConsoleService>();
        services.AddCobertura();

        var registrar = new TypeRegistrar(services);
        var app = new CommandApp<DefaultCommand>(registrar);

        app.Configure(config =>
        {
            config.SetApplicationName("cc");
            config.PropagateExceptions();
            config.AddCommand<ReportCommand>("report")
                .WithDescription("Generate a code coverage report.");
        });

        return app;
    }

    [Fact]
    public void NoArguments_ShouldReturnZero()
    {
        // Arrange
        var app = CreateApp();

        // Act
        var result = app.Run([]);

        // Assert
        Assert.Equal(0, result);
    }

    [Fact]
    public void ReportCommand_WithoutFiles_ReturnsError()
    {
        // Arrange
        var app = CreateApp();

        // Act
        var result = app.Run(["report"]);

        // Assert - No files specified returns error code 1
        Assert.Equal(1, result);
    }

    [Fact]
    public void VersionFlag_ShouldReturnZero()
    {
        // Arrange
        var app = CreateApp();

        // Act
        var result = app.Run(["--version"]);

        // Assert
        Assert.Equal(0, result);
    }

    [Fact]
    public void ShortVersionFlag_ShouldReturnZero()
    {
        // Arrange
        var app = CreateApp();

        // Act
        var result = app.Run(["-v"]);

        // Assert
        Assert.Equal(0, result);
    }

    [Fact]
    public void HelpFlag_ShouldReturnZero()
    {
        // Arrange
        var app = CreateApp();

        // Act
        var result = app.Run(["--help"]);

        // Assert
        Assert.Equal(0, result);
    }

    [Fact]
    public void ReportHelpFlag_ShouldReturnZero()
    {
        // Arrange
        var app = CreateApp();

        // Act
        var result = app.Run(["report", "--help"]);

        // Assert
        Assert.Equal(0, result);
    }

    [Fact]
    public void InvalidCommand_ShouldThrowException()
    {
        // Arrange
        var app = CreateApp();

        // Act & Assert - PropagateExceptions causes CommandParseException to be thrown
        Assert.Throws<Spectre.Console.Cli.CommandParseException>(() => app.Run(["nonexistent"]));
    }

    [Fact]
    public void ReportCommand_WithSimpleFile_ReturnsSuccessWithOutput()
    {
        // Arrange
        var app = CreateApp();
        var testFile = GetTestDataPath("simple.xml");

        // Act
        var result = app.Run(["report", testFile]);

        // Assert
        Assert.Equal(0, result);
    }

    [Fact]
    public void ReportCommand_WithBranchFile_ReturnsSuccessWithOutput()
    {
        // Arrange
        var app = CreateApp();
        var testFile = GetTestDataPath("with-branches.xml");

        // Act
        var result = app.Run(["report", testFile]);

        // Assert
        Assert.Equal(0, result);
    }

    [Fact]
    public void ReportCommand_WithFullCoverage_ReturnsOk()
    {
        // Arrange
        var app = CreateApp();
        var testFile = GetTestDataPath("full-coverage.xml");

        // Act
        var result = app.Run(["report", testFile]);

        // Assert
        Assert.Equal(0, result);
    }

    [Fact]
    public void ReportCommand_WithJsonOutput_ReturnsSuccess()
    {
        // Arrange
        var app = CreateApp();
        var testFile = GetTestDataPath("simple.xml");

        // Act
        var result = app.Run(["report", testFile, "--output", "json"]);

        // Assert
        Assert.Equal(0, result);
    }

    [Fact]
    public void ReportCommand_WithMarkdownOutput_ReturnsSuccess()
    {
        // Arrange
        var app = CreateApp();
        var testFile = GetTestDataPath("simple.xml");

        // Act
        var result = app.Run(["report", testFile, "--output", "markdown"]);

        // Assert
        Assert.Equal(0, result);
    }

    [Fact]
    public void ReportCommand_WithLimit_ReturnsSuccess()
    {
        // Arrange
        var app = CreateApp();
        var testFile = GetTestDataPath("simple.xml");

        // Act
        var result = app.Run(["report", testFile, "--limit", "1"]);

        // Assert
        Assert.Equal(0, result);
    }

    [Fact]
    public void ReportCommand_WithVerbose_ReturnsSuccess()
    {
        // Arrange
        var app = CreateApp();
        var testFile = GetTestDataPath("simple.xml");

        // Act
        var result = app.Run(["report", testFile, "--verbose"]);

        // Assert
        Assert.Equal(0, result);
    }

    [Fact]
    public void ReportCommand_WithMissingFile_ReturnsError()
    {
        // Arrange
        var app = CreateApp();

        // Act
        var result = app.Run(["report", "nonexistent.xml"]);

        // Assert
        Assert.Equal(1, result);
    }

    [Fact]
    public void ReportCommand_WithExcludeFlag_ReturnsSuccess()
    {
        // Arrange
        var app = CreateApp();
        var testFile = GetTestDataPath("simple.xml");

        // Act
        var result = app.Run(["report", testFile, "--exclude", "**/*.g.cs"]);

        // Assert
        Assert.Equal(0, result);
    }

    [Fact]
    public void ReportCommand_WithMultipleExcludeFlags_ReturnsSuccess()
    {
        // Arrange
        var app = CreateApp();
        var testFile = GetTestDataPath("simple.xml");

        // Act
        var result = app.Run(["report", testFile, "--exclude", "**/*.g.cs", "--exclude", "**/Migrations/*"]);

        // Assert
        Assert.Equal(0, result);
    }

    private static string GetTestDataPath(string filename)
    {
        return Path.Combine(AppContext.BaseDirectory, "TestData", filename);
    }
}
