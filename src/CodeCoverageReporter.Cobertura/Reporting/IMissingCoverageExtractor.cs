using CodeCoverageReporter.Cobertura.Models;

namespace CodeCoverageReporter.Cobertura.Reporting;

/// <summary>
/// Extracts missing coverage rows from a coverage report.
/// </summary>
public interface IMissingCoverageExtractor
{
    /// <summary>
    /// Extracts all rows with missing coverage from the report.
    /// </summary>
    /// <param name="report">The coverage report to extract from.</param>
    /// <returns>A list of rows representing missing coverage, sorted by File, Class, Method, and LineNumber.</returns>
    IReadOnlyList<MissingCoverageRow> Extract(CoverageReport report);
}
