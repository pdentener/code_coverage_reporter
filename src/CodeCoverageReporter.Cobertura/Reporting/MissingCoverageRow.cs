namespace CodeCoverageReporter.Cobertura.Reporting;

/// <summary>
/// Represents a single output row for missing coverage reporting.
/// </summary>
/// <param name="File">The source file path.</param>
/// <param name="Class">The fully-qualified class name.</param>
/// <param name="Method">The method name, or null for class-level lines.</param>
/// <param name="LineNumbers">The line numbers with missing coverage (will be formatted to collapsed ranges).</param>
/// <param name="Hits">The number of hits (always 0 for missing coverage).</param>
/// <param name="BranchCoverage">The branch coverage string (e.g., "50% (1/2)"), or null for non-branch rows.</param>
/// <param name="BranchConditions">The branch conditions string (e.g., "[0:jump 0%,1:jump 100%]"), or null for non-branch rows.</param>
public sealed record MissingCoverageRow(
    string File,
    string Class,
    string? Method,
    IReadOnlyList<int> LineNumbers,
    int Hits,
    string? BranchCoverage,
    string? BranchConditions
);
