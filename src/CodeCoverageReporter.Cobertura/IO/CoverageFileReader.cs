using Microsoft.Extensions.FileSystemGlobbing;
using Microsoft.Extensions.FileSystemGlobbing.Abstractions;

namespace CodeCoverageReporter.Cobertura.IO;

/// <summary>
/// Provides file reading capabilities for coverage reports, supporting both explicit paths and glob patterns.
/// </summary>
public sealed class CoverageFileReader : ICoverageFileReader
{
    private readonly string _basePath;

    /// <summary>
    /// Initializes a new instance of the <see cref="CoverageFileReader"/> class using the current directory as the base path.
    /// </summary>
    public CoverageFileReader() : this(Directory.GetCurrentDirectory())
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="CoverageFileReader"/> class with a specified base path.
    /// </summary>
    /// <param name="basePath">The base path for resolving relative paths and glob patterns.</param>
    public CoverageFileReader(string basePath)
    {
        _basePath = basePath;
    }

    /// <inheritdoc />
    public IReadOnlyList<string> ResolveFiles(IEnumerable<string> pathsOrPatterns)
    {
        ArgumentNullException.ThrowIfNull(pathsOrPatterns);

        var resolvedFiles = new List<string>();

        foreach (var pathOrPattern in pathsOrPatterns)
        {
            var files = ResolvePathOrPattern(pathOrPattern);
            resolvedFiles.AddRange(files);
        }

        // Remove duplicates while preserving order
        return resolvedFiles.Distinct().ToList();
    }

    /// <inheritdoc />
    public Stream OpenFile(string filePath)
    {
        var absolutePath = GetAbsolutePath(filePath);

        if (!File.Exists(absolutePath))
        {
            throw new CoberturaException($"File not found: {filePath}");
        }

        try
        {
            return File.OpenRead(absolutePath);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            throw new CoberturaException($"Cannot open file: {filePath}", ex);
        }
    }

    private List<string> ResolvePathOrPattern(string pathOrPattern)
    {
        // Check if it's a glob pattern (contains wildcard characters)
        if (IsGlobPattern(pathOrPattern))
        {
            return ResolveGlobPattern(pathOrPattern);
        }

        // Treat as explicit file path
        return ResolveExplicitPath(pathOrPattern);
    }

    private List<string> ResolveExplicitPath(string filePath)
    {
        var absolutePath = GetAbsolutePath(filePath);

        if (!File.Exists(absolutePath))
        {
            throw new CoberturaException($"File not found: {filePath}");
        }

        return [absolutePath];
    }

    private List<string> ResolveGlobPattern(string pattern)
    {
        var matcher = new Matcher();

        // Handle patterns with directory parts (e.g., "coverage/*.xml" or "**/coverage.xml")
        matcher.AddInclude(pattern);

        var directoryInfo = new DirectoryInfoWrapper(new DirectoryInfo(_basePath));
        var result = matcher.Execute(directoryInfo);

        if (!result.HasMatches)
        {
            throw new CoberturaException($"No files matched the pattern: {pattern}");
        }

        return result.Files
            .Select(f => Path.GetFullPath(Path.Combine(_basePath, f.Path)))
            .ToList();
    }

    private string GetAbsolutePath(string path)
    {
        if (Path.IsPathRooted(path))
        {
            return path;
        }

        return Path.GetFullPath(Path.Combine(_basePath, path));
    }

    private static bool IsGlobPattern(string path)
    {
        return path.Contains('*', StringComparison.Ordinal)
            || path.Contains('?', StringComparison.Ordinal)
            || path.Contains('[', StringComparison.Ordinal);
    }
}
