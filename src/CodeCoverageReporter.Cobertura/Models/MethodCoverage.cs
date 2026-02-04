using System.Diagnostics.CodeAnalysis;

namespace CodeCoverageReporter.Cobertura.Models;

/// <summary>
/// Represents method-level coverage information from a Cobertura report.
/// </summary>
/// <param name="Name">The method name.</param>
/// <param name="Signature">The method signature.</param>
/// <param name="Lines">The line coverage entries for this method.</param>
/// <param name="LineRate">The line coverage rate (0.0-1.0).</param>
/// <param name="BranchRate">The branch coverage rate (0.0-1.0).</param>
/// <param name="Complexity">The cyclomatic complexity of the method.</param>
/// <param name="TotalLines">The total number of lines in this method.</param>
/// <param name="CoveredLines">The number of covered lines in this method.</param>
// Exclusion: BranchRate property getter is never accessed in the codebase (parsed but unused).
[ExcludeFromCodeCoverage]
public sealed record MethodCoverage(
    string Name,
    string Signature,
    IReadOnlyList<LineCoverage> Lines,
    double LineRate,
    double BranchRate,
    int Complexity,
    int TotalLines,
    int CoveredLines
);
