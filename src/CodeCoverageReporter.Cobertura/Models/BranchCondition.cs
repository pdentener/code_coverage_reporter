namespace CodeCoverageReporter.Cobertura.Models;

/// <summary>
/// Represents an individual branch condition from Cobertura's conditions element.
/// </summary>
/// <param name="Number">The condition index/number.</param>
/// <param name="Type">The condition type (e.g., "jump", "switch").</param>
/// <param name="Coverage">The coverage percentage string (e.g., "50%").</param>
public sealed record BranchCondition(
    int Number,
    string Type,
    string Coverage
);
