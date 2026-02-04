using CodeCoverageReporter.Cobertura.Paths;

namespace CodeCoverageReporter.Cobertura.Tests.Paths;

public sealed class PathTransformerTests
{
    [Fact]
    public void Constructor_NullBasePath_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => new PathTransformer(null!));
    }

    [Fact]
    public void Constructor_EmptyBasePath_ThrowsArgumentException()
    {
        Assert.Throws<ArgumentException>(() => new PathTransformer(string.Empty));
    }

    [Fact]
    public void Constructor_WhitespaceBasePath_ThrowsArgumentException()
    {
        Assert.Throws<ArgumentException>(() => new PathTransformer("   "));
    }

    [Fact]
    public void Transform_NullPath_ThrowsArgumentNullException()
    {
        var transformer = new PathTransformer("/base");
        Assert.Throws<ArgumentNullException>(() => transformer.Transform(null!));
    }

    [Fact]
    public void Transform_ReturnsRelativePath()
    {
        // Arrange
        var basePath = Path.Combine(Path.GetTempPath(), "base");
        var absolutePath = Path.Combine(basePath, "subdir", "file.cs");
        var transformer = new PathTransformer(basePath);

        // Act
        var result = transformer.Transform(absolutePath);

        // Assert
        var expected = Path.Combine("subdir", "file.cs");
        Assert.Equal(expected, result);
    }

    [Fact]
    public void Transform_PathOutsideBase_ReturnsRelativeWithDotDot()
    {
        // Arrange
        var basePath = Path.Combine(Path.GetTempPath(), "base", "subdir");
        var absolutePath = Path.Combine(Path.GetTempPath(), "base", "other", "file.cs");
        var transformer = new PathTransformer(basePath);

        // Act
        var result = transformer.Transform(absolutePath);

        // Assert
        var expected = Path.Combine("..", "other", "file.cs");
        Assert.Equal(expected, result);
    }

    [Fact]
    public void Transform_SamePath_ReturnsDot()
    {
        // Arrange
        var basePath = Path.Combine(Path.GetTempPath(), "base");
        var transformer = new PathTransformer(basePath);

        // Act
        var result = transformer.Transform(basePath);

        // Assert
        Assert.Equal(".", result);
    }

    [Fact]
    public void Transform_FileInBaseDirectory_ReturnsFileName()
    {
        // Arrange
        var basePath = Path.Combine(Path.GetTempPath(), "base");
        var absolutePath = Path.Combine(basePath, "file.cs");
        var transformer = new PathTransformer(basePath);

        // Act
        var result = transformer.Transform(absolutePath);

        // Assert
        Assert.Equal("file.cs", result);
    }
}

public sealed class NullPathTransformerTests
{
    [Fact]
    public void Instance_ReturnsSameInstance()
    {
        var instance1 = NullPathTransformer.Instance;
        var instance2 = NullPathTransformer.Instance;

        Assert.Same(instance1, instance2);
    }

    [Fact]
    public void Transform_NullPath_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => NullPathTransformer.Instance.Transform(null!));
    }

    [Fact]
    public void Transform_ReturnsOriginalPath()
    {
        // Arrange
        var path = "/some/absolute/path/file.cs";

        // Act
        var result = NullPathTransformer.Instance.Transform(path);

        // Assert
        Assert.Equal(path, result);
    }

    [Fact]
    public void Transform_PreservesAbsolutePath()
    {
        // Arrange
        var absolutePath = Path.Combine(Path.GetTempPath(), "folder", "file.cs");

        // Act
        var result = NullPathTransformer.Instance.Transform(absolutePath);

        // Assert
        Assert.Equal(absolutePath, result);
    }
}
