using CodeCoverageReporter.Cobertura.Models;

namespace CodeCoverageReporter.Cobertura.Merging;

/// <summary>
/// Merges multiple Cobertura coverage reports into a single unified model.
/// </summary>
public interface ICoverageMerger
{
    /// <summary>
    /// Merges multiple coverage reports into a single report.
    /// </summary>
    /// <param name="reports">The coverage reports to merge.</param>
    /// <returns>A single merged coverage report.</returns>
    /// <exception cref="CoberturaException">Thrown when there are conflicting entries that cannot be merged.</exception>
    CoverageReport Merge(IEnumerable<CoverageReport> reports);
}
