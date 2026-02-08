using CodeCoverageReporter.Cobertura.Models;

namespace CodeCoverageReporter.Cobertura.Merging;

/// <summary>
/// Merges multiple Cobertura coverage reports into a single unified model.
/// </summary>
public sealed class CoverageMerger : ICoverageMerger
{
    /// <inheritdoc />
    public CoverageReport Merge(IEnumerable<CoverageReport> reports)
    {
        ArgumentNullException.ThrowIfNull(reports);

        var reportList = reports.ToList();

        if (reportList.Count == 0)
        {
            return CreateEmptyReport();
        }

        if (reportList.Count == 1)
        {
            return reportList[0];
        }

        // Merge all sources (deduplicated)
        var mergedSources = reportList
            .SelectMany(r => r.Sources)
            .Distinct()
            .ToList();

        // Merge packages by name
        var mergedPackages = MergePackages(reportList);

        // Calculate totals
        var totalLines = mergedPackages.Sum(p => p.TotalLines);
        var coveredLines = mergedPackages.Sum(p => p.CoveredLines);

        // Use max values for metadata from all reports
        var maxLineRate = CalculateOverallLineRate(mergedPackages);
        var maxBranchRate = CalculateOverallBranchRate(mergedPackages);
        var totalComplexity = mergedPackages.Sum(p => p.Complexity);

        return new CoverageReport(
            Packages: mergedPackages,
            Sources: mergedSources,
            LineRate: maxLineRate,
            BranchRate: maxBranchRate,
            Complexity: totalComplexity,
            Timestamp: reportList.Max(r => r.Timestamp),
            Version: reportList.First().Version,
            LinesCovered: coveredLines,
            LinesValid: totalLines,
            BranchesCovered: 0, // Recalculated if needed
            BranchesValid: 0,   // Recalculated if needed
            TotalLines: totalLines,
            CoveredLines: coveredLines
        );
    }

    private static CoverageReport CreateEmptyReport()
    {
        return new CoverageReport(
            Packages: [],
            Sources: [],
            LineRate: 0.0,
            BranchRate: 0.0,
            Complexity: 0,
            Timestamp: 0,
            Version: "",
            LinesCovered: 0,
            LinesValid: 0,
            BranchesCovered: 0,
            BranchesValid: 0,
            TotalLines: 0,
            CoveredLines: 0
        );
    }

    private static List<PackageCoverage> MergePackages(List<CoverageReport> reports)
    {
        var packagesByName = new Dictionary<string, List<PackageCoverage>>(StringComparer.Ordinal);

        foreach (var report in reports)
        {
            foreach (var package in report.Packages)
            {
                if (!packagesByName.TryGetValue(package.Name, out var packages))
                {
                    packages = [];
                    packagesByName[package.Name] = packages;
                }
                packages.Add(package);
            }
        }

        return packagesByName
            .Select(kvp => MergePackageGroup(kvp.Key, kvp.Value))
            .ToList();
    }

    private static PackageCoverage MergePackageGroup(string name, List<PackageCoverage> packages)
    {
        if (packages.Count == 1)
        {
            return packages[0];
        }

        var mergedClasses = MergeClasses(packages);

        var totalLines = mergedClasses.Sum(c => c.TotalLines);
        var coveredLines = mergedClasses.Sum(c => c.CoveredLines);
        var complexity = mergedClasses.Sum(c => c.Complexity);

        return new PackageCoverage(
            Name: name,
            Classes: mergedClasses,
            LineRate: totalLines > 0 ? (double)coveredLines / totalLines : 0.0,
            BranchRate: 0.0, // Could be calculated from class branch rates
            Complexity: complexity,
            TotalLines: totalLines,
            CoveredLines: coveredLines
        );
    }

    private static List<ClassCoverage> MergeClasses(List<PackageCoverage> packages)
    {
        var classesByKey = new Dictionary<string, List<ClassCoverage>>(StringComparer.Ordinal);

        foreach (var package in packages)
        {
            foreach (var classItem in package.Classes)
            {
                var key = $"{classItem.Name}|{classItem.FilePath ?? ""}";
                if (!classesByKey.TryGetValue(key, out var classes))
                {
                    classes = [];
                    classesByKey[key] = classes;
                }
                classes.Add(classItem);
            }
        }

        return classesByKey
            .Select(kvp => MergeClassGroup(kvp.Value[0].Name, kvp.Value))
            .ToList();
    }

    private static ClassCoverage MergeClassGroup(string name, List<ClassCoverage> classes)
    {
        if (classes.Count == 1)
        {
            return classes[0];
        }

        var filePath = classes[0].FilePath;
        var mergedMethods = MergeMethods(classes);
        var mergedClassLines = MergeClassLines(classes);

        var totalMethodLines = mergedMethods.Sum(m => m.TotalLines);
        var coveredMethodLines = mergedMethods.Sum(m => m.CoveredLines);
        var totalLines = totalMethodLines + mergedClassLines.Count;
        var coveredLines = coveredMethodLines + mergedClassLines.Count(l => l.Hits > 0);
        var complexity = mergedMethods.Sum(m => m.Complexity);

        return new ClassCoverage(
            Name: name,
            FilePath: filePath,
            Methods: mergedMethods,
            ClassLines: mergedClassLines,
            LineRate: totalLines > 0 ? (double)coveredLines / totalLines : 0.0,
            BranchRate: 0.0,
            Complexity: complexity,
            TotalLines: totalLines,
            CoveredLines: coveredLines
        );
    }

    private static List<MethodCoverage> MergeMethods(List<ClassCoverage> classes)
    {
        // Group methods by name + signature
        var methodsByKey = new Dictionary<string, List<MethodCoverage>>(StringComparer.Ordinal);

        foreach (var classItem in classes)
        {
            foreach (var method in classItem.Methods)
            {
                var key = $"{method.Name}|{method.Signature}";
                if (!methodsByKey.TryGetValue(key, out var methods))
                {
                    methods = [];
                    methodsByKey[key] = methods;
                }
                methods.Add(method);
            }
        }

        return methodsByKey
            .Select(kvp => MergeMethodGroup(kvp.Value))
            .ToList();
    }

    private static MethodCoverage MergeMethodGroup(List<MethodCoverage> methods)
    {
        if (methods.Count == 1)
        {
            return methods[0];
        }

        var first = methods[0];
        var mergedLines = MergeLines(methods.SelectMany(m => m.Lines).ToList());

        var totalLines = mergedLines.Count;
        var coveredLines = mergedLines.Count(l => l.Hits > 0);

        return new MethodCoverage(
            Name: first.Name,
            Signature: first.Signature,
            Lines: mergedLines,
            LineRate: totalLines > 0 ? (double)coveredLines / totalLines : 0.0,
            BranchRate: CoverageStatisticsCalculator.CalculateBranchRate(mergedLines),
            Complexity: methods.Max(m => m.Complexity),
            TotalLines: totalLines,
            CoveredLines: coveredLines
        );
    }

    private static List<LineCoverage> MergeClassLines(List<ClassCoverage> classes)
    {
        var allClassLines = classes.SelectMany(c => c.ClassLines).ToList();
        return MergeLines(allClassLines);
    }

    private static List<LineCoverage> MergeLines(List<LineCoverage> lines)
    {
        // Group lines by number and scope
        var linesByKey = new Dictionary<string, List<LineCoverage>>(StringComparer.Ordinal);

        foreach (var line in lines)
        {
            var key = $"{line.Number}|{line.Scope}";
            if (!linesByKey.TryGetValue(key, out var lineGroup))
            {
                lineGroup = [];
                linesByKey[key] = lineGroup;
            }
            lineGroup.Add(line);
        }

        return linesByKey
            .Select(kvp => MergeLineGroup(kvp.Value))
            .OrderBy(l => l.Number)
            .ToList();
    }

    private static LineCoverage MergeLineGroup(List<LineCoverage> lines)
    {
        if (lines.Count == 1)
        {
            return lines[0];
        }

        var first = lines[0];

        // Check for branch flag conflicts
        var branchFlags = lines.Select(l => l.IsBranch).Distinct().ToList();
        if (branchFlags.Count > 1)
        {
            throw new CoberturaException(
                $"Cannot merge line {first.Number}: conflicting branch flags");
        }

        // Sum hits across reports
        var totalHits = lines.Sum(l => l.Hits);

        // Merge conditions
        var mergedConditions = MergeConditions(lines);

        // Use the best condition coverage (or null if any is null but branch=true)
        string? conditionCoverage = null;
        if (first.IsBranch)
        {
            var coverages = lines
                .Select(l => l.ConditionCoverage)
                .Where(c => c is not null)
                .ToList();

            conditionCoverage = coverages.Count > 0 ? coverages.First() : null;
        }

        return new LineCoverage(
            Number: first.Number,
            Hits: totalHits,
            IsBranch: first.IsBranch,
            ConditionCoverage: conditionCoverage,
            Conditions: mergedConditions,
            FilePath: first.FilePath,
            Scope: first.Scope
        );
    }

    private static List<BranchCondition> MergeConditions(List<LineCoverage> lines)
    {
        // Group conditions by number
        var conditionsByNumber = new Dictionary<int, List<BranchCondition>>();

        foreach (var line in lines)
        {
            foreach (var condition in line.Conditions)
            {
                if (!conditionsByNumber.TryGetValue(condition.Number, out var conditions))
                {
                    conditions = [];
                    conditionsByNumber[condition.Number] = conditions;
                }
                conditions.Add(condition);
            }
        }

        return conditionsByNumber
            .Select(kvp => MergeConditionGroup(kvp.Value))
            .OrderBy(c => c.Number)
            .ToList();
    }

    private static BranchCondition MergeConditionGroup(List<BranchCondition> conditions)
    {
        if (conditions.Count == 1)
        {
            return conditions[0];
        }

        var first = conditions[0];

        // Use the max coverage percentage
        var maxCoverage = conditions
            .Select(c => CoverageStatisticsCalculator.ParseCoveragePercent(c.Coverage))
            .Max();

        return new BranchCondition(
            Number: first.Number,
            Type: first.Type,
            Coverage: $"{maxCoverage}%"
        );
    }

    private static double CalculateOverallLineRate(List<PackageCoverage> packages)
    {
        var totalLines = packages.Sum(p => p.TotalLines);
        var coveredLines = packages.Sum(p => p.CoveredLines);
        return totalLines > 0 ? (double)coveredLines / totalLines : 0.0;
    }

    private static double CalculateOverallBranchRate(List<PackageCoverage> packages)
    {
        // For now, return 0.0 - proper branch rate calculation would require
        // aggregating all branch conditions across all packages
        return 0.0;
    }
}
