using CodeCoverageReporter.Cobertura.Models;

namespace CodeCoverageReporter.Cobertura.Reporting;

/// <summary>
/// Extracts missing coverage rows from a coverage report with row grouping logic.
/// </summary>
public sealed class MissingCoverageExtractor : IMissingCoverageExtractor
{
    /// <inheritdoc />
    public IReadOnlyList<MissingCoverageRow> Extract(CoverageReport report)
    {
        ArgumentNullException.ThrowIfNull(report);

        var rows = new List<MissingCoverageRow>();

        foreach (var package in report.Packages)
        {
            foreach (var cls in package.Classes)
            {
                var filePath = cls.FilePath ?? string.Empty;
                var className = cls.Name;

                // Process ClassLines (lines not in any method)
                ExtractFromLines(cls.ClassLines, filePath, className, methodName: null, rows);

                // Process Methods
                foreach (var method in cls.Methods)
                {
                    ExtractFromLines(method.Lines, filePath, className, method.Name, rows);
                }
            }
        }

        return SortRows(rows);
    }

    private static List<MissingCoverageRow> SortRows(List<MissingCoverageRow> rows)
    {
        // Sort by File, Class, Method, first LineNumber
        return rows
            .OrderBy(r => r.File, StringComparer.Ordinal)
            .ThenBy(r => r.Class, StringComparer.Ordinal)
            .ThenBy(r => r.Method ?? string.Empty, StringComparer.Ordinal)
            .ThenBy(r => r.LineNumbers[0])
            .ToList();
    }

    private static void ExtractFromLines(
        IReadOnlyList<LineCoverage> lines,
        string filePath,
        string className,
        string? methodName,
        List<MissingCoverageRow> rows)
    {
        var buffer = new List<int>();

        // Sort lines by line number for proper grouping
        var sortedLines = lines.OrderBy(l => l.Number).ToList();

        foreach (var line in sortedLines)
        {
            bool isMissing = IsMissingCoverage(line);
            bool isBranchWithMissingCoverage = line.IsBranch && HasIncompleteBranchCoverage(line);

            if (isBranchWithMissingCoverage)
            {
                // Flush any accumulated non-branch lines first
                if (buffer.Count > 0)
                {
                    rows.Add(CreateNonBranchRow(filePath, className, methodName, buffer));
                    buffer.Clear();
                }

                // Emit branch line as its own row
                rows.Add(CreateBranchRow(filePath, className, methodName, line));
            }
            else if (isMissing)
            {
                // Accumulate non-branch uncovered lines
                buffer.Add(line.Number);
            }
        }

        // Flush remaining buffer at method/class boundary
        if (buffer.Count > 0)
        {
            rows.Add(CreateNonBranchRow(filePath, className, methodName, buffer));
        }
    }

    private static bool IsMissingCoverage(LineCoverage line)
    {
        // A line has missing coverage if:
        // 1. Hits == 0 (uncovered line), OR
        // 2. IsBranch == true AND branch coverage < 100%
        if (line.Hits == 0)
        {
            return true;
        }

        if (line.IsBranch && HasIncompleteBranchCoverage(line))
        {
            return true;
        }

        return false;
    }

    private static bool HasIncompleteBranchCoverage(LineCoverage line)
    {
        // Check if any condition has coverage that is not 100%
        if (line.Conditions.Count > 0)
        {
            return line.Conditions.Any(c => !c.Coverage.Equals("100%", StringComparison.OrdinalIgnoreCase));
        }

        // Fall back to parsing ConditionCoverage string (e.g., "50% (1/2)")
        if (!string.IsNullOrEmpty(line.ConditionCoverage))
        {
            return !line.ConditionCoverage.StartsWith("100%", StringComparison.OrdinalIgnoreCase);
        }

        // If it's marked as a branch but has no condition info, treat as unknown (incomplete)
        return line.IsBranch;
    }

    private static MissingCoverageRow CreateNonBranchRow(
        string filePath,
        string className,
        string? methodName,
        List<int> lineNumbers)
    {
        return new MissingCoverageRow(
            File: filePath,
            Class: className,
            Method: methodName,
            LineNumbers: lineNumbers.ToList(),
            Hits: 0,
            BranchCoverage: null,
            BranchConditions: null
        );
    }

    private static MissingCoverageRow CreateBranchRow(
        string filePath,
        string className,
        string? methodName,
        LineCoverage line)
    {
        var branchCoverage = line.ConditionCoverage ?? "unknown";
        var branchConditions = FormatBranchConditions(line.Conditions);

        return new MissingCoverageRow(
            File: filePath,
            Class: className,
            Method: methodName,
            LineNumbers: [line.Number],
            Hits: 0,
            BranchCoverage: branchCoverage,
            BranchConditions: branchConditions
        );
    }

    private static string? FormatBranchConditions(IReadOnlyList<BranchCondition> conditions)
    {
        if (conditions.Count == 0)
        {
            return null;
        }

        var formatted = conditions.Select(c => $"{c.Number}:{c.Type} {c.Coverage}");
        return $"[{string.Join(",", formatted)}]";
    }
}
