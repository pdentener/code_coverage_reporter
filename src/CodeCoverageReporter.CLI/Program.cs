using System.Diagnostics.CodeAnalysis;
using CodeCoverageReporter.CLI.Commands;
using CodeCoverageReporter.CLI.Infrastructure;
using CodeCoverageReporter.Cobertura.ExtensionMethods;
using Microsoft.Extensions.DependencyInjection;
using Spectre.Console;
using Spectre.Console.Cli;

[assembly: ExcludeFromCodeCoverage]

var services = new ServiceCollection();
services.AddSingleton<IAnsiConsole>(AnsiConsole.Console);
services.AddSingleton<IConsoleService, ConsoleService>();
services.AddCobertura();

var registrar = new TypeRegistrar(services);
var app = new CommandApp<DefaultCommand>(registrar);

app.Configure(config =>
{
    config.SetApplicationName("cover");
    config.AddCommand<ReportCommand>("report")
        .WithDescription("Generate a code coverage report.");
});

return app.Run(args);
