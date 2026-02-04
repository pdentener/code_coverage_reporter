using System.Diagnostics.CodeAnalysis;

namespace CodeCoverageReporter.Cobertura.Models;

/// <summary>
/// Represents class-level coverage information from a Cobertura report.
/// </summary>
/// <param name="Name">The fully-qualified class name.</param>
/// <param name="FilePath">The source file path, or null if not specified.</param>
/// <param name="Methods">The method coverage entries for this class.</param>
/// <param name="ClassLines">Lines that appear under the class but not within any method.</param>
/// <param name="LineRate">The line coverage rate (0.0-1.0).</param>
/// <param name="BranchRate">The branch coverage rate (0.0-1.0).</param>
/// <param name="Complexity">The cyclomatic complexity of the class.</param>
/// <param name="TotalLines">The total number of lines in this class.</param>
/// <param name="CoveredLines">The number of covered lines in this class.</param>
// Exclusion: BranchRate property getter is never accessed in the codebase (parsed but unused).
[ExcludeFromCodeCoverage]
public sealed record ClassCoverage(
    string Name,
    string? FilePath,
    IReadOnlyList<MethodCoverage> Methods,
    IReadOnlyList<LineCoverage> ClassLines,
    double LineRate,
    double BranchRate,
    int Complexity,
    int TotalLines,
    int CoveredLines
);
