using System.Text.Json;
using System.Text.Json.Serialization;
using CodeCoverageReporter.Cobertura.Paths;
using CodeCoverageReporter.Cobertura.Reporting;

namespace CodeCoverageReporter.Cobertura.Exporting;

/// <summary>
/// Exports missing coverage rows as JSON format.
/// </summary>
public sealed class JsonExporter : ICoverageExporter
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        WriteIndented = false
    };

    /// <inheritdoc />
    public string Export(IReadOnlyList<MissingCoverageRow> rows, int? limit = null, IPathTransformer? pathTransformer = null)
    {
        ArgumentNullException.ThrowIfNull(rows);

        var rowsToExport = limit.HasValue ? rows.Take(limit.Value).ToList() : rows;

        var jsonRows = rowsToExport.Select(row => ToJsonRow(row, pathTransformer)).ToList();
        return JsonSerializer.Serialize(jsonRows, JsonOptions);
    }

    private static JsonCoverageRow ToJsonRow(MissingCoverageRow row, IPathTransformer? pathTransformer)
    {
        var isBranch = row.BranchCoverage is not null;
        var filePath = pathTransformer?.Transform(row.File) ?? row.File;

        return new JsonCoverageRow
        {
            File = filePath,
            Class = row.Class,
            Method = row.Method,
            Lines = LineRangeFormatter.Format(row.LineNumbers),
            Hits = isBranch ? 0 : null,
            BranchCoverage = row.BranchCoverage,
            BranchConditions = row.BranchConditions
        };
    }

    private sealed class JsonCoverageRow
    {
        public required string File { get; init; }
        public required string Class { get; init; }
        public string? Method { get; init; }
        public required string Lines { get; init; }
        public int? Hits { get; init; }
        public string? BranchCoverage { get; init; }
        public string? BranchConditions { get; init; }
    }
}
