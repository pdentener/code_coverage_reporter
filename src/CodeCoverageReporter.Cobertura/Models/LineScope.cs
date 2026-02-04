namespace CodeCoverageReporter.Cobertura.Models;

/// <summary>
/// Indicates whether a line belongs to a method or directly to the class.
/// </summary>
public enum LineScope
{
    /// <summary>
    /// Line is within a method.
    /// </summary>
    Method,

    /// <summary>
    /// Line is directly under the class (not within any method).
    /// </summary>
    Class
}
