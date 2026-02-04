using CodeCoverageReporter.Cobertura.Exporting;
using CodeCoverageReporter.Cobertura.IO;
using CodeCoverageReporter.Cobertura.Merging;
using CodeCoverageReporter.Cobertura.Parsing;
using CodeCoverageReporter.Cobertura.Reporting;
using Microsoft.Extensions.DependencyInjection;

namespace CodeCoverageReporter.Cobertura.ExtensionMethods;

/// <summary>
/// Extension methods for registering Cobertura services with dependency injection.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds all Cobertura services to the service collection.
    /// </summary>
    /// <param name="services">The service collection to add services to.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddCobertura(this IServiceCollection services)
    {
        // Core services
        services.AddSingleton<ICoverageFileReader, CoverageFileReader>();
        services.AddSingleton<ICoberturaParser, CoberturaParser>();
        services.AddSingleton<ICoverageMerger, CoverageMerger>();

        // Reporting services
        services.AddSingleton<IMissingCoverageExtractor, MissingCoverageExtractor>();

        // Exporters
        services.AddSingleton<TableExporter>();
        services.AddSingleton<JsonExporter>();
        services.AddSingleton<MarkdownExporter>();

        return services;
    }
}
