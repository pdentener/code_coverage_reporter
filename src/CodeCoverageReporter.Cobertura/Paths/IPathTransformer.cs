namespace CodeCoverageReporter.Cobertura.Paths;

/// <summary>
/// Transforms file paths for display purposes.
/// </summary>
public interface IPathTransformer
{
    /// <summary>
    /// Transforms a file path for display.
    /// </summary>
    /// <param name="absolutePath">The absolute file path to transform.</param>
    /// <returns>The transformed path.</returns>
    string Transform(string absolutePath);
}
