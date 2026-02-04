using CodeCoverageReporter.Cobertura.Models;

namespace CodeCoverageReporter.Cobertura.Merging;

/// <summary>
/// Calculates coverage statistics from merged coverage data.
/// </summary>
public static class CoverageStatisticsCalculator
{
    /// <summary>
    /// Calculates the line rate (covered lines / total lines) from a collection of lines.
    /// </summary>
    /// <param name="lines">The lines to calculate coverage for.</param>
    /// <returns>The line rate as a value between 0.0 and 1.0.</returns>
    public static double CalculateLineRate(IReadOnlyList<LineCoverage> lines)
    {
        ArgumentNullException.ThrowIfNull(lines);

        if (lines.Count == 0)
        {
            return 0.0;
        }

        var coveredLines = lines.Count(l => l.Hits > 0);
        return (double)coveredLines / lines.Count;
    }

    /// <summary>
    /// Calculates the branch rate from a collection of lines with branch information.
    /// </summary>
    /// <param name="lines">The lines to calculate branch coverage for.</param>
    /// <returns>The branch rate as a value between 0.0 and 1.0.</returns>
    public static double CalculateBranchRate(IReadOnlyList<LineCoverage> lines)
    {
        ArgumentNullException.ThrowIfNull(lines);

        var branchLines = lines.Where(l => l.IsBranch && l.Conditions.Count > 0).ToList();
        if (branchLines.Count == 0)
        {
            return 0.0;
        }

        var totalConditions = branchLines.Sum(l => l.Conditions.Count);

        var coveredConditions = branchLines.Sum(l =>
            l.Conditions.Count(c => ParseCoveragePercent(c.Coverage) > 0));

        return (double)coveredConditions / totalConditions;
    }

    /// <summary>
    /// Sums complexity values from a collection of methods.
    /// </summary>
    /// <param name="methods">The methods to sum complexity for.</param>
    /// <returns>The total complexity.</returns>
    public static int SumComplexity(IReadOnlyList<MethodCoverage> methods)
    {
        ArgumentNullException.ThrowIfNull(methods);

        return methods.Sum(m => m.Complexity);
    }

    /// <summary>
    /// Parses a coverage percentage string (e.g., "50%") to a decimal value.
    /// </summary>
    /// <param name="coverageString">The coverage string to parse.</param>
    /// <returns>The coverage as a percentage (0-100), or 0 if parsing fails.</returns>
    public static double ParseCoveragePercent(string coverageString)
    {
        if (string.IsNullOrEmpty(coverageString))
        {
            return 0;
        }

        // Remove % symbol and parse
        var trimmed = coverageString.TrimEnd('%');
        return double.TryParse(trimmed, System.Globalization.CultureInfo.InvariantCulture, out var result)
            ? result
            : 0;
    }
}
