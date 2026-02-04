using System.Text;
using CodeCoverageReporter.Cobertura.Paths;
using CodeCoverageReporter.Cobertura.Reporting;

namespace CodeCoverageReporter.Cobertura.Exporting;

/// <summary>
/// Exports missing coverage rows as a pipe-separated table format.
/// </summary>
public sealed class TableExporter : ICoverageExporter
{
    private const string Header = "File|Class|Method|Lines|Hits|BranchCoverage|BranchConditions";

    /// <inheritdoc />
    public string Export(IReadOnlyList<MissingCoverageRow> rows, int? limit = null, IPathTransformer? pathTransformer = null)
    {
        ArgumentNullException.ThrowIfNull(rows);

        var sb = new StringBuilder();
        sb.AppendLine(Header);

        var rowsToExport = limit.HasValue ? rows.Take(limit.Value) : rows;

        foreach (var row in rowsToExport)
        {
            var filePath = pathTransformer?.Transform(row.File) ?? row.File;
            var line = string.Join("|",
                filePath,
                row.Class,
                row.Method ?? string.Empty,
                LineRangeFormatter.Format(row.LineNumbers),
                row.Hits,
                row.BranchCoverage ?? string.Empty,
                row.BranchConditions ?? string.Empty);

            sb.AppendLine(line);
        }

        return sb.ToString().TrimEnd();
    }
}
