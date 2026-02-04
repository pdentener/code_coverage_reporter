using CodeCoverageReporter.Cobertura.Exporting;
using CodeCoverageReporter.Cobertura.Paths;
using CodeCoverageReporter.Cobertura.Reporting;

namespace CodeCoverageReporter.Cobertura.Tests.Exporting;

public sealed class MarkdownExporterTests
{
    private readonly MarkdownExporter _exporter = new();
    private const string ExpectedHeader = "| File | Class | Method | Lines | Hits | Branch Coverage | Branch Conditions |";
    private const string ExpectedSeparator = "|------|-------|--------|-------|------|-----------------|-------------------|";

    [Fact]
    public void Export_NullRows_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => _exporter.Export(null!));
    }

    [Fact]
    public void Export_EmptyRows_ReturnsHeaderAndSeparatorOnly()
    {
        var result = _exporter.Export([]);

        var lines = result.Split(Environment.NewLine);
        Assert.Equal(2, lines.Length);
        Assert.Equal(ExpectedHeader, lines[0]);
        Assert.Equal(ExpectedSeparator, lines[1]);
    }

    [Fact]
    public void Export_SingleNonBranchRow_FormatsCorrectly()
    {
        var rows = new[]
        {
            new MissingCoverageRow("File.cs", "MyClass", "MyMethod", [10, 11, 12], 0, null, null)
        };

        var result = _exporter.Export(rows);

        var lines = result.Split(Environment.NewLine);
        Assert.Equal(3, lines.Length);
        Assert.Equal(ExpectedHeader, lines[0]);
        Assert.Equal(ExpectedSeparator, lines[1]);
        Assert.Equal("| File.cs | MyClass | MyMethod | [10-12] | 0 |  |  |", lines[2]);
    }

    [Fact]
    public void Export_SingleBranchRow_FormatsCorrectly()
    {
        var rows = new[]
        {
            new MissingCoverageRow("File.cs", "MyClass", "MyMethod", [10], 0, "50% (1/2)", "[0:jump 0%,1:jump 100%]")
        };

        var result = _exporter.Export(rows);

        var lines = result.Split(Environment.NewLine);
        Assert.Equal(3, lines.Length);
        Assert.Equal("| File.cs | MyClass | MyMethod | [10] | 0 | 50% (1/2) | [0:jump 0%,1:jump 100%] |", lines[2]);
    }

    [Fact]
    public void Export_MultipleRows_FormatsAll()
    {
        var rows = new[]
        {
            new MissingCoverageRow("File1.cs", "Class1", "Method1", [10], 0, null, null),
            new MissingCoverageRow("File2.cs", "Class2", "Method2", [20, 21], 0, "50%", "[0:jump 0%]")
        };

        var result = _exporter.Export(rows);

        var lines = result.Split(Environment.NewLine);
        Assert.Equal(4, lines.Length);
        Assert.Equal(ExpectedHeader, lines[0]);
        Assert.Equal(ExpectedSeparator, lines[1]);
        Assert.Contains("File1.cs", lines[2], StringComparison.Ordinal);
        Assert.Contains("File2.cs", lines[3], StringComparison.Ordinal);
    }

    [Fact]
    public void Export_WithLimit_LimitsOutput()
    {
        var rows = new[]
        {
            new MissingCoverageRow("File1.cs", "Class1", "Method1", [10], 0, null, null),
            new MissingCoverageRow("File2.cs", "Class2", "Method2", [20], 0, null, null),
            new MissingCoverageRow("File3.cs", "Class3", "Method3", [30], 0, null, null)
        };

        var result = _exporter.Export(rows, limit: 2);

        var lines = result.Split(Environment.NewLine);
        Assert.Equal(4, lines.Length); // Header + Separator + 2 data rows
        Assert.Contains("File1.cs", lines[2], StringComparison.Ordinal);
        Assert.Contains("File2.cs", lines[3], StringComparison.Ordinal);
        Assert.DoesNotContain("File3.cs", result, StringComparison.Ordinal);
    }

    [Fact]
    public void Export_NullMethod_UsesEmptyString()
    {
        var rows = new[]
        {
            new MissingCoverageRow("File.cs", "MyClass", null, [10], 0, null, null)
        };

        var result = _exporter.Export(rows);

        var lines = result.Split(Environment.NewLine);
        Assert.Equal("| File.cs | MyClass |  | [10] | 0 |  |  |", lines[2]);
    }

    [Fact]
    public void Export_LimitExceedsRowCount_ReturnsAllRows()
    {
        var rows = new[]
        {
            new MissingCoverageRow("File.cs", "MyClass", "MyMethod", [10], 0, null, null)
        };

        var result = _exporter.Export(rows, limit: 100);

        var lines = result.Split(Environment.NewLine);
        Assert.Equal(3, lines.Length);
    }

    [Fact]
    public void Export_LimitZero_ReturnsHeaderAndSeparatorOnly()
    {
        var rows = new[]
        {
            new MissingCoverageRow("File.cs", "MyClass", "MyMethod", [10], 0, null, null)
        };

        var result = _exporter.Export(rows, limit: 0);

        var lines = result.Split(Environment.NewLine);
        Assert.Equal(2, lines.Length);
        Assert.Equal(ExpectedHeader, lines[0]);
        Assert.Equal(ExpectedSeparator, lines[1]);
    }

    [Fact]
    public void Export_WithPathTransformer_TransformsPaths()
    {
        // Arrange
        var basePath = Path.Combine(Path.GetTempPath(), "base");
        var absolutePath = Path.Combine(basePath, "src", "File.cs");
        var rows = new[]
        {
            new MissingCoverageRow(absolutePath, "MyClass", "MyMethod", [10], 0, null, null)
        };
        var transformer = new PathTransformer(basePath);

        // Act
        var result = _exporter.Export(rows, pathTransformer: transformer);

        // Assert
        var lines = result.Split(Environment.NewLine);
        var expectedPath = Path.Combine("src", "File.cs");
        Assert.Contains(expectedPath, lines[2], StringComparison.Ordinal);
    }

    [Fact]
    public void Export_WithNullTransformer_UsesOriginalPaths()
    {
        // Arrange
        var absolutePath = "/absolute/path/to/File.cs";
        var rows = new[]
        {
            new MissingCoverageRow(absolutePath, "MyClass", "MyMethod", [10], 0, null, null)
        };

        // Act
        var result = _exporter.Export(rows, pathTransformer: null);

        // Assert
        var lines = result.Split(Environment.NewLine);
        Assert.Contains(absolutePath, lines[2], StringComparison.Ordinal);
    }

    [Fact]
    public void Export_WithNullPathTransformerInstance_UsesOriginalPaths()
    {
        // Arrange
        var absolutePath = "/absolute/path/to/File.cs";
        var rows = new[]
        {
            new MissingCoverageRow(absolutePath, "MyClass", "MyMethod", [10], 0, null, null)
        };

        // Act
        var result = _exporter.Export(rows, pathTransformer: NullPathTransformer.Instance);

        // Assert
        var lines = result.Split(Environment.NewLine);
        Assert.Contains(absolutePath, lines[2], StringComparison.Ordinal);
    }

    [Fact]
    public void Export_ValueWithPipeCharacter_EscapesPipeCharacter()
    {
        // Arrange - file path containing a pipe character
        var rows = new[]
        {
            new MissingCoverageRow("src|path/File.cs", "My|Class", "My|Method", [10], 0, null, null)
        };

        // Act
        var result = _exporter.Export(rows);

        // Assert
        var lines = result.Split(Environment.NewLine);
        Assert.Equal("| src\\|path/File.cs | My\\|Class | My\\|Method | [10] | 0 |  |  |", lines[2]);
    }

    [Fact]
    public void Export_BranchConditionsWithPipe_EscapesPipeCharacter()
    {
        // Arrange - branch conditions with pipe character
        var rows = new[]
        {
            new MissingCoverageRow("File.cs", "MyClass", "Method", [10], 0, "50%", "cond|ition")
        };

        // Act
        var result = _exporter.Export(rows);

        // Assert
        var lines = result.Split(Environment.NewLine);
        Assert.Contains("cond\\|ition", lines[2], StringComparison.Ordinal);
    }
}
