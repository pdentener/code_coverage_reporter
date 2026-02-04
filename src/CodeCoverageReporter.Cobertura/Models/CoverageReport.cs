using System.Diagnostics.CodeAnalysis;

namespace CodeCoverageReporter.Cobertura.Models;

/// <summary>
/// Represents a complete Cobertura coverage report.
/// </summary>
/// <param name="Packages">The package coverage entries.</param>
/// <param name="Sources">The source directory paths from the report.</param>
/// <param name="LineRate">The overall line coverage rate (0.0-1.0).</param>
/// <param name="BranchRate">The overall branch coverage rate (0.0-1.0).</param>
/// <param name="Complexity">The total cyclomatic complexity.</param>
/// <param name="Timestamp">The Unix timestamp when the report was generated.</param>
/// <param name="Version">The Cobertura format version.</param>
/// <param name="LinesCovered">The number of lines covered.</param>
/// <param name="LinesValid">The total number of valid/measurable lines.</param>
/// <param name="BranchesCovered">The number of branches covered.</param>
/// <param name="BranchesValid">The total number of valid/measurable branches.</param>
/// <param name="TotalLines">The total number of lines across all packages.</param>
/// <param name="CoveredLines">The number of covered lines across all packages.</param>
// Exclusion: BranchesCovered, BranchesValid, Complexity, LinesCovered, LinesValid property getters
// are never accessed in the codebase (parsed but unused).
[ExcludeFromCodeCoverage]
public sealed record CoverageReport(
    IReadOnlyList<PackageCoverage> Packages,
    IReadOnlyList<string> Sources,
    double LineRate,
    double BranchRate,
    int Complexity,
    long Timestamp,
    string Version,
    int LinesCovered,
    int LinesValid,
    int BranchesCovered,
    int BranchesValid,
    int TotalLines,
    int CoveredLines
);
