using System.Globalization;
using System.Xml.Linq;
using CodeCoverageReporter.Cobertura.Models;

namespace CodeCoverageReporter.Cobertura.Parsing;

/// <summary>
/// Parses Cobertura XML coverage reports into the in-memory model.
/// </summary>
public sealed class CoberturaParser : ICoberturaParser
{
    /// <inheritdoc />
    public CoverageReport Parse(Stream xmlStream)
    {
        var doc = XDocument.Load(xmlStream);
        return ParseDocument(doc);
    }

    /// <inheritdoc />
    public CoverageReport Parse(string xmlContent)
    {
        var doc = XDocument.Parse(xmlContent);
        return ParseDocument(doc);
    }

    private static CoverageReport ParseDocument(XDocument doc)
    {
        var coverage = doc.Element("coverage")
            ?? throw new InvalidOperationException("Invalid Cobertura XML: missing 'coverage' root element.");

        var sources = ParseSources(coverage);
        var packages = ParsePackages(coverage, sources);

        // Calculate totals from packages
        var totalLines = packages.Sum(p => p.TotalLines);
        var coveredLines = packages.Sum(p => p.CoveredLines);

        return new CoverageReport(
            Packages: packages,
            Sources: sources,
            LineRate: ParseDoubleAttribute(coverage, "line-rate", 0.0),
            BranchRate: ParseDoubleAttribute(coverage, "branch-rate", 0.0),
            Complexity: ParseIntAttribute(coverage, "complexity", 0),
            Timestamp: ParseLongAttribute(coverage, "timestamp", 0),
            Version: coverage.Attribute("version")?.Value ?? "",
            LinesCovered: ParseIntAttribute(coverage, "lines-covered", coveredLines),
            LinesValid: ParseIntAttribute(coverage, "lines-valid", totalLines),
            BranchesCovered: ParseIntAttribute(coverage, "branches-covered", 0),
            BranchesValid: ParseIntAttribute(coverage, "branches-valid", 0),
            TotalLines: totalLines,
            CoveredLines: coveredLines
        );
    }

    private static List<string> ParseSources(XElement coverage)
    {
        var sourcesElement = coverage.Element("sources");
        if (sourcesElement is null)
        {
            return [];
        }

        return sourcesElement
            .Elements("source")
            .Select(s => s.Value)
            .Where(s => !string.IsNullOrWhiteSpace(s))
            .ToList();
    }

    private static List<PackageCoverage> ParsePackages(XElement coverage, List<string> sources)
    {
        var packagesElement = coverage.Element("packages");
        if (packagesElement is null)
        {
            return [];
        }

        return packagesElement
            .Elements("package")
            .Select(p => ParsePackage(p, sources))
            .ToList();
    }

    private static PackageCoverage ParsePackage(XElement packageElement, List<string> sources)
    {
        var classes = ParseClasses(packageElement, sources);

        var totalLines = classes.Sum(c => c.TotalLines);
        var coveredLines = classes.Sum(c => c.CoveredLines);

        return new PackageCoverage(
            Name: packageElement.Attribute("name")?.Value ?? "",
            Classes: classes,
            LineRate: ParseDoubleAttribute(packageElement, "line-rate", 0.0),
            BranchRate: ParseDoubleAttribute(packageElement, "branch-rate", 0.0),
            Complexity: ParseIntAttribute(packageElement, "complexity", 0),
            TotalLines: totalLines,
            CoveredLines: coveredLines
        );
    }

    private static List<ClassCoverage> ParseClasses(XElement packageElement, List<string> sources)
    {
        var classesElement = packageElement.Element("classes");
        if (classesElement is null)
        {
            return [];
        }

        return classesElement
            .Elements("class")
            .Select(c => ParseClass(c, sources))
            .ToList();
    }

    private static ClassCoverage ParseClass(XElement classElement, List<string> sources)
    {
        var rawFilename = classElement.Attribute("filename")?.Value;
        var filePath = ResolveFilePath(rawFilename, sources);

        var methods = ParseMethods(classElement, filePath);

        // Get line numbers that belong to methods
        var methodLineNumbers = methods
            .SelectMany(m => m.Lines)
            .Select(l => l.Number)
            .ToHashSet();

        // Parse class-level lines (lines not in any method)
        var classLines = ParseClassLines(classElement, filePath, methodLineNumbers);

        var totalMethodLines = methods.Sum(m => m.TotalLines);
        var coveredMethodLines = methods.Sum(m => m.CoveredLines);

        var totalLines = totalMethodLines + classLines.Count;
        var coveredLines = coveredMethodLines + classLines.Count(l => l.Hits > 0);

        return new ClassCoverage(
            Name: classElement.Attribute("name")?.Value ?? "",
            FilePath: filePath,
            Methods: methods,
            ClassLines: classLines,
            LineRate: ParseDoubleAttribute(classElement, "line-rate", 0.0),
            BranchRate: ParseDoubleAttribute(classElement, "branch-rate", 0.0),
            Complexity: ParseIntAttribute(classElement, "complexity", 0),
            TotalLines: totalLines,
            CoveredLines: coveredLines
        );
    }

    private static List<MethodCoverage> ParseMethods(XElement classElement, string? filePath)
    {
        var methodsElement = classElement.Element("methods");
        if (methodsElement is null)
        {
            return [];
        }

        return methodsElement
            .Elements("method")
            .Select(m => ParseMethod(m, filePath))
            .ToList();
    }

    private static MethodCoverage ParseMethod(XElement methodElement, string? filePath)
    {
        var lines = ParseLines(methodElement, filePath, LineScope.Method);

        var totalLines = lines.Count;
        var coveredLines = lines.Count(l => l.Hits > 0);

        return new MethodCoverage(
            Name: methodElement.Attribute("name")?.Value ?? "",
            Signature: methodElement.Attribute("signature")?.Value ?? "",
            Lines: lines,
            LineRate: ParseDoubleAttribute(methodElement, "line-rate", 0.0),
            BranchRate: ParseDoubleAttribute(methodElement, "branch-rate", 0.0),
            Complexity: ParseIntAttribute(methodElement, "complexity", 0),
            TotalLines: totalLines,
            CoveredLines: coveredLines
        );
    }

    private static List<LineCoverage> ParseClassLines(
        XElement classElement,
        string? filePath,
        HashSet<int> methodLineNumbers)
    {
        var linesElement = classElement.Element("lines");
        if (linesElement is null)
        {
            return [];
        }

        return linesElement
            .Elements("line")
            .Select(l => ParseLine(l, filePath, LineScope.Class))
            .Where(l => !methodLineNumbers.Contains(l.Number))
            .ToList();
    }

    private static List<LineCoverage> ParseLines(XElement parent, string? filePath, LineScope scope)
    {
        var linesElement = parent.Element("lines");
        if (linesElement is null)
        {
            return [];
        }

        return linesElement
            .Elements("line")
            .Select(l => ParseLine(l, filePath, scope))
            .ToList();
    }

    private static LineCoverage ParseLine(XElement lineElement, string? filePath, LineScope scope)
    {
        var isBranch = ParseBoolAttribute(lineElement, "branch", false);
        var conditionCoverage = lineElement.Attribute("condition-coverage")?.Value;
        var conditions = ParseConditions(lineElement);

        return new LineCoverage(
            Number: ParseIntAttribute(lineElement, "number", 0),
            Hits: ParseIntAttribute(lineElement, "hits", 0),
            IsBranch: isBranch,
            ConditionCoverage: conditionCoverage,
            Conditions: conditions,
            FilePath: filePath,
            Scope: scope
        );
    }

    private static List<BranchCondition> ParseConditions(XElement lineElement)
    {
        var conditionsElement = lineElement.Element("conditions");
        if (conditionsElement is null)
        {
            return [];
        }

        return conditionsElement
            .Elements("condition")
            .Select(ParseCondition)
            .ToList();
    }

    private static BranchCondition ParseCondition(XElement conditionElement)
    {
        return new BranchCondition(
            Number: ParseIntAttribute(conditionElement, "number", 0),
            Type: conditionElement.Attribute("type")?.Value ?? "",
            Coverage: conditionElement.Attribute("coverage")?.Value ?? ""
        );
    }

    private static string? ResolveFilePath(string? rawFilename, List<string> sources)
    {
        if (string.IsNullOrEmpty(rawFilename))
        {
            return null;
        }

        // If it's already an absolute path, return as-is
        if (Path.IsPathRooted(rawFilename))
        {
            return rawFilename;
        }

        // Try to resolve against sources
        foreach (var source in sources)
        {
            var combinedPath = Path.Combine(source, rawFilename);
            if (File.Exists(combinedPath))
            {
                return combinedPath;
            }
        }

        // Fall back to raw filename
        return rawFilename;
    }

    /// <summary>
    /// Parses a boolean attribute case-insensitively.
    /// </summary>
    private static bool ParseBoolAttribute(XElement element, string attributeName, bool defaultValue)
    {
        var value = element.Attribute(attributeName)?.Value;
        if (string.IsNullOrEmpty(value))
        {
            return defaultValue;
        }

        return value.Equals("true", StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Parses a double attribute.
    /// </summary>
    private static double ParseDoubleAttribute(XElement element, string attributeName, double defaultValue)
    {
        var value = element.Attribute(attributeName)?.Value;
        if (string.IsNullOrEmpty(value))
        {
            return defaultValue;
        }

        return double.TryParse(value, CultureInfo.InvariantCulture, out var result)
            ? result
            : defaultValue;
    }

    /// <summary>
    /// Parses an integer attribute.
    /// </summary>
    private static int ParseIntAttribute(XElement element, string attributeName, int defaultValue)
    {
        var value = element.Attribute(attributeName)?.Value;
        if (string.IsNullOrEmpty(value))
        {
            return defaultValue;
        }

        return int.TryParse(value, CultureInfo.InvariantCulture, out var result)
            ? result
            : defaultValue;
    }

    /// <summary>
    /// Parses a long attribute.
    /// </summary>
    private static long ParseLongAttribute(XElement element, string attributeName, long defaultValue)
    {
        var value = element.Attribute(attributeName)?.Value;
        if (string.IsNullOrEmpty(value))
        {
            return defaultValue;
        }

        return long.TryParse(value, CultureInfo.InvariantCulture, out var result)
            ? result
            : defaultValue;
    }
}
