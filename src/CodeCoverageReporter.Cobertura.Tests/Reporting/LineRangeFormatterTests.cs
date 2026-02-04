using CodeCoverageReporter.Cobertura.Reporting;

namespace CodeCoverageReporter.Cobertura.Tests.Reporting;

public sealed class LineRangeFormatterTests
{
    [Fact]
    public void Format_NullList_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => LineRangeFormatter.Format(null!));
    }

    [Fact]
    public void Format_EmptyList_ReturnsEmptyBrackets()
    {
        var result = LineRangeFormatter.Format([]);
        Assert.Equal("[]", result);
    }

    [Fact]
    public void Format_SingleLine_ReturnsSingleNumber()
    {
        var result = LineRangeFormatter.Format([42]);
        Assert.Equal("[42]", result);
    }

    [Fact]
    public void Format_TwoConsecutiveLines_ReturnsRange()
    {
        var result = LineRangeFormatter.Format([42, 43]);
        Assert.Equal("[42-43]", result);
    }

    [Fact]
    public void Format_TwoNonConsecutiveLines_ReturnsSeparateNumbers()
    {
        var result = LineRangeFormatter.Format([42, 44]);
        Assert.Equal("[42, 44]", result);
    }

    [Fact]
    public void Format_ThreeConsecutiveLines_ReturnsRange()
    {
        var result = LineRangeFormatter.Format([10, 11, 12]);
        Assert.Equal("[10-12]", result);
    }

    [Fact]
    public void Format_ComplexPattern_ReturnsCorrectFormat()
    {
        var result = LineRangeFormatter.Format([10, 11, 12, 15, 20, 25, 26, 27]);
        Assert.Equal("[10-12, 15, 20, 25-27]", result);
    }

    [Fact]
    public void Format_UnsortedInput_SortsAndFormats()
    {
        var result = LineRangeFormatter.Format([30, 10, 20, 11, 12]);
        Assert.Equal("[10-12, 20, 30]", result);
    }

    [Fact]
    public void Format_DuplicateLineNumbers_DeduplicatesAndFormats()
    {
        var result = LineRangeFormatter.Format([10, 10, 11, 11, 12]);
        Assert.Equal("[10-12]", result);
    }

    [Fact]
    public void Format_SingleRange_ReturnsRange()
    {
        var result = LineRangeFormatter.Format([1, 2, 3, 4, 5]);
        Assert.Equal("[1-5]", result);
    }

    [Fact]
    public void Format_AllSingleNumbers_ReturnsCommaSeparated()
    {
        var result = LineRangeFormatter.Format([1, 3, 5, 7, 9]);
        Assert.Equal("[1, 3, 5, 7, 9]", result);
    }

    [Fact]
    public void Format_LargeGaps_HandlesCorrectly()
    {
        var result = LineRangeFormatter.Format([1, 100, 1000]);
        Assert.Equal("[1, 100, 1000]", result);
    }

    [Fact]
    public void Format_RangeAtStart_FormatsCorrectly()
    {
        var result = LineRangeFormatter.Format([1, 2, 3, 10]);
        Assert.Equal("[1-3, 10]", result);
    }

    [Fact]
    public void Format_RangeAtEnd_FormatsCorrectly()
    {
        var result = LineRangeFormatter.Format([1, 10, 11, 12]);
        Assert.Equal("[1, 10-12]", result);
    }

    [Fact]
    public void Format_TwoRanges_FormatsCorrectly()
    {
        var result = LineRangeFormatter.Format([1, 2, 3, 10, 11, 12]);
        Assert.Equal("[1-3, 10-12]", result);
    }
}
