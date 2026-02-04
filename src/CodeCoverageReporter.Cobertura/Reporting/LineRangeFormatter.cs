using System.Globalization;

namespace CodeCoverageReporter.Cobertura.Reporting;

/// <summary>
/// Utility for formatting line numbers into collapsed range notation.
/// </summary>
public static class LineRangeFormatter
{
    /// <summary>
    /// Formats a list of line numbers into collapsed range notation.
    /// </summary>
    /// <param name="lineNumbers">The line numbers to format.</param>
    /// <returns>A string in the format "[start-end, n, start-end]" (e.g., "[42-45, 50, 60-62]").</returns>
    /// <example>
    /// Format([]) returns "[]"
    /// Format([42]) returns "[42]"
    /// Format([42, 43]) returns "[42-43]"
    /// Format([42, 44]) returns "[42, 44]"
    /// Format([10, 11, 12, 15, 20, 25, 26, 27]) returns "[10-12, 15, 20, 25-27]"
    /// </example>
    public static string Format(IReadOnlyList<int> lineNumbers)
    {
        ArgumentNullException.ThrowIfNull(lineNumbers);

        if (lineNumbers.Count == 0)
        {
            return "[]";
        }

        // Sort and deduplicate
        var sorted = lineNumbers.Distinct().OrderBy(n => n).ToList();

        var ranges = new List<string>();
        var rangeStart = sorted[0];
        var rangeEnd = sorted[0];

        for (var i = 1; i < sorted.Count; i++)
        {
            if (sorted[i] == rangeEnd + 1)
            {
                // Extend the current range
                rangeEnd = sorted[i];
            }
            else
            {
                // Flush the current range and start a new one
                ranges.Add(FormatRange(rangeStart, rangeEnd));
                rangeStart = sorted[i];
                rangeEnd = sorted[i];
            }
        }

        // Flush the final range
        ranges.Add(FormatRange(rangeStart, rangeEnd));

        return $"[{string.Join(", ", ranges)}]";
    }

    private static string FormatRange(int start, int end)
    {
        return start == end
            ? start.ToString(CultureInfo.InvariantCulture)
            : string.Create(CultureInfo.InvariantCulture, $"{start}-{end}");
    }
}
