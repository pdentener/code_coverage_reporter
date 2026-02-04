using System.Globalization;
using System.Text;
using CodeCoverageReporter.Cobertura.Paths;
using CodeCoverageReporter.Cobertura.Reporting;

namespace CodeCoverageReporter.Cobertura.Exporting;

/// <summary>
/// Exports missing coverage rows as a markdown table format.
/// </summary>
public sealed class MarkdownExporter : ICoverageExporter
{
    private const string Header = "| File | Class | Method | Lines | Hits | Branch Coverage | Branch Conditions |";
    private const string Separator = "|------|-------|--------|-------|------|-----------------|-------------------|";

    /// <inheritdoc />
    public string Export(IReadOnlyList<MissingCoverageRow> rows, int? limit = null, IPathTransformer? pathTransformer = null)
    {
        ArgumentNullException.ThrowIfNull(rows);

        var sb = new StringBuilder();
        sb.AppendLine(Header);
        sb.AppendLine(Separator);

        var rowsToExport = limit.HasValue ? rows.Take(limit.Value) : rows;

        foreach (var row in rowsToExport)
        {
            var filePath = pathTransformer?.Transform(row.File) ?? row.File;
            var line = string.Join(" | ",
                EscapePipe(filePath),
                EscapePipe(row.Class),
                EscapePipe(row.Method ?? string.Empty),
                EscapePipe(LineRangeFormatter.Format(row.LineNumbers)),
                row.Hits.ToString(CultureInfo.InvariantCulture),
                EscapePipe(row.BranchCoverage ?? string.Empty),
                EscapePipe(row.BranchConditions ?? string.Empty));

            sb.Append("| ");
            sb.Append(line);
            sb.AppendLine(" |");
        }

        return sb.ToString().TrimEnd();
    }

    private static string EscapePipe(string value)
    {
        return value.Replace("|", "\\|", StringComparison.Ordinal);
    }
}
