namespace CodeCoverageReporter.Cobertura.Paths;

/// <summary>
/// Transforms absolute file paths to relative paths based on a base directory.
/// </summary>
public sealed class PathTransformer : IPathTransformer
{
    private readonly string _basePath;

    /// <summary>
    /// Initializes a new instance of the <see cref="PathTransformer"/> class.
    /// </summary>
    /// <param name="basePath">The base directory for calculating relative paths.</param>
    public PathTransformer(string basePath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(basePath);
        _basePath = basePath;
    }

    /// <inheritdoc />
    public string Transform(string absolutePath)
    {
        ArgumentNullException.ThrowIfNull(absolutePath);

        return Path.GetRelativePath(_basePath, absolutePath);
    }
}
