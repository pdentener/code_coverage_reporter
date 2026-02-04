using Spectre.Console;

namespace CodeCoverageReporter.CLI.Infrastructure;

/// <summary>
/// Abstraction for console output operations to enable testability.
/// </summary>
internal interface IConsoleService
{
    void WriteFiglet(string text, Color color);
    void WriteMarkup(string markup);
    void WriteLine(string text);
}
