using CodeCoverageReporter.Cobertura.Models;
using CodeCoverageReporter.Cobertura.Reporting;

namespace CodeCoverageReporter.Cobertura.Tests.Reporting;

public sealed class MissingCoverageExtractorTests
{
    private readonly MissingCoverageExtractor _extractor = new();

    [Fact]
    public void Extract_NullReport_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => _extractor.Extract(null!));
    }

    [Fact]
    public void Extract_EmptyReport_ReturnsEmptyList()
    {
        // Arrange
        var report = CreateReport([]);

        // Act
        var result = _extractor.Extract(report);

        // Assert
        Assert.Empty(result);
    }

    [Fact]
    public void Extract_AllLinesCovered_ReturnsEmptyList()
    {
        // Arrange
        var lines = new[]
        {
            CreateLine(10, hits: 1),
            CreateLine(11, hits: 5),
            CreateLine(12, hits: 2)
        };
        var report = CreateReportWithMethod("TestFile.cs", "TestClass", "TestMethod", lines);

        // Act
        var result = _extractor.Extract(report);

        // Assert
        Assert.Empty(result);
    }

    [Fact]
    public void Extract_SingleUncoveredLine_ReturnsSingleRow()
    {
        // Arrange
        var lines = new[]
        {
            CreateLine(10, hits: 0)
        };
        var report = CreateReportWithMethod("TestFile.cs", "TestClass", "TestMethod", lines);

        // Act
        var result = _extractor.Extract(report);

        // Assert
        var row = Assert.Single(result);
        Assert.Equal("TestFile.cs", row.File);
        Assert.Equal("TestClass", row.Class);
        Assert.Equal("TestMethod", row.Method);
        Assert.Equal([10], row.LineNumbers);
        Assert.Equal(0, row.Hits);
        Assert.Null(row.BranchCoverage);
        Assert.Null(row.BranchConditions);
    }

    [Fact]
    public void Extract_MultipleConsecutiveUncoveredLines_GroupedIntoOneRow()
    {
        // Arrange
        var lines = new[]
        {
            CreateLine(10, hits: 0),
            CreateLine(11, hits: 0),
            CreateLine(12, hits: 0)
        };
        var report = CreateReportWithMethod("TestFile.cs", "TestClass", "TestMethod", lines);

        // Act
        var result = _extractor.Extract(report);

        // Assert
        var row = Assert.Single(result);
        Assert.Equal([10, 11, 12], row.LineNumbers);
    }

    [Fact]
    public void Extract_BranchLineWithIncompleteCoverage_EmitsSeparateRow()
    {
        // Arrange
        var conditions = new[]
        {
            new BranchCondition(0, "jump", "0%"),
            new BranchCondition(1, "jump", "100%")
        };
        var lines = new[]
        {
            CreateLine(10, hits: 1, isBranch: true, conditionCoverage: "50% (1/2)", conditions: conditions)
        };
        var report = CreateReportWithMethod("TestFile.cs", "TestClass", "TestMethod", lines);

        // Act
        var result = _extractor.Extract(report);

        // Assert
        var row = Assert.Single(result);
        Assert.Equal([10], row.LineNumbers);
        Assert.Equal("50% (1/2)", row.BranchCoverage);
        Assert.Equal("[0:jump 0%,1:jump 100%]", row.BranchConditions);
    }

    [Fact]
    public void Extract_NonBranchLinesFlushBeforeBranchLine()
    {
        // Arrange
        var branchConditions = new[]
        {
            new BranchCondition(0, "jump", "0%"),
            new BranchCondition(1, "jump", "100%")
        };
        var lines = new[]
        {
            CreateLine(10, hits: 0),
            CreateLine(11, hits: 0),
            CreateLine(12, hits: 1, isBranch: true, conditionCoverage: "50% (1/2)", conditions: branchConditions)
        };
        var report = CreateReportWithMethod("TestFile.cs", "TestClass", "TestMethod", lines);

        // Act
        var result = _extractor.Extract(report);

        // Assert
        Assert.Equal(2, result.Count);
        Assert.Equal([10, 11], result[0].LineNumbers);
        Assert.Null(result[0].BranchCoverage);
        Assert.Equal([12], result[1].LineNumbers);
        Assert.Equal("50% (1/2)", result[1].BranchCoverage);
    }

    [Fact]
    public void Extract_ClassLines_HandledCorrectly()
    {
        // Arrange
        var classLines = new[]
        {
            CreateLine(5, hits: 0, scope: LineScope.Class)
        };
        var report = CreateReportWithClassLines("TestFile.cs", "TestClass", classLines);

        // Act
        var result = _extractor.Extract(report);

        // Assert
        var row = Assert.Single(result);
        Assert.Equal("TestFile.cs", row.File);
        Assert.Equal("TestClass", row.Class);
        Assert.Null(row.Method);
        Assert.Equal([5], row.LineNumbers);
    }

    [Fact]
    public void Extract_SortsByFileClassMethodLineNumber()
    {
        // Arrange
        var packages = new[]
        {
            new PackageCoverage("Package1",
            [
                new ClassCoverage("ClassB", "FileB.cs",
                [
                    new MethodCoverage("MethodA", "void MethodA()", [CreateLine(20, hits: 0)], 0, 0, 0, 1, 0)
                ], [], 0, 0, 0, 1, 0),
                new ClassCoverage("ClassA", "FileA.cs",
                [
                    new MethodCoverage("MethodA", "void MethodA()", [CreateLine(10, hits: 0)], 0, 0, 0, 1, 0)
                ], [], 0, 0, 0, 1, 0)
            ], 0, 0, 0, 0, 0)
        };
        var report = new CoverageReport(packages, [], 0, 0, 0, 0, "1.0", 0, 2, 0, 0, 2, 0);

        // Act
        var result = _extractor.Extract(report);

        // Assert
        Assert.Equal(2, result.Count);
        Assert.Equal("FileA.cs", result[0].File);
        Assert.Equal("FileB.cs", result[1].File);
    }

    [Fact]
    public void Extract_MixedCoveredAndUncoveredLines_OnlyUncoveredIncluded()
    {
        // Arrange
        var lines = new[]
        {
            CreateLine(10, hits: 1),
            CreateLine(11, hits: 0),
            CreateLine(12, hits: 5),
            CreateLine(13, hits: 0),
            CreateLine(14, hits: 0)
        };
        var report = CreateReportWithMethod("TestFile.cs", "TestClass", "TestMethod", lines);

        // Act
        var result = _extractor.Extract(report);

        // Assert
        var row = Assert.Single(result);
        Assert.Equal([11, 13, 14], row.LineNumbers);
    }

    [Fact]
    public void Extract_BranchLineWithHitsButIncompleteCoverage_TreatedAsUncovered()
    {
        // Arrange
        var conditions = new[]
        {
            new BranchCondition(0, "jump", "50%")
        };
        var lines = new[]
        {
            CreateLine(10, hits: 5, isBranch: true, conditionCoverage: "50% (1/2)", conditions: conditions)
        };
        var report = CreateReportWithMethod("TestFile.cs", "TestClass", "TestMethod", lines);

        // Act
        var result = _extractor.Extract(report);

        // Assert
        var row = Assert.Single(result);
        Assert.Equal(0, row.Hits);
        Assert.Equal("50% (1/2)", row.BranchCoverage);
    }

    [Fact]
    public void Extract_BranchLineWithFullCoverage_NotIncluded()
    {
        // Arrange
        var conditions = new[]
        {
            new BranchCondition(0, "jump", "100%"),
            new BranchCondition(1, "jump", "100%")
        };
        var lines = new[]
        {
            CreateLine(10, hits: 5, isBranch: true, conditionCoverage: "100% (2/2)", conditions: conditions)
        };
        var report = CreateReportWithMethod("TestFile.cs", "TestClass", "TestMethod", lines);

        // Act
        var result = _extractor.Extract(report);

        // Assert
        Assert.Empty(result);
    }

    [Fact]
    public void Extract_ConsecutiveBranchLines_EachGetsOwnRow()
    {
        // Arrange
        var conditions1 = new[] { new BranchCondition(0, "jump", "0%") };
        var conditions2 = new[] { new BranchCondition(0, "jump", "50%") };
        var lines = new[]
        {
            CreateLine(10, hits: 1, isBranch: true, conditionCoverage: "0% (0/1)", conditions: conditions1),
            CreateLine(11, hits: 1, isBranch: true, conditionCoverage: "50% (1/2)", conditions: conditions2)
        };
        var report = CreateReportWithMethod("TestFile.cs", "TestClass", "TestMethod", lines);

        // Act
        var result = _extractor.Extract(report);

        // Assert
        Assert.Equal(2, result.Count);
        Assert.Equal([10], result[0].LineNumbers);
        Assert.Equal([11], result[1].LineNumbers);
    }

    [Fact]
    public void Extract_BranchLineWithNoConditions_TreatedAsIncompleteBranch()
    {
        // Arrange
        var lines = new[]
        {
            CreateLine(10, hits: 1, isBranch: true, conditionCoverage: null, conditions: [])
        };
        var report = CreateReportWithMethod("TestFile.cs", "TestClass", "TestMethod", lines);

        // Act
        var result = _extractor.Extract(report);

        // Assert
        var row = Assert.Single(result);
        Assert.Equal("unknown", row.BranchCoverage);
        Assert.Null(row.BranchConditions);
    }

    private static LineCoverage CreateLine(
        int number,
        int hits,
        bool isBranch = false,
        string? conditionCoverage = null,
        IReadOnlyList<BranchCondition>? conditions = null,
        LineScope scope = LineScope.Method)
    {
        return new LineCoverage(number, hits, isBranch, conditionCoverage, conditions ?? [], null, scope);
    }

    private static CoverageReport CreateReport(IReadOnlyList<PackageCoverage> packages)
    {
        return new CoverageReport(packages, [], 0, 0, 0, 0, "1.0", 0, 0, 0, 0, 0, 0);
    }

    private static CoverageReport CreateReportWithMethod(
        string filePath,
        string className,
        string methodName,
        LineCoverage[] lines)
    {
        var method = new MethodCoverage(methodName, $"void {methodName}()", lines, 0, 0, 0, lines.Length, 0);
        var cls = new ClassCoverage(className, filePath, [method], [], 0, 0, 0, lines.Length, 0);
        var package = new PackageCoverage("TestPackage", [cls], 0, 0, 0, lines.Length, 0);
        return CreateReport([package]);
    }

    private static CoverageReport CreateReportWithClassLines(
        string filePath,
        string className,
        LineCoverage[] classLines)
    {
        var cls = new ClassCoverage(className, filePath, [], classLines, 0, 0, 0, classLines.Length, 0);
        var package = new PackageCoverage("TestPackage", [cls], 0, 0, 0, classLines.Length, 0);
        return CreateReport([package]);
    }

    [Fact]
    public void Extract_ClassWithNullFilePath_UsesEmptyString()
    {
        // Arrange - create a class with null FilePath to hit the ?? string.Empty branch
        var lines = new[] { CreateLine(10, hits: 0) };
        var method = new MethodCoverage("TestMethod", "void TestMethod()", lines, 0, 0, 0, 1, 0);
        var cls = new ClassCoverage("TestClass", null, [method], [], 0, 0, 0, 1, 0);
        var package = new PackageCoverage("TestPackage", [cls], 0, 0, 0, 1, 0);
        var report = CreateReport([package]);

        // Act
        var result = _extractor.Extract(report);

        // Assert
        var row = Assert.Single(result);
        Assert.Equal(string.Empty, row.File);
        Assert.Equal("TestClass", row.Class);
        Assert.Equal("TestMethod", row.Method);
    }

    [Fact]
    public void Extract_BranchWithConditionCoverageStringNotComplete_IncludedInOutput()
    {
        // Arrange - branch line with empty Conditions but ConditionCoverage string indicating incomplete coverage
        var lines = new[]
        {
            CreateLine(10, hits: 1, isBranch: true, conditionCoverage: "50% (1/2)", conditions: [])
        };
        var report = CreateReportWithMethod("TestFile.cs", "TestClass", "TestMethod", lines);

        // Act
        var result = _extractor.Extract(report);

        // Assert
        var row = Assert.Single(result);
        Assert.Equal([10], row.LineNumbers);
        Assert.Equal("50% (1/2)", row.BranchCoverage);
        Assert.Null(row.BranchConditions); // No conditions to format
    }

    [Fact]
    public void Extract_BranchWithConditionCoverageString100Percent_ExcludedFromOutput()
    {
        // Arrange - branch line with empty Conditions but ConditionCoverage string indicating full coverage
        var lines = new[]
        {
            CreateLine(10, hits: 1, isBranch: true, conditionCoverage: "100% (2/2)", conditions: [])
        };
        var report = CreateReportWithMethod("TestFile.cs", "TestClass", "TestMethod", lines);

        // Act
        var result = _extractor.Extract(report);

        // Assert
        Assert.Empty(result);
    }

    [Fact]
    public void Extract_MultipleRowsSameFileAndClass_SortsByMethodThenLineNumber()
    {
        // Arrange - same file and class, different methods to exercise ThenBy Method comparison
        // Also include same method with different line numbers to exercise ThenBy LineNumber comparison
        var packages = new[]
        {
            new PackageCoverage("Package",
            [
                new ClassCoverage("SameClass", "SameFile.cs",
                [
                    new MethodCoverage("MethodZ", "void MethodZ()", [CreateLine(30, hits: 0)], 0, 0, 0, 1, 0),
                    new MethodCoverage("MethodA", "void MethodA()", [CreateLine(20, hits: 0)], 0, 0, 0, 1, 0),
                    new MethodCoverage("MethodA", "void MethodA(int)", [CreateLine(10, hits: 0)], 0, 0, 0, 1, 0)
                ], [], 0, 0, 0, 3, 0)
            ], 0, 0, 0, 3, 0)
        };
        var report = new CoverageReport(packages, [], 0, 0, 0, 0, "1.0", 0, 3, 0, 0, 3, 0);

        // Act
        var result = _extractor.Extract(report);

        // Assert
        Assert.Equal(3, result.Count);
        // All have same file and class, so sorted by method then line number
        Assert.Equal("SameFile.cs", result[0].File);
        Assert.Equal("SameClass", result[0].Class);
        Assert.Equal("MethodA", result[0].Method);
        Assert.Equal([10], result[0].LineNumbers);

        Assert.Equal("MethodA", result[1].Method);
        Assert.Equal([20], result[1].LineNumbers);

        Assert.Equal("MethodZ", result[2].Method);
        Assert.Equal([30], result[2].LineNumbers);
    }

    [Fact]
    public void Extract_MixedClassLevelAndMethodLevel_SortsByNullMethodFirstThenLineNumber()
    {
        // Arrange - class-level lines (null Method) and method-level lines in same file/class
        // This exercises the null coalescing operator (Method ?? string.Empty) in SortRows
        var classLines = new[]
        {
            CreateLine(5, hits: 0, scope: LineScope.Class),
            CreateLine(8, hits: 0, scope: LineScope.Class)
        };
        var methodLines = new[] { CreateLine(15, hits: 0) };
        var packages = new[]
        {
            new PackageCoverage("Package",
            [
                new ClassCoverage("TestClass", "TestFile.cs",
                [
                    new MethodCoverage("TestMethod", "void TestMethod()", methodLines, 0, 0, 0, 1, 0)
                ], classLines, 0, 0, 0, 3, 0)
            ], 0, 0, 0, 3, 0)
        };
        var report = new CoverageReport(packages, [], 0, 0, 0, 0, "1.0", 0, 3, 0, 0, 3, 0);

        // Act
        var result = _extractor.Extract(report);

        // Assert
        Assert.Equal(2, result.Count);

        // First row: class-level lines (null Method, coalesces to empty string, sorts first)
        Assert.Equal("TestFile.cs", result[0].File);
        Assert.Equal("TestClass", result[0].Class);
        Assert.Null(result[0].Method);
        Assert.Equal([5, 8], result[0].LineNumbers);

        // Second row: method-level line
        Assert.Equal("TestMethod", result[1].Method);
        Assert.Equal([15], result[1].LineNumbers);
    }

    [Fact]
    public void Extract_MultipleClassLevelBranchRows_SortedByLineNumber()
    {
        // Arrange - multiple class-level branch rows with null Method, sorted by LineNumber
        // This ensures the ThenBy on LineNumbers is exercised when Methods are equal (both null)
        // Using branch lines because each branch with incomplete coverage creates its own row
        var branchConditions = new[] { new BranchCondition(0, "jump", "50%") };
        var classLines = new[]
        {
            CreateLine(30, hits: 1, isBranch: true, conditionCoverage: "50% (1/2)", conditions: branchConditions, scope: LineScope.Class),
            CreateLine(10, hits: 1, isBranch: true, conditionCoverage: "50% (1/2)", conditions: branchConditions, scope: LineScope.Class),
            CreateLine(20, hits: 1, isBranch: true, conditionCoverage: "50% (1/2)", conditions: branchConditions, scope: LineScope.Class)
        };
        var packages = new[]
        {
            new PackageCoverage("Package",
            [
                new ClassCoverage("TestClass", "TestFile.cs", [], classLines, 0, 0, 0, 3, 0)
            ], 0, 0, 0, 3, 0)
        };
        var report = new CoverageReport(packages, [], 0, 0, 0, 0, "1.0", 0, 3, 0, 0, 3, 0);

        // Act
        var result = _extractor.Extract(report);

        // Assert - all have null Method, sorted by first line number
        Assert.Equal(3, result.Count);

        // All rows have null Method (class-level) and are sorted by line number
        Assert.Equal([10], result[0].LineNumbers);
        Assert.Null(result[0].Method);
        Assert.NotNull(result[0].BranchCoverage);

        Assert.Equal([20], result[1].LineNumbers);
        Assert.Null(result[1].Method);

        Assert.Equal([30], result[2].LineNumbers);
        Assert.Null(result[2].Method);
    }
}
