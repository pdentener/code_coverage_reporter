using System.ComponentModel;
using System.Globalization;
using CodeCoverageReporter.CLI.Infrastructure;
using CodeCoverageReporter.Cobertura;
using CodeCoverageReporter.Cobertura.Exporting;
using CodeCoverageReporter.Cobertura.IO;
using CodeCoverageReporter.Cobertura.Merging;
using CodeCoverageReporter.Cobertura.Models;
using CodeCoverageReporter.Cobertura.Parsing;
using CodeCoverageReporter.Cobertura.Paths;
using CodeCoverageReporter.Cobertura.Reporting;
using Microsoft.Extensions.FileSystemGlobbing;
using Spectre.Console.Cli;

namespace CodeCoverageReporter.CLI.Commands;

/// <summary>
/// Command for generating missing coverage reports from Cobertura XML files.
/// </summary>
[Description("Generate a missing coverage report from Cobertura XML files.")]
internal sealed class ReportCommand : Command<ReportCommand.Settings>
{
    private readonly IConsoleService _consoleService;
    private readonly ICoverageFileReader _fileReader;
    private readonly ICoberturaParser _parser;
    private readonly ICoverageMerger _merger;
    private readonly IMissingCoverageExtractor _extractor;
    private readonly TableExporter _tableExporter;
    private readonly JsonExporter _jsonExporter;
    private readonly MarkdownExporter _markdownExporter;

    internal sealed class Settings : CommandSettings
    {
        [CommandArgument(0, "[files]")]
        [Description("Cobertura XML file paths or glob patterns.")]
        public string[]? Files { get; init; }

        [CommandOption("--limit")]
        [Description("Maximum number of rows to output.")]
        public int? Limit { get; init; }

        [CommandOption("--output")]
        [Description("Output format: table (default), json, or markdown.")]
        [DefaultValue("table")]
        public string Output { get; init; } = "table";

        [CommandOption("--verbose")]
        [Description("Show verbose processing information.")]
        public bool Verbose { get; init; }

        [CommandOption("--absolute-paths")]
        [Description("Show full absolute file paths instead of relative paths.")]
        public bool AbsolutePaths { get; init; }

        [CommandOption("--base-path")]
        [Description("Base directory for calculating relative paths (defaults to current directory).")]
        public string? BasePath { get; init; }

        [CommandOption("--exclude")]
        [Description("Glob patterns to exclude files (can be specified multiple times).")]
        public string[]? Exclude { get; init; }
    }

    public ReportCommand(
        IConsoleService consoleService,
        ICoverageFileReader fileReader,
        ICoberturaParser parser,
        ICoverageMerger merger,
        IMissingCoverageExtractor extractor,
        TableExporter tableExporter,
        JsonExporter jsonExporter,
        MarkdownExporter markdownExporter)
    {
        _consoleService = consoleService;
        _fileReader = fileReader;
        _parser = parser;
        _merger = merger;
        _extractor = extractor;
        _tableExporter = tableExporter;
        _jsonExporter = jsonExporter;
        _markdownExporter = markdownExporter;
    }

    public override int Execute(CommandContext context, Settings settings)
    {
        try
        {
            return ExecuteInternal(settings);
        }
        catch (CoberturaException ex)
        {
            _consoleService.WriteLine($"Error: {ex.Message}");
            return 1;
        }
    }

    private int ExecuteInternal(Settings settings)
    {
        // 1. Validate files argument
        if (settings.Files is null || settings.Files.Length == 0)
        {
            _consoleService.WriteLine("Error: No files specified.");
            return 1;
        }

        // 2. Validate path options
        if (settings.AbsolutePaths && !string.IsNullOrEmpty(settings.BasePath))
        {
            _consoleService.WriteLine("Error: Cannot specify both --absolute-paths and --base-path.");
            return 1;
        }

        if (!string.IsNullOrEmpty(settings.BasePath) && !Directory.Exists(settings.BasePath))
        {
            _consoleService.WriteLine($"Error: Base path directory does not exist: {settings.BasePath}");
            return 1;
        }

        // 3. Create path transformer
        var pathTransformer = CreatePathTransformer(settings);

        // 4. Resolve file paths
        var resolvedFiles = _fileReader.ResolveFiles(settings.Files);

        // 5. Verbose: show files being processed
        if (settings.Verbose)
        {
            WriteVerbose($"Processing {resolvedFiles.Count} file(s):");
            foreach (var file in resolvedFiles)
            {
                WriteVerbose($"  {file}");
            }
        }

        // 6. Parse each file
        var reports = new List<CoverageReport>();
        foreach (var filePath in resolvedFiles)
        {
            try
            {
                using var stream = _fileReader.OpenFile(filePath);
                var report = _parser.Parse(stream);
                reports.Add(report);
            }
            catch (CoberturaException)
            {
                throw;
            }
            catch (Exception ex)
            {
                throw new CoberturaException($"Failed to parse '{filePath}': {ex.Message}", ex);
            }
        }

        // 7. Merge reports
        var mergedReport = _merger.Merge(reports);

        if (settings.Verbose)
        {
            WriteVerbose($"Merged {reports.Count} report(s).");
        }

        // 8. Extract missing coverage rows
        var rows = _extractor.Extract(mergedReport);

        // 9. Apply exclude filters if specified
        if (settings.Exclude is { Length: > 0 })
        {
            rows = FilterExcludedFiles(rows, settings.Exclude);

            if (settings.Verbose)
            {
                WriteVerbose(string.Create(CultureInfo.InvariantCulture, $"After exclusion filter: {rows.Count} row(s) remaining."));
            }
        }

        // 10. If no rows: output "Code coverage OK"
        if (rows.Count == 0)
        {
            _consoleService.WriteLine("Code coverage OK");
            return 0;
        }

        // 11. Verbose: show limit info
        if (settings.Verbose && settings.Limit.HasValue && settings.Limit.Value < rows.Count)
        {
            WriteVerbose(string.Create(CultureInfo.InvariantCulture, $"Showing first {settings.Limit.Value} of {rows.Count} rows."));
        }

        // 12. Select exporter and export
        var exporter = SelectExporter(settings.Output);
        var output = exporter.Export(rows, settings.Limit, pathTransformer);

        // 13. Output result to stdout
        _consoleService.WriteLine(output);

        return 0;
    }

    private ICoverageExporter SelectExporter(string outputFormat)
    {
        return outputFormat.ToUpperInvariant() switch
        {
            "JSON" => _jsonExporter,
            "MARKDOWN" => _markdownExporter,
            _ => _tableExporter
        };
    }

    private static IPathTransformer CreatePathTransformer(Settings settings)
    {
        if (settings.AbsolutePaths)
        {
            return NullPathTransformer.Instance;
        }

        if (!string.IsNullOrEmpty(settings.BasePath))
        {
            return new PathTransformer(Path.GetFullPath(settings.BasePath));
        }

        return new PathTransformer(Environment.CurrentDirectory);
    }

    private static List<MissingCoverageRow> FilterExcludedFiles(
        IReadOnlyList<MissingCoverageRow> rows,
        string[] excludePatterns)
    {
        var matcher = new Matcher();
        foreach (var pattern in excludePatterns)
        {
            matcher.AddInclude(pattern);
        }

        return rows.Where(row => !IsFileExcluded(row.File, matcher)).ToList();
    }

    private static bool IsFileExcluded(string filePath, Matcher matcher)
    {
        // Normalize path separators for cross-platform matching
        var normalizedPath = filePath.Replace('\\', '/');

        // Match against the full path
        var result = matcher.Match(normalizedPath);
        if (result.HasMatches)
        {
            return true;
        }

        // Also try matching against just the filename for simple patterns
        var fileName = Path.GetFileName(filePath);
        result = matcher.Match(fileName);
        return result.HasMatches;
    }

    private static void WriteVerbose(string message)
    {
        Console.Error.WriteLine(message);
    }
}
