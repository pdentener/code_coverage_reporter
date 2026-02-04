using CodeCoverageReporter.Cobertura.IO;

namespace CodeCoverageReporter.Cobertura.Tests.IO;

public sealed class CoverageFileReaderTests : IDisposable
{
    private readonly string _tempDir;
    private readonly CoverageFileReader _reader;

    public CoverageFileReaderTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), "cobertura_tests_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_tempDir);
        _reader = new CoverageFileReader(_tempDir);
    }

    public void Dispose()
    {
        // Clean up temp directory
        if (Directory.Exists(_tempDir))
        {
            Directory.Delete(_tempDir, recursive: true);
        }
    }

    [Fact]
    public void ResolveFiles_SingleFile_ReturnsAbsolutePath()
    {
        // Arrange
        var testFile = Path.Combine(_tempDir, "coverage.xml");
        File.WriteAllText(testFile, "<coverage/>");

        // Act
        var result = _reader.ResolveFiles(["coverage.xml"]);

        // Assert
        Assert.Single(result);
        Assert.Equal(testFile, result[0]);
    }

    [Fact]
    public void ResolveFiles_MultipleFiles_ReturnsAllPaths()
    {
        // Arrange
        var file1 = Path.Combine(_tempDir, "coverage1.xml");
        var file2 = Path.Combine(_tempDir, "coverage2.xml");
        File.WriteAllText(file1, "<coverage/>");
        File.WriteAllText(file2, "<coverage/>");

        // Act
        var result = _reader.ResolveFiles(["coverage1.xml", "coverage2.xml"]);

        // Assert
        Assert.Equal(2, result.Count);
        Assert.Contains(file1, result);
        Assert.Contains(file2, result);
    }

    [Fact]
    public void ResolveFiles_GlobPattern_MatchesFiles()
    {
        // Arrange
        var file1 = Path.Combine(_tempDir, "coverage1.xml");
        var file2 = Path.Combine(_tempDir, "coverage2.xml");
        var otherFile = Path.Combine(_tempDir, "other.txt");
        File.WriteAllText(file1, "<coverage/>");
        File.WriteAllText(file2, "<coverage/>");
        File.WriteAllText(otherFile, "not xml");

        // Act
        var result = _reader.ResolveFiles(["*.xml"]);

        // Assert
        Assert.Equal(2, result.Count);
        Assert.Contains(file1, result);
        Assert.Contains(file2, result);
        Assert.DoesNotContain(otherFile, result);
    }

    [Fact]
    public void ResolveFiles_GlobPatternWithDirectory_MatchesFiles()
    {
        // Arrange
        var subDir = Path.Combine(_tempDir, "subdir");
        Directory.CreateDirectory(subDir);
        var file1 = Path.Combine(subDir, "coverage.xml");
        File.WriteAllText(file1, "<coverage/>");

        // Act
        var result = _reader.ResolveFiles(["subdir/*.xml"]);

        // Assert
        Assert.Single(result);
        Assert.Equal(file1, result[0]);
    }

    [Fact]
    public void ResolveFiles_RecursiveGlobPattern_MatchesNestedFiles()
    {
        // Arrange
        var subDir = Path.Combine(_tempDir, "level1", "level2");
        Directory.CreateDirectory(subDir);
        var file1 = Path.Combine(_tempDir, "root.xml");
        var file2 = Path.Combine(subDir, "nested.xml");
        File.WriteAllText(file1, "<coverage/>");
        File.WriteAllText(file2, "<coverage/>");

        // Act
        var result = _reader.ResolveFiles(["**/*.xml"]);

        // Assert
        Assert.Equal(2, result.Count);
        Assert.Contains(file1, result);
        Assert.Contains(file2, result);
    }

    [Fact]
    public void ResolveFiles_MixedPathsAndPatterns_ResolvesAll()
    {
        // Arrange
        var file1 = Path.Combine(_tempDir, "explicit.xml");
        var file2 = Path.Combine(_tempDir, "pattern.xml");
        File.WriteAllText(file1, "<coverage/>");
        File.WriteAllText(file2, "<coverage/>");

        // Act
        var result = _reader.ResolveFiles(["explicit.xml", "pattern.xml"]);

        // Assert
        Assert.Equal(2, result.Count);
    }

    [Fact]
    public void ResolveFiles_DuplicateFiles_RemovesDuplicates()
    {
        // Arrange
        var file = Path.Combine(_tempDir, "coverage.xml");
        File.WriteAllText(file, "<coverage/>");

        // Act - same file referenced twice
        var result = _reader.ResolveFiles(["coverage.xml", "coverage.xml"]);

        // Assert
        Assert.Single(result);
    }

    [Fact]
    public void ResolveFiles_FileNotFound_ThrowsCoberturaException()
    {
        // Act & Assert
        var ex = Assert.Throws<CoberturaException>(() =>
            _reader.ResolveFiles(["nonexistent.xml"]));

        Assert.Contains("nonexistent.xml", ex.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("not found", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void ResolveFiles_GlobNoMatches_ThrowsCoberturaException()
    {
        // Act & Assert
        var ex = Assert.Throws<CoberturaException>(() =>
            _reader.ResolveFiles(["*.xml"]));

        Assert.Contains("*.xml", ex.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("No files matched", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void ResolveFiles_AbsolutePath_ReturnsAsIs()
    {
        // Arrange
        var file = Path.Combine(_tempDir, "coverage.xml");
        File.WriteAllText(file, "<coverage/>");

        // Act
        var result = _reader.ResolveFiles([file]);

        // Assert
        Assert.Single(result);
        Assert.Equal(file, result[0]);
    }

    [Fact]
    public void ResolveFiles_NullParameter_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            _reader.ResolveFiles(null!));
    }

    [Fact]
    public void ResolveFiles_EmptyCollection_ReturnsEmptyList()
    {
        // Act
        var result = _reader.ResolveFiles([]);

        // Assert
        Assert.Empty(result);
    }

    [Fact]
    public void OpenFile_ExistingFile_ReturnsStream()
    {
        // Arrange
        var file = Path.Combine(_tempDir, "coverage.xml");
        var content = "<coverage/>";
        File.WriteAllText(file, content);

        // Act
        using var stream = _reader.OpenFile("coverage.xml");

        // Assert
        Assert.NotNull(stream);
        using var reader = new StreamReader(stream);
        var readContent = reader.ReadToEnd();
        Assert.Equal(content, readContent);
    }

    [Fact]
    public void OpenFile_AbsolutePath_ReturnsStream()
    {
        // Arrange
        var file = Path.Combine(_tempDir, "coverage.xml");
        File.WriteAllText(file, "<coverage/>");

        // Act
        using var stream = _reader.OpenFile(file);

        // Assert
        Assert.NotNull(stream);
    }

    [Fact]
    public void OpenFile_FileNotFound_ThrowsCoberturaException()
    {
        // Act & Assert
        var ex = Assert.Throws<CoberturaException>(() =>
            _reader.OpenFile("nonexistent.xml"));

        Assert.Contains("nonexistent.xml", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Constructor_DefaultBasePath_UsesCurrentDirectory()
    {
        // Arrange - create file in current directory
        var testFile = Path.Combine(Directory.GetCurrentDirectory(), "test_" + Guid.NewGuid().ToString("N") + ".xml");
        try
        {
            File.WriteAllText(testFile, "<coverage/>");
            var reader = new CoverageFileReader();

            // Act
            var result = reader.ResolveFiles([Path.GetFileName(testFile)]);

            // Assert
            Assert.Single(result);
            Assert.Equal(testFile, result[0]);
        }
        finally
        {
            // Cleanup
            if (File.Exists(testFile))
            {
                File.Delete(testFile);
            }
        }
    }

    [Fact]
    public void OpenFile_WhenFileCannotBeOpened_ThrowsCoberturaException()
    {
        // Arrange - create a file and lock it exclusively
        var file = Path.Combine(_tempDir, "locked.xml");
        File.WriteAllText(file, "<coverage/>");

        // Open the file with exclusive lock to cause IOException when OpenFile tries to open it
        using var lockStream = new FileStream(file, FileMode.Open, FileAccess.Read, FileShare.None);

        // Act & Assert
        var ex = Assert.Throws<CoberturaException>(() => _reader.OpenFile("locked.xml"));
        Assert.Contains("locked.xml", ex.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Cannot open file", ex.Message, StringComparison.OrdinalIgnoreCase);
        Assert.NotNull(ex.InnerException);
        Assert.IsType<IOException>(ex.InnerException);
    }

}
