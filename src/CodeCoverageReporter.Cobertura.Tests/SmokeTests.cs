using CodeCoverageReporter.Cobertura.Models;

namespace CodeCoverageReporter.Cobertura.Tests;

public sealed class SmokeTests
{
    [Fact]
    public void CoverageReport_CanBeCreated()
    {
        // Arrange & Act
        var report = new CoverageReport(
            Packages: [],
            Sources: [],
            LineRate: 0.0,
            BranchRate: 0.0,
            Complexity: 0,
            Timestamp: 0,
            Version: "1.9",
            LinesCovered: 0,
            LinesValid: 0,
            BranchesCovered: 0,
            BranchesValid: 0,
            TotalLines: 0,
            CoveredLines: 0
        );

        // Assert
        Assert.NotNull(report);
    }

    [Fact]
    public void LineCoverage_CanBeCreated()
    {
        // Arrange & Act
        var line = new LineCoverage(
            Number: 42,
            Hits: 5,
            IsBranch: true,
            ConditionCoverage: "50% (1/2)",
            Conditions: [new BranchCondition(0, "jump", "50%")],
            FilePath: "/src/Test.cs",
            Scope: LineScope.Method
        );

        // Assert
        Assert.NotNull(line);
        Assert.Equal(42, line.Number);
        Assert.Equal(5, line.Hits);
        Assert.True(line.IsBranch);
    }

    [Fact]
    public void MethodCoverage_CanBeCreated()
    {
        // Arrange & Act
        var method = new MethodCoverage(
            Name: "TestMethod",
            Signature: "(System.String)",
            Lines: [],
            LineRate: 0.75,
            BranchRate: 0.5,
            Complexity: 3,
            TotalLines: 20,
            CoveredLines: 15
        );

        // Assert
        Assert.NotNull(method);
        Assert.Equal("TestMethod", method.Name);
        Assert.Equal(0.75, method.LineRate);
    }

    [Fact]
    public void ClassCoverage_CanBeCreated()
    {
        // Arrange & Act
        var classCoverage = new ClassCoverage(
            Name: "MyApp.Services.TestService",
            FilePath: "/src/Services/TestService.cs",
            Methods: [],
            ClassLines: [],
            LineRate: 0.8,
            BranchRate: 0.6,
            Complexity: 5,
            TotalLines: 100,
            CoveredLines: 80
        );

        // Assert
        Assert.NotNull(classCoverage);
        Assert.Equal("MyApp.Services.TestService", classCoverage.Name);
    }

    [Fact]
    public void PackageCoverage_CanBeCreated()
    {
        // Arrange & Act
        var package = new PackageCoverage(
            Name: "MyApp.Services",
            Classes: [],
            LineRate: 0.85,
            BranchRate: 0.7,
            Complexity: 10,
            TotalLines: 500,
            CoveredLines: 425
        );

        // Assert
        Assert.NotNull(package);
        Assert.Equal("MyApp.Services", package.Name);
    }
}
