using System.IO;
using CodeCoverageReporter.CLI.Infrastructure;
using Spectre.Console;
using Spectre.Console.Testing;

namespace CodeCoverageReporter.CLI.Tests.Infrastructure;

public sealed class ConsoleServiceTests
{
    [Fact]
    public void WriteFiglet_ShouldWriteFigletTextToConsole()
    {
        // Arrange
        using var testConsole = new TestConsole();
        var service = new ConsoleService(testConsole);

        // Act
        service.WriteFiglet("Test", Color.Green);

        // Assert
        var output = testConsole.Output;
        // Figlet text renders as ASCII art, so we just verify output is not empty
        // and contains typical ASCII art characters
        Assert.False(string.IsNullOrWhiteSpace(output));
        Assert.Contains("_", output, StringComparison.Ordinal);
    }

    [Fact]
    public void WriteMarkup_ShouldWriteMarkupToConsole()
    {
        // Arrange
        using var testConsole = new TestConsole();
        var service = new ConsoleService(testConsole);

        // Act
        service.WriteMarkup("[red]Hello[/]");

        // Assert
        var output = testConsole.Output;
        Assert.Contains("Hello", output, StringComparison.Ordinal);
    }

    [Fact]
    public void WriteLine_ShouldWriteLineToStandardOutput()
    {
        // Arrange
        using var testConsole = new TestConsole();
        var service = new ConsoleService(testConsole);
        using var stringWriter = new StringWriter();
        var originalOut = Console.Out;

        try
        {
            Console.SetOut(stringWriter);

            // Act
            service.WriteLine("Hello World");

            // Assert
            var output = stringWriter.ToString();
            Assert.Contains("Hello World", output, StringComparison.Ordinal);
        }
        finally
        {
            Console.SetOut(originalOut);
        }
    }
}
