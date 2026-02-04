# CodeCoverageReporter.CLI

Command-line interface for the Code Coverage Reporter tool. Executable name: `cover`

## Directory Structure

```
├── Program.cs                    # Entry point, DI setup, command registration
├── Commands/
│   ├── DefaultCommand.cs         # Banner display and version info (-v/--version)
│   └── ReportCommand.cs          # Missing coverage report generation
└── Infrastructure/
    ├── IConsoleService.cs        # Console abstraction interface
    ├── ConsoleService.cs         # Spectre.Console implementation
    ├── TypeRegistrar.cs          # DI registrar for Spectre.Console.Cli
    └── TypeResolver.cs           # DI resolver for Spectre.Console.Cli
```

## Commands

| Command | Description | Status |
|---------|-------------|--------|
| (default) | Displays ASCII banner and version | Implemented |
| `report` | Generates missing coverage report from Cobertura XML | Implemented |

### Report Command

Generate a missing coverage report from Cobertura XML files.

```bash
cover report [files] [--limit N] [--output table|json|markdown] [--exclude <pattern>] [--verbose] [--absolute-paths] [--base-path <dir>]
```

**Arguments:**
- `[files]` - One or more Cobertura XML file paths or glob patterns

**Options:**
- `--limit N` - Maximum number of rows to output
- `--output table|json|markdown` - Output format (default: table)
- `--exclude <pattern>` - Glob patterns to exclude files (can be specified multiple times)
- `--verbose` - Show verbose processing information to stderr
- `--absolute-paths` - Show full absolute file paths instead of relative paths
- `--base-path <dir>` - Base directory for calculating relative paths (defaults to current directory)

**Path Display:**
By default, file paths are shown relative to the current working directory. Use `--absolute-paths` to display full absolute paths, or `--base-path` to specify a custom base directory for relative path calculation. Note: `--absolute-paths` and `--base-path` cannot be used together.

**Exit Codes:**
- `0` - Success
- `1` - Error (no files specified, file not found, invalid XML)

**Examples:**

```bash
# Process a single file
cover report coverage.xml

# Process multiple files (merged)
cover report coverage1.xml coverage2.xml

# Use glob patterns
cover report "coverage/*.xml"

# Limit output to 10 rows
cover report coverage.xml --limit 10

# Output as JSON
cover report coverage.xml --output json

# Verbose mode (processing info to stderr)
cover report coverage.xml --verbose

# Show absolute paths instead of relative
cover report coverage.xml --absolute-paths

# Use custom base path for relative paths
cover report coverage.xml --base-path ./src

# Output as markdown table
cover report coverage.xml --output markdown

# Exclude generated files and migrations
cover report coverage.xml --exclude "**/*.g.cs" --exclude "**/Migrations/*"
```

**Output Formats:**

Table format (default):
```
File|Class|Method|Lines|Hits|BranchCoverage|BranchConditions
TestFile.cs|MyClass|MyMethod|[10-12]|0||
BranchFile.cs|MyClass|BranchMethod|[15]|0|50% (1/2)|[0:jump 0%,1:jump 100%]
```

JSON format:
```json
[
  {"file":"TestFile.cs","class":"MyClass","method":"MyMethod","lines":"[10-12]"},
  {"file":"BranchFile.cs","class":"MyClass","method":"BranchMethod","lines":"[15]","hits":0,"branchCoverage":"50% (1/2)","branchConditions":"[0:jump 0%,1:jump 100%]"}
]
```

Markdown format:
```markdown
| File | Class | Method | Lines | Hits | Branch Coverage | Branch Conditions |
|------|-------|--------|-------|------|-----------------|-------------------|
| TestFile.cs | MyClass | MyMethod | [10-12] | 0 |  |  |
| BranchFile.cs | MyClass | BranchMethod | [15] | 0 | 50% (1/2) | [0:jump 0%,1:jump 100%] |
```

## Dependencies

| Package | Purpose |
|---------|---------|
| Spectre.Console | Rich console output (colors, ASCII art, markup) |
| Spectre.Console.Cli | CLI framework for command routing and help generation |
| Microsoft.Extensions.DependencyInjection | Dependency injection container |
| CodeCoverageReporter.Cobertura | Cobertura parsing, merging, and reporting |

## Architecture

- **Command Pattern** - Commands implement `Command<TSettings>` from Spectre.Console.Cli
- **Dependency Injection** - Microsoft.Extensions.DependencyInjection with custom adapters (TypeRegistrar/TypeResolver)
- **Testability** - IConsoleService abstraction enables unit testing without actual console output

## Testing

- Test project: `CodeCoverageReporter.CLI.Tests`
- InternalsVisibleTo configured for test access
- DynamicProxyGenAssembly2 configured for mocking support
- Integration tests with real Cobertura XML files in `TestData/`
