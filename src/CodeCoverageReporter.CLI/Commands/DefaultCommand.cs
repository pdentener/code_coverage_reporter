using System.ComponentModel;
using System.Reflection;
using CodeCoverageReporter.CLI.Infrastructure;
using Spectre.Console;
using Spectre.Console.Cli;

namespace CodeCoverageReporter.CLI.Commands;

/// <summary>
/// Default command that displays the ASCII art banner and version when no command is specified.
/// </summary>
[Description("CLI tool for reporting missing code coverage from Cobertura XML reports.")]
internal sealed class DefaultCommand : Command<DefaultCommand.Settings>
{
    private readonly IConsoleService _consoleService;

    internal sealed class Settings : CommandSettings
    {
        [CommandOption("-v|--version")]
        [Description("Display the version number only.")]
        public bool Version { get; init; }
    }

    public DefaultCommand(IConsoleService consoleService)
    {
        _consoleService = consoleService;
    }

    public override int Execute(CommandContext context, Settings settings)
    {
        var version = Assembly.GetEntryAssembly()?.GetName().Version;
        var versionString = version is not null ? $"v{version.Major}.{version.Minor}.{version.Build}" : "v1.0.0";

        if (settings.Version)
        {
            _consoleService.WriteLine(versionString);
            return 0;
        }

        _consoleService.WriteFiglet("Code Coverage Reporter", Color.Green);
        _consoleService.WriteMarkup($"[grey]{versionString}[/]");

        return 0;
    }
}
