using System.Text.Json;
using CodeCoverageReporter.Cobertura.Exporting;
using CodeCoverageReporter.Cobertura.Paths;
using CodeCoverageReporter.Cobertura.Reporting;

namespace CodeCoverageReporter.Cobertura.Tests.Exporting;

public sealed class JsonExporterTests
{
    private readonly JsonExporter _exporter = new();

    [Fact]
    public void Export_NullRows_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => _exporter.Export(null!));
    }

    [Fact]
    public void Export_EmptyRows_ReturnsEmptyJsonArray()
    {
        var result = _exporter.Export([]);

        Assert.Equal("[]", result);
    }

    [Fact]
    public void Export_SingleNonBranchRow_FormatsCorrectly()
    {
        var rows = new[]
        {
            new MissingCoverageRow("File.cs", "MyClass", "MyMethod", [10, 11, 12], 0, null, null)
        };

        var result = _exporter.Export(rows);

        using var doc = JsonDocument.Parse(result);
        var root = doc.RootElement;
        Assert.Equal(JsonValueKind.Array, root.ValueKind);
        Assert.Equal(1, root.GetArrayLength());

        var row = root[0];
        Assert.Equal("File.cs", row.GetProperty("file").GetString());
        Assert.Equal("MyClass", row.GetProperty("class").GetString());
        Assert.Equal("MyMethod", row.GetProperty("method").GetString());
        Assert.Equal("[10-12]", row.GetProperty("lines").GetString());

        // Non-branch row should not have hits, branchCoverage, or branchConditions
        Assert.False(row.TryGetProperty("hits", out _));
        Assert.False(row.TryGetProperty("branchCoverage", out _));
        Assert.False(row.TryGetProperty("branchConditions", out _));
    }

    [Fact]
    public void Export_SingleBranchRow_IncludesBranchFields()
    {
        var rows = new[]
        {
            new MissingCoverageRow("File.cs", "MyClass", "MyMethod", [10], 0, "50% (1/2)", "[0:jump 0%,1:jump 100%]")
        };

        var result = _exporter.Export(rows);

        using var doc = JsonDocument.Parse(result);
        var row = doc.RootElement[0];

        Assert.Equal("File.cs", row.GetProperty("file").GetString());
        Assert.Equal(0, row.GetProperty("hits").GetInt32());
        Assert.Equal("50% (1/2)", row.GetProperty("branchCoverage").GetString());
        Assert.Equal("[0:jump 0%,1:jump 100%]", row.GetProperty("branchConditions").GetString());
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

        using var doc = JsonDocument.Parse(result);
        Assert.Equal(2, doc.RootElement.GetArrayLength());
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

        using var doc = JsonDocument.Parse(result);
        Assert.Equal(2, doc.RootElement.GetArrayLength());
        Assert.Equal("File1.cs", doc.RootElement[0].GetProperty("file").GetString());
        Assert.Equal("File2.cs", doc.RootElement[1].GetProperty("file").GetString());
    }

    [Fact]
    public void Export_NullMethod_OmitsMethodField()
    {
        var rows = new[]
        {
            new MissingCoverageRow("File.cs", "MyClass", null, [10], 0, null, null)
        };

        var result = _exporter.Export(rows);

        using var doc = JsonDocument.Parse(result);
        var row = doc.RootElement[0];

        // Method should be omitted when null
        Assert.False(row.TryGetProperty("method", out _));
    }

    [Fact]
    public void Export_UsesCamelCaseNaming()
    {
        var rows = new[]
        {
            new MissingCoverageRow("File.cs", "MyClass", "MyMethod", [10], 0, "50%", "[0:jump]")
        };

        var result = _exporter.Export(rows);

        // Should use camelCase: file, class, method, lines, hits, branchCoverage, branchConditions
        Assert.Contains("\"file\"", result, StringComparison.Ordinal);
        Assert.Contains("\"class\"", result, StringComparison.Ordinal);
        Assert.Contains("\"method\"", result, StringComparison.Ordinal);
        Assert.Contains("\"lines\"", result, StringComparison.Ordinal);
        Assert.Contains("\"hits\"", result, StringComparison.Ordinal);
        Assert.Contains("\"branchCoverage\"", result, StringComparison.Ordinal);
        Assert.Contains("\"branchConditions\"", result, StringComparison.Ordinal);
    }

    [Fact]
    public void Export_LimitExceedsRowCount_ReturnsAllRows()
    {
        var rows = new[]
        {
            new MissingCoverageRow("File.cs", "MyClass", "MyMethod", [10], 0, null, null)
        };

        var result = _exporter.Export(rows, limit: 100);

        using var doc = JsonDocument.Parse(result);
        Assert.Equal(1, doc.RootElement.GetArrayLength());
    }

    [Fact]
    public void Export_LimitZero_ReturnsEmptyArray()
    {
        var rows = new[]
        {
            new MissingCoverageRow("File.cs", "MyClass", "MyMethod", [10], 0, null, null)
        };

        var result = _exporter.Export(rows, limit: 0);

        Assert.Equal("[]", result);
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
        using var doc = JsonDocument.Parse(result);
        var expectedPath = Path.Combine("src", "File.cs");
        Assert.Equal(expectedPath, doc.RootElement[0].GetProperty("file").GetString());
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
        using var doc = JsonDocument.Parse(result);
        Assert.Equal(absolutePath, doc.RootElement[0].GetProperty("file").GetString());
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
        using var doc = JsonDocument.Parse(result);
        Assert.Equal(absolutePath, doc.RootElement[0].GetProperty("file").GetString());
    }
}
