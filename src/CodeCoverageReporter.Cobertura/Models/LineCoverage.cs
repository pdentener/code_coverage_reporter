namespace CodeCoverageReporter.Cobertura.Models;

/// <summary>
/// Represents line-level coverage information from a Cobertura report.
/// </summary>
/// <param name="Number">The line number in the source file.</param>
/// <param name="Hits">The number of times this line was executed.</param>
/// <param name="IsBranch">Whether this line contains a branch (case-insensitive from XML).</param>
/// <param name="ConditionCoverage">The aggregate condition coverage string (e.g., "50% (1/2)"), or null if not present.</param>
/// <param name="Conditions">The individual branch conditions, or empty list if none.</param>
/// <param name="FilePath">The resolved file path (from sources), or raw filename if resolution failed.</param>
/// <param name="Scope">Whether this line belongs to a method or directly to the class.</param>
public sealed record LineCoverage(
    int Number,
    int Hits,
    bool IsBranch,
    string? ConditionCoverage,
    IReadOnlyList<BranchCondition> Conditions,
    string? FilePath,
    LineScope Scope
);
