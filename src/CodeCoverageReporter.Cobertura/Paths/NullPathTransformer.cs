namespace CodeCoverageReporter.Cobertura.Paths;

/// <summary>
/// A path transformer that returns paths unchanged (identity transformation).
/// Used when absolute paths should be displayed.
/// </summary>
public sealed class NullPathTransformer : IPathTransformer
{
    /// <summary>
    /// Gets a shared instance of the <see cref="NullPathTransformer"/>.
    /// </summary>
    public static NullPathTransformer Instance { get; } = new();

    private NullPathTransformer()
    {
    }

    /// <inheritdoc />
    public string Transform(string absolutePath)
    {
        ArgumentNullException.ThrowIfNull(absolutePath);
        return absolutePath;
    }
}
