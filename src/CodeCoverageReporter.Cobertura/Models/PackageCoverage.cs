using System.Diagnostics.CodeAnalysis;

namespace CodeCoverageReporter.Cobertura.Models;

/// <summary>
/// Represents package-level coverage information from a Cobertura report.
/// </summary>
/// <param name="Name">The package/namespace name.</param>
/// <param name="Classes">The class coverage entries for this package.</param>
/// <param name="LineRate">The line coverage rate (0.0-1.0).</param>
/// <param name="BranchRate">The branch coverage rate (0.0-1.0).</param>
/// <param name="Complexity">The total cyclomatic complexity of the package.</param>
/// <param name="TotalLines">The total number of lines in this package.</param>
/// <param name="CoveredLines">The number of covered lines in this package.</param>
// Exclusion: BranchRate property getter is never accessed in the codebase (parsed but unused).
[ExcludeFromCodeCoverage]
public sealed record PackageCoverage(
    string Name,
    IReadOnlyList<ClassCoverage> Classes,
    double LineRate,
    double BranchRate,
    int Complexity,
    int TotalLines,
    int CoveredLines
);
