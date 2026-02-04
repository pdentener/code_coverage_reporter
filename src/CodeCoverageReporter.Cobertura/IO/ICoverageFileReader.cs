namespace CodeCoverageReporter.Cobertura.IO;

/// <summary>
/// Provides file reading capabilities for coverage reports, supporting both explicit paths and glob patterns.
/// </summary>
public interface ICoverageFileReader
{
    /// <summary>
    /// Resolves a collection of paths or glob patterns to actual file paths.
    /// </summary>
    /// <param name="pathsOrPatterns">Explicit file paths or glob patterns (e.g., "coverage/*.xml").</param>
    /// <returns>A list of resolved file paths.</returns>
    /// <exception cref="CoberturaException">Thrown when a file does not exist or a pattern matches no files.</exception>
    IReadOnlyList<string> ResolveFiles(IEnumerable<string> pathsOrPatterns);

    /// <summary>
    /// Opens a file for reading.
    /// </summary>
    /// <param name="filePath">The path to the file.</param>
    /// <returns>A stream for reading the file.</returns>
    /// <exception cref="CoberturaException">Thrown when the file does not exist or cannot be opened.</exception>
    Stream OpenFile(string filePath);
}
