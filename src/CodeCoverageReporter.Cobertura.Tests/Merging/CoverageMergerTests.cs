using CodeCoverageReporter.Cobertura.Merging;
using CodeCoverageReporter.Cobertura.Models;

namespace CodeCoverageReporter.Cobertura.Tests.Merging;

public sealed class CoverageMergerTests
{
    private readonly CoverageMerger _merger = new();

    [Fact]
    public void Merge_EmptyReports_ReturnsEmptyReport()
    {
        // Arrange
        var reports = Array.Empty<CoverageReport>();

        // Act
        var result = _merger.Merge(reports);

        // Assert
        Assert.NotNull(result);
        Assert.Empty(result.Packages);
        Assert.Empty(result.Sources);
        Assert.Equal(0, result.TotalLines);
        Assert.Equal(0, result.CoveredLines);
    }

    [Fact]
    public void Merge_SingleReport_ReturnsUnmodified()
    {
        // Arrange
        var report = CreateReport("Package1", "Class1", "Method1", 10, 8);

        // Act
        var result = _merger.Merge([report]);

        // Assert
        Assert.Same(report, result);
    }

    [Fact]
    public void Merge_DisjointPackages_CombinesPackages()
    {
        // Arrange
        var report1 = CreateReport("Package1", "Class1", "Method1", 10, 8);
        var report2 = CreateReport("Package2", "Class2", "Method2", 5, 3);

        // Act
        var result = _merger.Merge([report1, report2]);

        // Assert
        Assert.Equal(2, result.Packages.Count);
        Assert.Contains(result.Packages, p => p.Name == "Package1");
        Assert.Contains(result.Packages, p => p.Name == "Package2");
        Assert.Equal(15, result.TotalLines);
        Assert.Equal(11, result.CoveredLines);
    }

    [Fact]
    public void Merge_OverlappingPackages_MergesClasses()
    {
        // Arrange
        var report1 = CreateReport("Package1", "Class1", "Method1", 10, 8);
        var report2 = CreateReport("Package1", "Class2", "Method2", 5, 3);

        // Act
        var result = _merger.Merge([report1, report2]);

        // Assert
        Assert.Single(result.Packages);
        var package = result.Packages[0];
        Assert.Equal("Package1", package.Name);
        Assert.Equal(2, package.Classes.Count);
        Assert.Equal(15, package.TotalLines);
        Assert.Equal(11, package.CoveredLines);
    }

    [Fact]
    public void Merge_OverlappingClasses_MergesMethods()
    {
        // Arrange
        var report1 = CreateReportWithFilePath("Package1", "Class1", "Method1", "Class1.cs", 10, 8);
        var report2 = CreateReportWithFilePath("Package1", "Class1", "Method2", "Class1.cs", 5, 3);

        // Act
        var result = _merger.Merge([report1, report2]);

        // Assert
        Assert.Single(result.Packages);
        var classItem = result.Packages[0].Classes[0];
        Assert.Equal("Class1", classItem.Name);
        Assert.Equal(2, classItem.Methods.Count);
    }

    [Fact]
    public void Merge_OverlappingMethods_SumsLineHits()
    {
        // Arrange
        var line1 = new LineCoverage(10, 5, false, null, [], "test.cs", LineScope.Method);
        var line2 = new LineCoverage(10, 3, false, null, [], "test.cs", LineScope.Method);

        var report1 = CreateReportWithLines("Package1", "Class1", "Method1", "test.cs", [line1]);
        var report2 = CreateReportWithLines("Package1", "Class1", "Method1", "test.cs", [line2]);

        // Act
        var result = _merger.Merge([report1, report2]);

        // Assert
        var mergedLine = result.Packages[0].Classes[0].Methods[0].Lines[0];
        Assert.Equal(10, mergedLine.Number);
        Assert.Equal(8, mergedLine.Hits); // 5 + 3
    }

    [Fact]
    public void Merge_BranchConditions_MergesUsingMaxCoverage()
    {
        // Arrange
        var condition1 = new BranchCondition(0, "jump", "25%");
        var condition2 = new BranchCondition(0, "jump", "75%");

        var line1 = new LineCoverage(10, 5, true, "25% (1/4)", [condition1], "test.cs", LineScope.Method);
        var line2 = new LineCoverage(10, 3, true, "75% (3/4)", [condition2], "test.cs", LineScope.Method);

        var report1 = CreateReportWithLines("Package1", "Class1", "Method1", "test.cs", [line1]);
        var report2 = CreateReportWithLines("Package1", "Class1", "Method1", "test.cs", [line2]);

        // Act
        var result = _merger.Merge([report1, report2]);

        // Assert
        var mergedLine = result.Packages[0].Classes[0].Methods[0].Lines[0];
        Assert.True(mergedLine.IsBranch);
        Assert.Single(mergedLine.Conditions);
        Assert.Equal("75%", mergedLine.Conditions[0].Coverage);
    }

    [Fact]
    public void Merge_SourcesDeduplication_RemovesDuplicates()
    {
        // Arrange
        var report1 = new CoverageReport([], ["/src/", "/tests/"], 0, 0, 0, 0, "1.9", 0, 0, 0, 0, 0, 0);
        var report2 = new CoverageReport([], ["/src/", "/lib/"], 0, 0, 0, 0, "1.9", 0, 0, 0, 0, 0, 0);

        // Act
        var result = _merger.Merge([report1, report2]);

        // Assert
        Assert.Equal(3, result.Sources.Count);
        Assert.Contains("/src/", result.Sources);
        Assert.Contains("/tests/", result.Sources);
        Assert.Contains("/lib/", result.Sources);
    }

    [Fact]
    public void Merge_StatisticsRecalculated_AfterMerge()
    {
        // Arrange
        var report1 = CreateReport("Package1", "Class1", "Method1", 10, 8);
        var report2 = CreateReport("Package1", "Class1", "Method2", 10, 2);

        // Act
        var result = _merger.Merge([report1, report2]);

        // Assert
        Assert.Equal(20, result.TotalLines);
        Assert.Equal(10, result.CoveredLines);
        Assert.Equal(0.5, result.LineRate);
    }

    [Fact]
    public void Merge_DifferentFilePaths_KeepsClassesSeparate()
    {
        // Arrange - Same class name in different files (e.g., source + source-generated partial class)
        var report1 = CreateReportWithFilePath("Package1", "Class1", "Method1", "path1/Class1.cs", 10, 8);
        var report2 = CreateReportWithFilePath("Package1", "Class1", "Method2", "path2/Class1.cs", 5, 3);

        // Act
        var result = _merger.Merge([report1, report2]);

        // Assert - Classes with different file paths are kept separate
        Assert.Single(result.Packages);
        Assert.Equal(2, result.Packages[0].Classes.Count);
        Assert.Contains(result.Packages[0].Classes, c => c.FilePath == "path1/Class1.cs");
        Assert.Contains(result.Packages[0].Classes, c => c.FilePath == "path2/Class1.cs");
    }

    [Fact]
    public void Merge_SameClassNameSameFilePath_MergesIntoSingleClass()
    {
        // Arrange - Same class name and same file path from different reports should merge
        var report1 = CreateReportWithFilePath("Package1", "Class1", "Method1", "src/Class1.cs", 10, 8);
        var report2 = CreateReportWithFilePath("Package1", "Class1", "Method2", "src/Class1.cs", 5, 3);

        // Act
        var result = _merger.Merge([report1, report2]);

        // Assert - Classes with same name and file path merge into one
        Assert.Single(result.Packages);
        Assert.Single(result.Packages[0].Classes);
        Assert.Equal("Class1", result.Packages[0].Classes[0].Name);
        Assert.Equal("src/Class1.cs", result.Packages[0].Classes[0].FilePath);
        Assert.Equal(2, result.Packages[0].Classes[0].Methods.Count);
    }

    [Fact]
    public void Merge_ConflictingBranchFlags_ThrowsCoberturaException()
    {
        // Arrange
        var line1 = new LineCoverage(10, 5, true, "50%", [], "test.cs", LineScope.Method);
        var line2 = new LineCoverage(10, 3, false, null, [], "test.cs", LineScope.Method);

        var report1 = CreateReportWithLines("Package1", "Class1", "Method1", "test.cs", [line1]);
        var report2 = CreateReportWithLines("Package1", "Class1", "Method1", "test.cs", [line2]);

        // Act & Assert
        var ex = Assert.Throws<CoberturaException>(() => _merger.Merge([report1, report2]));
        Assert.Contains("branch", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Merge_NullParameter_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => _merger.Merge(null!));
    }

    [Fact]
    public void Merge_DeterministicOrder_SameResultRegardlessOfInputOrder()
    {
        // Arrange
        var report1 = CreateReport("Package1", "Class1", "Method1", 10, 8);
        var report2 = CreateReport("Package2", "Class2", "Method2", 5, 3);

        // Act
        var result1 = _merger.Merge([report1, report2]);
        var result2 = _merger.Merge([report2, report1]);

        // Assert
        Assert.Equal(result1.TotalLines, result2.TotalLines);
        Assert.Equal(result1.CoveredLines, result2.CoveredLines);
        Assert.Equal(result1.Packages.Count, result2.Packages.Count);
    }

    [Fact]
    public void Merge_ClassLines_MergedSeparatelyFromMethods()
    {
        // Arrange
        var methodLine = new LineCoverage(10, 5, false, null, [], "test.cs", LineScope.Method);
        var classLine = new LineCoverage(5, 3, false, null, [], "test.cs", LineScope.Class);

        var method = new MethodCoverage("Method1", "()", [methodLine], 1.0, 0, 1, 1, 1);
        var classItem = new ClassCoverage("Class1", "test.cs", [method], [classLine], 1.0, 0, 1, 2, 2);
        var package = new PackageCoverage("Package1", [classItem], 1.0, 0, 1, 2, 2);
        var report = new CoverageReport([package], [], 1.0, 0, 1, 0, "1.9", 2, 2, 0, 0, 2, 2);

        // Act
        var result = _merger.Merge([report]);

        // Assert
        var mergedClass = result.Packages[0].Classes[0];
        Assert.Single(mergedClass.Methods);
        Assert.Single(mergedClass.ClassLines);
        Assert.Equal(5, mergedClass.ClassLines[0].Number);
    }

    private static CoverageReport CreateReport(string packageName, string className, string methodName, int totalLines, int coveredLines)
    {
        var lines = Enumerable.Range(1, totalLines)
            .Select(i => new LineCoverage(i, i <= coveredLines ? 1 : 0, false, null, [], null, LineScope.Method))
            .ToList();

        var method = new MethodCoverage(methodName, "()", lines, (double)coveredLines / totalLines, 0, 1, totalLines, coveredLines);
        var classItem = new ClassCoverage(className, null, [method], [], (double)coveredLines / totalLines, 0, 1, totalLines, coveredLines);
        var package = new PackageCoverage(packageName, [classItem], (double)coveredLines / totalLines, 0, 1, totalLines, coveredLines);

        return new CoverageReport([package], [], (double)coveredLines / totalLines, 0, 1, 0, "1.9", coveredLines, totalLines, 0, 0, totalLines, coveredLines);
    }

    private static CoverageReport CreateReportWithFilePath(string packageName, string className, string methodName, string filePath, int totalLines, int coveredLines)
    {
        var lines = Enumerable.Range(1, totalLines)
            .Select(i => new LineCoverage(i, i <= coveredLines ? 1 : 0, false, null, [], filePath, LineScope.Method))
            .ToList();

        var method = new MethodCoverage(methodName, "()", lines, (double)coveredLines / totalLines, 0, 1, totalLines, coveredLines);
        var classItem = new ClassCoverage(className, filePath, [method], [], (double)coveredLines / totalLines, 0, 1, totalLines, coveredLines);
        var package = new PackageCoverage(packageName, [classItem], (double)coveredLines / totalLines, 0, 1, totalLines, coveredLines);

        return new CoverageReport([package], [], (double)coveredLines / totalLines, 0, 1, 0, "1.9", coveredLines, totalLines, 0, 0, totalLines, coveredLines);
    }

    private static CoverageReport CreateReportWithLines(string packageName, string className, string methodName, string filePath, List<LineCoverage> lines)
    {
        var totalLines = lines.Count;
        var coveredLines = lines.Count(l => l.Hits > 0);

        var method = new MethodCoverage(methodName, "()", lines, totalLines > 0 ? (double)coveredLines / totalLines : 0, 0, 1, totalLines, coveredLines);
        var classItem = new ClassCoverage(className, filePath, [method], [], totalLines > 0 ? (double)coveredLines / totalLines : 0, 0, 1, totalLines, coveredLines);
        var package = new PackageCoverage(packageName, [classItem], totalLines > 0 ? (double)coveredLines / totalLines : 0, 0, 1, totalLines, coveredLines);

        return new CoverageReport([package], [], totalLines > 0 ? (double)coveredLines / totalLines : 0, 0, 1, 0, "1.9", coveredLines, totalLines, 0, 0, totalLines, coveredLines);
    }

    [Fact]
    public void Merge_SingleLinePerNumber_ReturnsUnmergedLines()
    {
        // Arrange - Each line has unique number, triggering single-line early return in MergeLineGroup
        var line1 = new LineCoverage(10, 5, false, null, [], "test.cs", LineScope.Method);
        var line2 = new LineCoverage(20, 3, false, null, [], "test.cs", LineScope.Method);

        var report1 = CreateReportWithLines("Package1", "Class1", "Method1", "test.cs", [line1]);
        var report2 = CreateReportWithLines("Package1", "Class1", "Method1", "test.cs", [line2]);

        // Act
        var result = _merger.Merge([report1, report2]);

        // Assert - Both lines should be present, each passed through the single-line path
        var mergedLines = result.Packages[0].Classes[0].Methods[0].Lines;
        Assert.Equal(2, mergedLines.Count);
        Assert.Contains(mergedLines, l => l.Number == 10 && l.Hits == 5);
        Assert.Contains(mergedLines, l => l.Number == 20 && l.Hits == 3);
    }

    [Fact]
    public void Merge_SingleBranchConditionPerNumber_ReturnsUnmergedConditions()
    {
        // Arrange - Each condition has unique number, triggering single-condition early return in MergeConditionGroup
        var condition1 = new BranchCondition(0, "jump", "50%");
        var condition2 = new BranchCondition(1, "jump", "100%");

        var line1 = new LineCoverage(10, 5, true, "50% (1/2)", [condition1], "test.cs", LineScope.Method);
        var line2 = new LineCoverage(10, 3, true, "100% (2/2)", [condition2], "test.cs", LineScope.Method);

        var report1 = CreateReportWithLines("Package1", "Class1", "Method1", "test.cs", [line1]);
        var report2 = CreateReportWithLines("Package1", "Class1", "Method1", "test.cs", [line2]);

        // Act
        var result = _merger.Merge([report1, report2]);

        // Assert - Both conditions should be present, each passed through the single-condition path
        var mergedLine = result.Packages[0].Classes[0].Methods[0].Lines[0];
        Assert.Equal(2, mergedLine.Conditions.Count);
        Assert.Contains(mergedLine.Conditions, c => c.Number == 0 && c.Coverage == "50%");
        Assert.Contains(mergedLine.Conditions, c => c.Number == 1 && c.Coverage == "100%");
    }

    [Fact]
    public void Merge_BranchLinesWithConditionCoverage_SelectsFirstNonNullCoverage()
    {
        // Arrange - Test condition coverage selection when merging branch lines
        var condition = new BranchCondition(0, "jump", "75%");
        var line1 = new LineCoverage(10, 2, true, "75% (3/4)", [condition], "test.cs", LineScope.Method);
        var line2 = new LineCoverage(10, 1, true, "50% (2/4)", [condition], "test.cs", LineScope.Method);

        var report1 = CreateReportWithLines("Package1", "Class1", "Method1", "test.cs", [line1]);
        var report2 = CreateReportWithLines("Package1", "Class1", "Method1", "test.cs", [line2]);

        // Act
        var result = _merger.Merge([report1, report2]);

        // Assert - First non-null condition coverage should be selected
        var mergedLine = result.Packages[0].Classes[0].Methods[0].Lines[0];
        Assert.True(mergedLine.IsBranch);
        Assert.Equal("75% (3/4)", mergedLine.ConditionCoverage);
        Assert.Equal(3, mergedLine.Hits); // 2 + 1
    }

    [Fact]
    public void Merge_BranchLinesWithNullConditionCoverage_ReturnsNullCoverage()
    {
        // Arrange - All branch lines have null condition coverage
        var line1 = new LineCoverage(10, 2, true, null, [], "test.cs", LineScope.Method);
        var line2 = new LineCoverage(10, 1, true, null, [], "test.cs", LineScope.Method);

        var report1 = CreateReportWithLines("Package1", "Class1", "Method1", "test.cs", [line1]);
        var report2 = CreateReportWithLines("Package1", "Class1", "Method1", "test.cs", [line2]);

        // Act
        var result = _merger.Merge([report1, report2]);

        // Assert - Condition coverage should remain null when all inputs are null
        var mergedLine = result.Packages[0].Classes[0].Methods[0].Lines[0];
        Assert.True(mergedLine.IsBranch);
        Assert.Null(mergedLine.ConditionCoverage);
    }

    [Fact]
    public void Merge_ClassLinesFromMultipleReports_MergesCorrectly()
    {
        // Arrange - Test class lines merging across reports
        var classLine1 = new LineCoverage(5, 2, false, null, [], "test.cs", LineScope.Class);
        var classLine2 = new LineCoverage(5, 3, false, null, [], "test.cs", LineScope.Class);

        var method = new MethodCoverage("Method1", "()", [], 0, 0, 1, 0, 0);
        var class1 = new ClassCoverage("Class1", "test.cs", [method], [classLine1], 1.0, 0, 1, 1, 1);
        var class2 = new ClassCoverage("Class1", "test.cs", [method], [classLine2], 1.0, 0, 1, 1, 1);
        var package1 = new PackageCoverage("Package1", [class1], 1.0, 0, 1, 1, 1);
        var package2 = new PackageCoverage("Package1", [class2], 1.0, 0, 1, 1, 1);
        var report1 = new CoverageReport([package1], [], 1.0, 0, 1, 0, "1.9", 1, 1, 0, 0, 1, 1);
        var report2 = new CoverageReport([package2], [], 1.0, 0, 1, 0, "1.9", 1, 1, 0, 0, 1, 1);

        // Act
        var result = _merger.Merge([report1, report2]);

        // Assert - Class lines should be merged with hits summed
        var mergedClass = result.Packages[0].Classes[0];
        Assert.Single(mergedClass.ClassLines);
        Assert.Equal(5, mergedClass.ClassLines[0].Number);
        Assert.Equal(5, mergedClass.ClassLines[0].Hits); // 2 + 3
    }

    [Fact]
    public void Merge_ClassWithNullFilePath_MergesSuccessfully()
    {
        // Arrange - Classes with null file paths should merge without conflict
        var line1 = new LineCoverage(10, 5, false, null, [], null, LineScope.Method);
        var line2 = new LineCoverage(20, 3, false, null, [], null, LineScope.Method);

        var method1 = new MethodCoverage("Method1", "()", [line1], 1.0, 0, 1, 1, 1);
        var method2 = new MethodCoverage("Method2", "()", [line2], 1.0, 0, 1, 1, 1);
        var class1 = new ClassCoverage("Class1", null, [method1], [], 1.0, 0, 1, 1, 1);
        var class2 = new ClassCoverage("Class1", null, [method2], [], 1.0, 0, 1, 1, 1);
        var package1 = new PackageCoverage("Package1", [class1], 1.0, 0, 1, 1, 1);
        var package2 = new PackageCoverage("Package1", [class2], 1.0, 0, 1, 1, 1);
        var report1 = new CoverageReport([package1], [], 1.0, 0, 1, 0, "1.9", 1, 1, 0, 0, 1, 1);
        var report2 = new CoverageReport([package2], [], 1.0, 0, 1, 0, "1.9", 1, 1, 0, 0, 1, 1);

        // Act
        var result = _merger.Merge([report1, report2]);

        // Assert
        var mergedClass = result.Packages[0].Classes[0];
        Assert.Null(mergedClass.FilePath);
        Assert.Equal(2, mergedClass.Methods.Count);
    }

    [Fact]
    public void Merge_PackagesWithZeroLines_CalculatesLineRateAsZero()
    {
        // Arrange - Test line rate calculation when totalLines is zero
        var method = new MethodCoverage("Method1", "()", [], 0, 0, 1, 0, 0);
        var class1 = new ClassCoverage("Class1", "test.cs", [method], [], 0, 0, 1, 0, 0);
        var class2 = new ClassCoverage("Class2", "test2.cs", [method], [], 0, 0, 1, 0, 0);
        var package1 = new PackageCoverage("Package1", [class1], 0, 0, 1, 0, 0);
        var package2 = new PackageCoverage("Package1", [class2], 0, 0, 1, 0, 0);
        var report1 = new CoverageReport([package1], [], 0, 0, 1, 0, "1.9", 0, 0, 0, 0, 0, 0);
        var report2 = new CoverageReport([package2], [], 0, 0, 1, 0, "1.9", 0, 0, 0, 0, 0, 0);

        // Act
        var result = _merger.Merge([report1, report2]);

        // Assert - LineRate should be 0 when totalLines is 0
        var mergedPackage = result.Packages[0];
        Assert.Equal(0.0, mergedPackage.LineRate);
        Assert.Equal(0, mergedPackage.TotalLines);
    }

    [Fact]
    public void Merge_MethodsWithZeroLines_CalculatesLineRateAsZero()
    {
        // Arrange - Test method line rate calculation when totalLines is zero
        var method1 = new MethodCoverage("Method1", "()", [], 0, 0, 1, 0, 0);
        var method2 = new MethodCoverage("Method1", "()", [], 0, 0, 2, 0, 0);
        var class1 = new ClassCoverage("Class1", "test.cs", [method1], [], 0, 0, 1, 0, 0);
        var class2 = new ClassCoverage("Class1", "test.cs", [method2], [], 0, 0, 2, 0, 0);
        var package1 = new PackageCoverage("Package1", [class1], 0, 0, 1, 0, 0);
        var package2 = new PackageCoverage("Package1", [class2], 0, 0, 2, 0, 0);
        var report1 = new CoverageReport([package1], [], 0, 0, 1, 0, "1.9", 0, 0, 0, 0, 0, 0);
        var report2 = new CoverageReport([package2], [], 0, 0, 2, 0, "1.9", 0, 0, 0, 0, 0, 0);

        // Act
        var result = _merger.Merge([report1, report2]);

        // Assert - Method should have max complexity and 0 line rate
        var mergedMethod = result.Packages[0].Classes[0].Methods[0];
        Assert.Equal(0.0, mergedMethod.LineRate);
        Assert.Equal(2, mergedMethod.Complexity); // Max complexity across merged methods
    }

    [Fact]
    public void Merge_ClassesWithZeroLines_CalculatesLineRateAsZero()
    {
        // Arrange - Test class line rate calculation when totalLines is zero
        var method = new MethodCoverage("Method1", "()", [], 0, 0, 1, 0, 0);
        var class1 = new ClassCoverage("Class1", "test.cs", [method], [], 0, 0, 1, 0, 0);
        var class2 = new ClassCoverage("Class1", "test.cs", [method], [], 0, 0, 2, 0, 0);
        var package1 = new PackageCoverage("Package1", [class1], 0, 0, 1, 0, 0);
        var package2 = new PackageCoverage("Package1", [class2], 0, 0, 2, 0, 0);
        var report1 = new CoverageReport([package1], [], 0, 0, 1, 0, "1.9", 0, 0, 0, 0, 0, 0);
        var report2 = new CoverageReport([package2], [], 0, 0, 2, 0, "1.9", 0, 0, 0, 0, 0, 0);

        // Act
        var result = _merger.Merge([report1, report2]);

        // Assert - Class should have 0 line rate when no lines
        var mergedClass = result.Packages[0].Classes[0];
        Assert.Equal(0.0, mergedClass.LineRate);
    }
}
