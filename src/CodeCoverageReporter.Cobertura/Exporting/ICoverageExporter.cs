using CodeCoverageReporter.Cobertura.Paths;
using CodeCoverageReporter.Cobertura.Reporting;

namespace CodeCoverageReporter.Cobertura.Exporting;

/// <summary>
/// Exports missing coverage rows to a specific output format.
/// </summary>
public interface ICoverageExporter
{
    /// <summary>
    /// Exports the missing coverage rows to a formatted string.
    /// </summary>
    /// <param name="rows">The missing coverage rows to export.</param>
    /// <param name="limit">Optional limit on the number of rows to include.</param>
    /// <param name="pathTransformer">Optional path transformer for file paths.</param>
    /// <returns>The formatted output string.</returns>
    string Export(IReadOnlyList<MissingCoverageRow> rows, int? limit = null, IPathTransformer? pathTransformer = null);
}
