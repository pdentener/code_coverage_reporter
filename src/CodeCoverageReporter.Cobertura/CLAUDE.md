# CodeCoverageReporter.Cobertura

Standalone class library for loading, parsing, and merging Cobertura XML coverage files into an efficient in-memory model.

## Directory Structure

```
├── Models/
│   ├── LineScope.cs              # Enum: Method vs Class scope
│   ├── BranchCondition.cs        # Branch condition data
│   ├── LineCoverage.cs           # Line-level coverage
│   ├── MethodCoverage.cs         # Method-level coverage
│   ├── ClassCoverage.cs          # Class-level coverage
│   ├── PackageCoverage.cs        # Package-level coverage
│   └── CoverageReport.cs         # Root coverage report
├── Parsing/
│   ├── ICoberturaParser.cs       # Parser interface
│   └── CoberturaParser.cs        # XML parsing implementation
├── IO/
│   ├── ICoverageFileReader.cs    # File reader interface
│   └── CoverageFileReader.cs     # File/glob resolution
├── Merging/
│   ├── ICoverageMerger.cs        # Merger interface
│   ├── CoverageMerger.cs         # Report merging logic
│   └── CoverageStatisticsCalculator.cs  # Statistics helpers
├── Reporting/
│   ├── MissingCoverageRow.cs     # Missing coverage row model
│   ├── IMissingCoverageExtractor.cs # Extractor interface
│   ├── MissingCoverageExtractor.cs  # Extraction with grouping logic
│   └── LineRangeFormatter.cs     # Line range formatting utility
├── Exporting/
│   ├── ICoverageExporter.cs      # Exporter interface
│   ├── TableExporter.cs          # Pipe-separated table format
│   ├── JsonExporter.cs           # JSON format
│   └── MarkdownExporter.cs       # Markdown table format
├── Paths/
│   ├── IPathTransformer.cs       # Path transformer interface
│   ├── PathTransformer.cs        # Relative path transformation
│   └── NullPathTransformer.cs    # Identity transformation (singleton)
├── ExtensionMethods/
│   └── ServiceCollectionExtensions.cs # DI registration
└── CoberturaException.cs         # Domain-specific exception
```

## Public API

### Parsing

```csharp
ICoberturaParser parser = new CoberturaParser();
CoverageReport report = parser.Parse(xmlStream);
CoverageReport report = parser.Parse(xmlString);
```

### File Resolution (with glob support)

```csharp
ICoverageFileReader reader = new CoverageFileReader();
IReadOnlyList<string> files = reader.ResolveFiles(["coverage/*.xml", "results.xml"]);
Stream stream = reader.OpenFile("coverage.xml");
```

### Merging Multiple Reports

```csharp
ICoverageMerger merger = new CoverageMerger();
CoverageReport merged = merger.Merge([report1, report2, report3]);
```

### Missing Coverage Extraction

```csharp
IMissingCoverageExtractor extractor = new MissingCoverageExtractor();
IReadOnlyList<MissingCoverageRow> rows = extractor.Extract(report);
```

Extraction logic:
- Groups consecutive uncovered non-branch lines into single rows
- Branch lines with incomplete coverage (< 100%) emit separate rows
- Sorted by File > Class > Method > LineNumber

### Path Transformation

```csharp
// Relative path transformation
IPathTransformer transformer = new PathTransformer("/base/path");
string relativePath = transformer.Transform("/base/path/src/file.cs"); // "src/file.cs"

// Identity transformation (no change)
IPathTransformer noop = NullPathTransformer.Instance;
```

### Exporting

```csharp
// Table format (pipe-separated)
ICoverageExporter tableExporter = new TableExporter();
string table = tableExporter.Export(rows, limit: 10, pathTransformer);

// JSON format
ICoverageExporter jsonExporter = new JsonExporter();
string json = jsonExporter.Export(rows, limit: 10, pathTransformer);

// Markdown table format
ICoverageExporter markdownExporter = new MarkdownExporter();
string markdown = markdownExporter.Export(rows, limit: 10, pathTransformer);
```

### Dependency Injection

```csharp
services.AddCobertura(); // Registers all Cobertura services
```

## Coverage Model Hierarchy

```
CoverageReport
  └── PackageCoverage (namespace/assembly)
        └── ClassCoverage (fully qualified name)
              ├── MethodCoverage (signature)
              │     └── LineCoverage (number + hits + branch info)
              └── ClassLines (lines not in any method)
```

Each level stores:
- Name/identifier
- Line rate (0.0-1.0)
- Branch rate (0.0-1.0)
- Complexity
- Total lines / covered lines

## Dependencies

| Package | Purpose |
|---------|---------|
| Microsoft.Extensions.DependencyInjection.Abstractions | DI service registration extensions |
| Microsoft.Extensions.FileSystemGlobbing | Cross-platform glob pattern matching |
| System.Text.Json (built-in) | JSON serialization |
| System.Xml.Linq (built-in) | LINQ to XML parsing |

## Architecture

- **Immutable Records** - All models are C# records with `IReadOnlyList<T>` collections
- **Interface Segregation** - Separate interfaces for parsing, file I/O, and merging
- **Fail-Fast** - `CoberturaException` thrown on invalid input or conflicts
- **Case-Insensitive Booleans** - Handles `branch="True"`, `branch="true"`, `branch="TRUE"`

## Key Design Decisions

1. **Rates stored as 0.0-1.0** - Conversion to percentage is a presentation concern
2. **Line coverage from hits** - `Hits > 0` = covered, `Hits == 0` = uncovered
3. **ClassLines separate from Methods** - Cobertura allows `<lines>` under `<class>` outside methods
4. **Strict merge mode** - Fails on conflicts (different filenames, branch flag mismatches)
5. **File path resolution** - Resolves `filename` against `<sources>` when possible

## Testing

- Test project: `CodeCoverageReporter.Cobertura.Tests`
- InternalsVisibleTo configured for test access
- DynamicProxyGenAssembly2 configured for NSubstitute mocking
- 185 tests

### Test Organization

```
├── SmokeTests.cs                           # Basic model instantiation
├── Parsing/CoberturaParserTests.cs         # XML parsing tests
├── IO/CoverageFileReaderTests.cs           # File/glob resolution tests
├── Reporting/
│   ├── MissingCoverageExtractorTests.cs    # Extraction logic tests
│   └── LineRangeFormatterTests.cs          # Line range formatting tests
├── Exporting/
│   ├── TableExporterTests.cs               # Table format export tests
│   ├── JsonExporterTests.cs                # JSON format export tests
│   └── MarkdownExporterTests.cs            # Markdown format export tests
├── Merging/
│   ├── CoverageMergerTests.cs              # Report merging tests
│   └── CoverageStatisticsCalculatorTests.cs # Statistics calculation tests
└── Paths/PathTransformerTests.cs           # Path transformation tests
```

## Error Handling

All errors throw `CoberturaException` with descriptive messages:
- Missing files: `"File not found: {path}"`
- No glob matches: `"No files matched the pattern: {pattern}"`
- Invalid XML: `"Invalid Cobertura XML: missing 'coverage' root element."`
- Merge conflicts: `"Cannot merge class '{name}': conflicting file paths"`
