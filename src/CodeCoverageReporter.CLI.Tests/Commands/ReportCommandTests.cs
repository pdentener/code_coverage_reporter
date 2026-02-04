using CodeCoverageReporter.CLI.Commands;
using CodeCoverageReporter.CLI.Infrastructure;
using CodeCoverageReporter.Cobertura;
using CodeCoverageReporter.Cobertura.Exporting;
using CodeCoverageReporter.Cobertura.IO;
using CodeCoverageReporter.Cobertura.Merging;
using CodeCoverageReporter.Cobertura.Models;
using CodeCoverageReporter.Cobertura.Parsing;
using CodeCoverageReporter.Cobertura.Reporting;
using NSubstitute;

namespace CodeCoverageReporter.CLI.Tests.Commands;

public sealed class ReportCommandTests
{
    private readonly IConsoleService _consoleService;
    private readonly ICoverageFileReader _fileReader;
    private readonly ICoberturaParser _parser;
    private readonly ICoverageMerger _merger;
    private readonly IMissingCoverageExtractor _extractor;
    private readonly TableExporter _tableExporter;
    private readonly JsonExporter _jsonExporter;
    private readonly MarkdownExporter _markdownExporter;
    private readonly ReportCommand _command;

    public ReportCommandTests()
    {
        _consoleService = Substitute.For<IConsoleService>();
        _fileReader = Substitute.For<ICoverageFileReader>();
        _parser = Substitute.For<ICoberturaParser>();
        _merger = Substitute.For<ICoverageMerger>();
        _extractor = Substitute.For<IMissingCoverageExtractor>();
        _tableExporter = new TableExporter();
        _jsonExporter = new JsonExporter();
        _markdownExporter = new MarkdownExporter();
        _command = new ReportCommand(
            _consoleService,
            _fileReader,
            _parser,
            _merger,
            _extractor,
            _tableExporter,
            _jsonExporter,
            _markdownExporter);
    }

    [Fact]
    public void Execute_NoFilesArgument_ReturnsErrorCode()
    {
        // Arrange
        var settings = new ReportCommand.Settings { Files = null };

        // Act
        var result = _command.Execute(null!, settings);

        // Assert
        Assert.Equal(1, result);
        _consoleService.Received(1).WriteLine("Error: No files specified.");
    }

    [Fact]
    public void Execute_EmptyFilesArgument_ReturnsErrorCode()
    {
        // Arrange
        var settings = new ReportCommand.Settings { Files = [] };

        // Act
        var result = _command.Execute(null!, settings);

        // Assert
        Assert.Equal(1, result);
        _consoleService.Received(1).WriteLine("Error: No files specified.");
    }

    [Fact]
    public void Execute_ValidFile_ProcessesAndOutputsReport()
    {
        // Arrange
        var settings = new ReportCommand.Settings { Files = ["coverage.xml"] };
        SetupSuccessfulPipeline(
        [
            new MissingCoverageRow("File.cs", "MyClass", "MyMethod", [10], 0, null, null)
        ]);

        // Act
        var result = _command.Execute(null!, settings);

        // Assert
        Assert.Equal(0, result);
        _consoleService.Received(1).WriteLine(Arg.Is<string>(s => s.Contains("File.cs", StringComparison.Ordinal)));
    }

    [Fact]
    public void Execute_NoMissingCoverage_OutputsOkMessage()
    {
        // Arrange
        var settings = new ReportCommand.Settings { Files = ["coverage.xml"] };
        SetupSuccessfulPipeline([]);

        // Act
        var result = _command.Execute(null!, settings);

        // Assert
        Assert.Equal(0, result);
        _consoleService.Received(1).WriteLine("Code coverage OK");
    }

    [Fact]
    public void Execute_WithLimitFlag_LimitsOutput()
    {
        // Arrange
        var settings = new ReportCommand.Settings { Files = ["coverage.xml"], Limit = 1 };
        SetupSuccessfulPipeline(
        [
            new MissingCoverageRow("File1.cs", "Class1", "Method1", [10], 0, null, null),
            new MissingCoverageRow("File2.cs", "Class2", "Method2", [20], 0, null, null)
        ]);

        // Act
        var result = _command.Execute(null!, settings);

        // Assert
        Assert.Equal(0, result);
        _consoleService.Received(1).WriteLine(Arg.Is<string>(s =>
            s.Contains("File1.cs", StringComparison.Ordinal) && !s.Contains("File2.cs", StringComparison.Ordinal)));
    }

    [Fact]
    public void Execute_OutputJson_ProducesJsonOutput()
    {
        // Arrange
        var settings = new ReportCommand.Settings { Files = ["coverage.xml"], Output = "json" };
        SetupSuccessfulPipeline(
        [
            new MissingCoverageRow("File.cs", "MyClass", "MyMethod", [10], 0, null, null)
        ]);

        // Act
        var result = _command.Execute(null!, settings);

        // Assert
        Assert.Equal(0, result);
        _consoleService.Received(1).WriteLine(Arg.Is<string>(s =>
            s.StartsWith('[') && s.Contains("\"file\"", StringComparison.Ordinal)));
    }

    [Fact]
    public void Execute_OutputTable_ProducesTableOutput()
    {
        // Arrange
        var settings = new ReportCommand.Settings { Files = ["coverage.xml"], Output = "table" };
        SetupSuccessfulPipeline(
        [
            new MissingCoverageRow("File.cs", "MyClass", "MyMethod", [10], 0, null, null)
        ]);

        // Act
        var result = _command.Execute(null!, settings);

        // Assert
        Assert.Equal(0, result);
        _consoleService.Received(1).WriteLine(Arg.Is<string>(s =>
            s.StartsWith("File|Class|Method", StringComparison.Ordinal)));
    }

    [Fact]
    public void Execute_OutputMarkdown_ProducesMarkdownOutput()
    {
        // Arrange
        var settings = new ReportCommand.Settings { Files = ["coverage.xml"], Output = "markdown" };
        SetupSuccessfulPipeline(
        [
            new MissingCoverageRow("File.cs", "MyClass", "MyMethod", [10], 0, null, null)
        ]);

        // Act
        var result = _command.Execute(null!, settings);

        // Assert
        Assert.Equal(0, result);
        _consoleService.Received(1).WriteLine(Arg.Is<string>(s =>
            s.StartsWith("| File | Class | Method", StringComparison.Ordinal) &&
            s.Contains("|------|", StringComparison.Ordinal)));
    }

    [Fact]
    public void Execute_CoberturaException_ReturnsErrorCode()
    {
        // Arrange
        var settings = new ReportCommand.Settings { Files = ["coverage.xml"] };
        _fileReader.ResolveFiles(Arg.Any<IEnumerable<string>>())
            .Returns(_ => throw new CoberturaException("File not found: coverage.xml"));

        // Act
        var result = _command.Execute(null!, settings);

        // Assert
        Assert.Equal(1, result);
        _consoleService.Received(1).WriteLine("Error: File not found: coverage.xml");
    }

    [Fact]
    public void Execute_MultipleFiles_MergesReports()
    {
        // Arrange
        var settings = new ReportCommand.Settings { Files = ["coverage1.xml", "coverage2.xml"] };
        var resolvedFiles = new[] { "/path/coverage1.xml", "/path/coverage2.xml" };
        _fileReader.ResolveFiles(Arg.Any<IEnumerable<string>>()).Returns(resolvedFiles);
        _fileReader.OpenFile(Arg.Any<string>()).Returns(new MemoryStream());

        var report1 = CreateEmptyReport();
        var report2 = CreateEmptyReport();
        var mergedReport = CreateEmptyReport();

        _parser.Parse(Arg.Any<Stream>()).Returns(report1, report2);
        _merger.Merge(Arg.Any<IEnumerable<CoverageReport>>()).Returns(mergedReport);
        _extractor.Extract(mergedReport).Returns([]);

        // Act
        var result = _command.Execute(null!, settings);

        // Assert
        Assert.Equal(0, result);
        _merger.Received(1).Merge(Arg.Is<IEnumerable<CoverageReport>>(x => x.Count() == 2));
    }

    [Fact]
    public void Execute_ParsingError_ReturnsErrorWithFilePath()
    {
        // Arrange
        var settings = new ReportCommand.Settings { Files = ["coverage.xml"] };
        var resolvedFiles = new[] { "/path/coverage.xml" };
        _fileReader.ResolveFiles(Arg.Any<IEnumerable<string>>()).Returns(resolvedFiles);
        _fileReader.OpenFile("/path/coverage.xml").Returns(new MemoryStream());
        _parser.Parse(Arg.Any<Stream>()).Returns(_ => throw new InvalidOperationException("Invalid XML"));

        // Act
        var result = _command.Execute(null!, settings);

        // Assert
        Assert.Equal(1, result);
        _consoleService.Received(1).WriteLine(Arg.Is<string>(s =>
            s.Contains("/path/coverage.xml", StringComparison.Ordinal) && s.Contains("Invalid XML", StringComparison.Ordinal)));
    }

    [Fact]
    public void Execute_Default_UsesRelativePaths()
    {
        // Arrange
        // Use a path relative to the current directory for testing
        var currentDir = Environment.CurrentDirectory;
        var absolutePath = Path.Combine(currentDir, "subdir", "File.cs");
        var settings = new ReportCommand.Settings { Files = ["coverage.xml"] };
        SetupSuccessfulPipeline(
        [
            new MissingCoverageRow(absolutePath, "MyClass", "MyMethod", [10], 0, null, null)
        ]);

        // Act
        var result = _command.Execute(null!, settings);

        // Assert
        Assert.Equal(0, result);
        var expectedRelativePath = Path.Combine("subdir", "File.cs");
        _consoleService.Received(1).WriteLine(Arg.Is<string>(s =>
            s.Contains(expectedRelativePath, StringComparison.Ordinal)));
    }

    [Fact]
    public void Execute_AbsolutePathsFlag_UsesAbsolutePaths()
    {
        // Arrange
        var absolutePath = "/absolute/path/to/File.cs";
        var settings = new ReportCommand.Settings { Files = ["coverage.xml"], AbsolutePaths = true };
        SetupSuccessfulPipeline(
        [
            new MissingCoverageRow(absolutePath, "MyClass", "MyMethod", [10], 0, null, null)
        ]);

        // Act
        var result = _command.Execute(null!, settings);

        // Assert
        Assert.Equal(0, result);
        _consoleService.Received(1).WriteLine(Arg.Is<string>(s =>
            s.Contains(absolutePath, StringComparison.Ordinal)));
    }

    [Fact]
    public void Execute_BasePathOption_UsesCustomBasePath()
    {
        // Arrange
        var tempDir = Path.GetTempPath();
        var basePath = Path.Combine(tempDir, "custombase");
        var absolutePath = Path.Combine(basePath, "src", "File.cs");

        // Ensure base path exists for validation
        Directory.CreateDirectory(basePath);
        try
        {
            var settings = new ReportCommand.Settings { Files = ["coverage.xml"], BasePath = basePath };
            SetupSuccessfulPipeline(
            [
                new MissingCoverageRow(absolutePath, "MyClass", "MyMethod", [10], 0, null, null)
            ]);

            // Act
            var result = _command.Execute(null!, settings);

            // Assert
            Assert.Equal(0, result);
            var expectedRelativePath = Path.Combine("src", "File.cs");
            _consoleService.Received(1).WriteLine(Arg.Is<string>(s =>
                s.Contains(expectedRelativePath, StringComparison.Ordinal)));
        }
        finally
        {
            // Cleanup
            Directory.Delete(basePath, recursive: true);
        }
    }

    [Fact]
    public void Execute_BothOptionsSpecified_ReturnsError()
    {
        // Arrange
        var tempDir = Path.GetTempPath();
        var settings = new ReportCommand.Settings
        {
            Files = ["coverage.xml"],
            AbsolutePaths = true,
            BasePath = tempDir
        };

        // Act
        var result = _command.Execute(null!, settings);

        // Assert
        Assert.Equal(1, result);
        _consoleService.Received(1).WriteLine("Error: Cannot specify both --absolute-paths and --base-path.");
    }

    [Fact]
    public void Execute_InvalidBasePath_ReturnsError()
    {
        // Arrange
        var settings = new ReportCommand.Settings
        {
            Files = ["coverage.xml"],
            BasePath = "/nonexistent/path/that/does/not/exist"
        };

        // Act
        var result = _command.Execute(null!, settings);

        // Assert
        Assert.Equal(1, result);
        _consoleService.Received(1).WriteLine(Arg.Is<string>(s =>
            s.Contains("Base path directory does not exist", StringComparison.Ordinal)));
    }

    [Fact]
    public void Execute_ExcludeSinglePattern_FiltersMatchingFiles()
    {
        // Arrange
        var settings = new ReportCommand.Settings
        {
            Files = ["coverage.xml"],
            Exclude = ["**/*.g.cs"]
        };
        SetupSuccessfulPipeline(
        [
            new MissingCoverageRow("src/File.cs", "MyClass", "MyMethod", [10], 0, null, null),
            new MissingCoverageRow("src/File.g.cs", "Generated", "Method", [20], 0, null, null)
        ]);

        // Act
        var result = _command.Execute(null!, settings);

        // Assert
        Assert.Equal(0, result);
        _consoleService.Received(1).WriteLine(Arg.Is<string>(s =>
            s.Contains("src/File.cs", StringComparison.Ordinal) &&
            !s.Contains("src/File.g.cs", StringComparison.Ordinal)));
    }

    [Fact]
    public void Execute_ExcludeMultiplePatterns_FiltersAllMatchingFiles()
    {
        // Arrange
        var settings = new ReportCommand.Settings
        {
            Files = ["coverage.xml"],
            Exclude = ["**/*.g.cs", "**/Migrations/*"]
        };
        SetupSuccessfulPipeline(
        [
            new MissingCoverageRow("src/File.cs", "MyClass", "MyMethod", [10], 0, null, null),
            new MissingCoverageRow("src/File.g.cs", "Generated", "Method", [20], 0, null, null),
            new MissingCoverageRow("src/Migrations/Migration.cs", "Migration", "Up", [30], 0, null, null)
        ]);

        // Act
        var result = _command.Execute(null!, settings);

        // Assert
        Assert.Equal(0, result);
        _consoleService.Received(1).WriteLine(Arg.Is<string>(s =>
            s.Contains("src/File.cs", StringComparison.Ordinal) &&
            !s.Contains("src/File.g.cs", StringComparison.Ordinal) &&
            !s.Contains("Migrations", StringComparison.Ordinal)));
    }

    [Fact]
    public void Execute_ExcludeNoMatches_ReturnsAllRows()
    {
        // Arrange
        var settings = new ReportCommand.Settings
        {
            Files = ["coverage.xml"],
            Exclude = ["**/*.xyz"]
        };
        SetupSuccessfulPipeline(
        [
            new MissingCoverageRow("src/File.cs", "MyClass", "MyMethod", [10], 0, null, null),
            new MissingCoverageRow("src/Another.cs", "Another", "Method", [20], 0, null, null)
        ]);

        // Act
        var result = _command.Execute(null!, settings);

        // Assert
        Assert.Equal(0, result);
        _consoleService.Received(1).WriteLine(Arg.Is<string>(s =>
            s.Contains("src/File.cs", StringComparison.Ordinal) &&
            s.Contains("src/Another.cs", StringComparison.Ordinal)));
    }

    [Fact]
    public void Execute_ExcludeAllFiles_OutputsCodeCoverageOk()
    {
        // Arrange
        var settings = new ReportCommand.Settings
        {
            Files = ["coverage.xml"],
            Exclude = ["**/*.cs"]
        };
        SetupSuccessfulPipeline(
        [
            new MissingCoverageRow("src/File.cs", "MyClass", "MyMethod", [10], 0, null, null),
            new MissingCoverageRow("src/Another.cs", "Another", "Method", [20], 0, null, null)
        ]);

        // Act
        var result = _command.Execute(null!, settings);

        // Assert
        Assert.Equal(0, result);
        _consoleService.Received(1).WriteLine("Code coverage OK");
    }

    private void SetupSuccessfulPipeline(IReadOnlyList<MissingCoverageRow> rows)
    {
        var resolvedFiles = new[] { "/path/coverage.xml" };
        _fileReader.ResolveFiles(Arg.Any<IEnumerable<string>>()).Returns(resolvedFiles);
        _fileReader.OpenFile(Arg.Any<string>()).Returns(new MemoryStream());

        var report = CreateEmptyReport();
        _parser.Parse(Arg.Any<Stream>()).Returns(report);
        _merger.Merge(Arg.Any<IEnumerable<CoverageReport>>()).Returns(report);
        _extractor.Extract(report).Returns(rows);
    }

    private static CoverageReport CreateEmptyReport()
    {
        return new CoverageReport([], [], 0, 0, 0, 0, "1.0", 0, 0, 0, 0, 0, 0);
    }
}
