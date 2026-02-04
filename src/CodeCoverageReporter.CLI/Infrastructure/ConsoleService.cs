using Spectre.Console;

namespace CodeCoverageReporter.CLI.Infrastructure;

/// <summary>
/// Implementation of console service using Spectre.Console.
/// </summary>
internal sealed class ConsoleService : IConsoleService
{
    private readonly IAnsiConsole _console;

    public ConsoleService(IAnsiConsole console)
    {
        _console = console;
    }

    public void WriteFiglet(string text, Color color)
    {
        var figlet = new FigletText(text).Color(color);
        _console.Write(figlet);
    }

    public void WriteMarkup(string markup)
    {
        _console.MarkupLine(markup);
    }

    public void WriteLine(string text)
    {
        Console.Out.WriteLine(text);
    }
}
