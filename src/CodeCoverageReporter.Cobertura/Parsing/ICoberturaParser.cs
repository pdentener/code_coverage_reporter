using CodeCoverageReporter.Cobertura.Models;

namespace CodeCoverageReporter.Cobertura.Parsing;

/// <summary>
/// Parses Cobertura XML coverage reports into the in-memory model.
/// </summary>
public interface ICoberturaParser
{
    /// <summary>
    /// Parses a Cobertura XML report from a stream.
    /// </summary>
    /// <param name="xmlStream">The stream containing the Cobertura XML.</param>
    /// <returns>The parsed coverage report.</returns>
    CoverageReport Parse(Stream xmlStream);

    /// <summary>
    /// Parses a Cobertura XML report from a string.
    /// </summary>
    /// <param name="xmlContent">The Cobertura XML content.</param>
    /// <returns>The parsed coverage report.</returns>
    CoverageReport Parse(string xmlContent);
}
