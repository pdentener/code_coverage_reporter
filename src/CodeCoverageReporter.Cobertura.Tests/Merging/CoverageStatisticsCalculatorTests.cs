using CodeCoverageReporter.Cobertura.Merging;
using CodeCoverageReporter.Cobertura.Models;

namespace CodeCoverageReporter.Cobertura.Tests.Merging;

public sealed class CoverageStatisticsCalculatorTests
{
    [Fact]
    public void CalculateLineRate_EmptyLines_ReturnsZero()
    {
        // Arrange
        var lines = Array.Empty<LineCoverage>();

        // Act
        var result = CoverageStatisticsCalculator.CalculateLineRate(lines);

        // Assert
        Assert.Equal(0.0, result);
    }

    [Fact]
    public void CalculateLineRate_AllLinesCovered_ReturnsOne()
    {
        // Arrange
        var lines = new[]
        {
            CreateLine(1, 5),
            CreateLine(2, 3),
            CreateLine(3, 1)
        };

        // Act
        var result = CoverageStatisticsCalculator.CalculateLineRate(lines);

        // Assert
        Assert.Equal(1.0, result);
    }

    [Fact]
    public void CalculateLineRate_NoLinesCovered_ReturnsZero()
    {
        // Arrange
        var lines = new[]
        {
            CreateLine(1, 0),
            CreateLine(2, 0),
            CreateLine(3, 0)
        };

        // Act
        var result = CoverageStatisticsCalculator.CalculateLineRate(lines);

        // Assert
        Assert.Equal(0.0, result);
    }

    [Fact]
    public void CalculateLineRate_PartialCoverage_ReturnsCorrectRate()
    {
        // Arrange
        var lines = new[]
        {
            CreateLine(1, 5),
            CreateLine(2, 0),
            CreateLine(3, 3),
            CreateLine(4, 0)
        };

        // Act
        var result = CoverageStatisticsCalculator.CalculateLineRate(lines);

        // Assert
        Assert.Equal(0.5, result); // 2 out of 4
    }

    [Fact]
    public void CalculateLineRate_NullParameter_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            CoverageStatisticsCalculator.CalculateLineRate(null!));
    }

    [Fact]
    public void CalculateBranchRate_EmptyLines_ReturnsZero()
    {
        // Arrange
        var lines = Array.Empty<LineCoverage>();

        // Act
        var result = CoverageStatisticsCalculator.CalculateBranchRate(lines);

        // Assert
        Assert.Equal(0.0, result);
    }

    [Fact]
    public void CalculateBranchRate_NoBranchLines_ReturnsZero()
    {
        // Arrange
        var lines = new[]
        {
            CreateLine(1, 5),
            CreateLine(2, 3)
        };

        // Act
        var result = CoverageStatisticsCalculator.CalculateBranchRate(lines);

        // Assert
        Assert.Equal(0.0, result);
    }

    [Fact]
    public void CalculateBranchRate_AllConditionsCovered_ReturnsOne()
    {
        // Arrange
        var lines = new[]
        {
            CreateBranchLine(1, 5, [
                new BranchCondition(0, "jump", "100%"),
                new BranchCondition(1, "jump", "100%")
            ])
        };

        // Act
        var result = CoverageStatisticsCalculator.CalculateBranchRate(lines);

        // Assert
        Assert.Equal(1.0, result);
    }

    [Fact]
    public void CalculateBranchRate_PartialConditionsCovered_ReturnsCorrectRate()
    {
        // Arrange
        var lines = new[]
        {
            CreateBranchLine(1, 5, [
                new BranchCondition(0, "jump", "100%"),
                new BranchCondition(1, "jump", "0%")
            ])
        };

        // Act
        var result = CoverageStatisticsCalculator.CalculateBranchRate(lines);

        // Assert
        Assert.Equal(0.5, result); // 1 out of 2
    }

    [Fact]
    public void CalculateBranchRate_NullParameter_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            CoverageStatisticsCalculator.CalculateBranchRate(null!));
    }

    [Fact]
    public void SumComplexity_EmptyMethods_ReturnsZero()
    {
        // Arrange
        var methods = Array.Empty<MethodCoverage>();

        // Act
        var result = CoverageStatisticsCalculator.SumComplexity(methods);

        // Assert
        Assert.Equal(0, result);
    }

    [Fact]
    public void SumComplexity_MultipleMethods_ReturnsSumOfComplexity()
    {
        // Arrange
        var methods = new[]
        {
            CreateMethod("Method1", 3),
            CreateMethod("Method2", 5),
            CreateMethod("Method3", 2)
        };

        // Act
        var result = CoverageStatisticsCalculator.SumComplexity(methods);

        // Assert
        Assert.Equal(10, result);
    }

    [Fact]
    public void SumComplexity_NullParameter_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            CoverageStatisticsCalculator.SumComplexity(null!));
    }

    [Theory]
    [InlineData("50%", 50)]
    [InlineData("100%", 100)]
    [InlineData("0%", 0)]
    [InlineData("75.5%", 75.5)]
    [InlineData("50", 50)]
    [InlineData("", 0)]
    [InlineData(null, 0)]
    [InlineData("invalid", 0)]
    public void ParseCoveragePercent_VariousInputs_ReturnsExpectedValue(string? input, double expected)
    {
        // Act
        var result = CoverageStatisticsCalculator.ParseCoveragePercent(input!);

        // Assert
        Assert.Equal(expected, result);
    }

    private static LineCoverage CreateLine(int number, int hits)
    {
        return new LineCoverage(
            Number: number,
            Hits: hits,
            IsBranch: false,
            ConditionCoverage: null,
            Conditions: [],
            FilePath: null,
            Scope: LineScope.Method
        );
    }

    private static LineCoverage CreateBranchLine(int number, int hits, BranchCondition[] conditions)
    {
        return new LineCoverage(
            Number: number,
            Hits: hits,
            IsBranch: true,
            ConditionCoverage: null,
            Conditions: conditions,
            FilePath: null,
            Scope: LineScope.Method
        );
    }

    private static MethodCoverage CreateMethod(string name, int complexity)
    {
        return new MethodCoverage(
            Name: name,
            Signature: "()",
            Lines: [],
            LineRate: 1.0,
            BranchRate: 1.0,
            Complexity: complexity,
            TotalLines: 0,
            CoveredLines: 0
        );
    }
}
