using CodeCoverageReporter.CLI.Infrastructure;
using Microsoft.Extensions.DependencyInjection;

namespace CodeCoverageReporter.CLI.Tests.Infrastructure;

public sealed class TypeResolverTests
{
    [Fact]
    public void Resolve_NullType_ReturnsNull()
    {
        // Arrange
        var services = new ServiceCollection();
        using var resolver = new TypeResolver(services.BuildServiceProvider());

        // Act
        var result = resolver.Resolve(null);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void Resolve_RegisteredType_ReturnsService()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddSingleton<TestService>();
        using var resolver = new TypeResolver(services.BuildServiceProvider());

        // Act
        var result = resolver.Resolve(typeof(TestService));

        // Assert
        Assert.NotNull(result);
        Assert.IsType<TestService>(result);
    }

    [Fact]
    public void Resolve_UnregisteredType_ReturnsNull()
    {
        // Arrange
        var services = new ServiceCollection();
        using var resolver = new TypeResolver(services.BuildServiceProvider());

        // Act
        var result = resolver.Resolve(typeof(TestService));

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void Dispose_DisposesUnderlyingProvider()
    {
        // Arrange
        var services = new ServiceCollection();
        var provider = services.BuildServiceProvider();
        var resolver = new TypeResolver(provider);

        // Act & Assert - should not throw
        resolver.Dispose();
    }

    private sealed class TestService { }
}
