# Code Coverage Reporter CLI

A .NET 10 command-line tool that analyzes Cobertura XML code coverage reports and identifies missing coverage. It generates compact, token-efficient output optimized for LLM consumption, making it ideal for AI-assisted code review and automated coverage analysis pipelines.

## Why This Tool Exists

I created this tool because my AI coding agent (Claude Code) couldn't read in all the Cobertura XML coverage files due to token limits. Instead, it started generating custom Python scripts to parse the XML — which took a long time, consumed a lot of tokens, and ultimately didn't produce the coverage information I was looking for.

That's the reason for this optimized CLI. After building it and creating a proper agent skill file, the code coverage analysis I needed suddenly wasn't a problem at all anymore. The agent can now execute a single command and receive a concise, structured report of exactly what needs testing — ready for immediate action.

### Quick Example

```bash
cover report ./coverage/**/coverage.cobertura.xml
```

| File | Class | Method | Lines | Hits | Branch Coverage | Branch Conditions |
|------|-------|--------|-------|------|-----------------|-------------------|
| src/Services/UserService.cs | UserService | ValidateUser | [45-48] | 0 |  |  |
| src/Services/UserService.cs | UserService | ProcessLogin | [62] | 0 | 50% (1/2) | [0:jump 50%] |
| src/Controllers/AuthController.cs | AuthController | Login | [23,25-27] | 0 |  |  |

The output is token-efficient, immediately actionable, and works seamlessly in agent pipelines. No parsing code required — just results.

## Features

- Parse and analyze Cobertura XML coverage reports
- Merge multiple coverage files into a single report
- Identify uncovered lines and incomplete branch coverage
- Group consecutive uncovered lines for cleaner output
- Output in table, JSON, or markdown format
- Support for glob patterns to process multiple files
- Relative or absolute file path display

---

# Part 1: Using the CLI

## Installation

### As a .NET Global Tool (Recommended)

```bash
dotnet tool install -g CodeCoverageReporterCLI
```

After installation, the `cover` command is available globally:

```bash
cover report ./coverage/**/coverage.cobertura.xml --output markdown
```

To update to the latest version:

```bash
dotnet tool update -g CodeCoverageReporterCLI
```

To uninstall:

```bash
dotnet tool uninstall -g CodeCoverageReporterCLI
```

## CLI Usage

### Display Version

```bash
cover
cover -v
```

### Generate Coverage Report

```bash
cover report <files> [options]
```

**Arguments:**

| Argument | Description |
|----------|-------------|
| `<files>` | One or more Cobertura XML file paths or glob patterns |

**Options:**

| Option | Description |
|--------|-------------|
| `--limit <N>` | Maximum number of rows to output |
| `--output <format>` | Output format: `table` (default), `json`, or `markdown` |
| `--exclude <pattern>` | Glob patterns to exclude files (can be specified multiple times) |
| `--verbose` | Show processing information to stderr |
| `--absolute-paths` | Display full absolute file paths |
| `--base-path <dir>` | Base directory for calculating relative paths |
| `-h, --help` | Show help information |

### Examples

```bash
# Process a single coverage file
cover report coverage.cobertura.xml

# Process multiple files (results are merged)
cover report coverage1.xml coverage2.xml

# Use glob patterns
cover report "coverage/*.xml"
cover report "**/coverage.cobertura.xml"

# Limit output to first 10 uncovered items
cover report coverage.xml --limit 10

# Output as JSON (useful for CI/CD pipelines)
cover report coverage.xml --output json

# Verbose mode (shows file processing info on stderr)
cover report coverage.xml --verbose

# Show absolute paths instead of relative
cover report coverage.xml --absolute-paths

# Calculate relative paths from a specific directory
cover report coverage.xml --base-path ./src

# Output as markdown table
cover report coverage.xml --output markdown

# Exclude generated files and migrations
cover report coverage.xml --exclude "**/*.g.cs" --exclude "**/Migrations/*"
```

## Output Formats

### Table Format (Default)

```
File|Class|Method|Lines|Hits|BranchCoverage|BranchConditions
src/Services/UserService.cs|UserService|ValidateUser|[45-48]|0||
src/Services/UserService.cs|UserService|ProcessLogin|[62]|0|50% (1/2)|[0:jump 50%]
src/Controllers/AuthController.cs|AuthController|Login|[23,25-27]|0||
```

**Columns:**

| Column | Description |
|--------|-------------|
| File | Source file path (relative by default) |
| Class | Fully qualified class name |
| Method | Method name (empty for class-level lines) |
| Lines | Uncovered line numbers (`[10]`, `[10-12]`, or `[10,15-17]`) |
| Hits | Execution count (0 for uncovered) |
| BranchCoverage | Branch coverage percentage (for branch lines) |
| BranchConditions | Individual branch condition details |

### JSON Format

```json
[
  {
    "file": "src/Services/UserService.cs",
    "class": "UserService",
    "method": "ValidateUser",
    "lines": "[45-48]"
  },
  {
    "file": "src/Services/UserService.cs",
    "class": "UserService",
    "method": "ProcessLogin",
    "lines": "[62]",
    "branchCoverage": "50% (1/2)",
    "branchConditions": "[0:jump 50%]"
  }
]
```

### Markdown Format

```markdown
| File | Class | Method | Lines | Hits | Branch Coverage | Branch Conditions |
|------|-------|--------|-------|------|-----------------|-------------------|
| src/Services/UserService.cs | UserService | ValidateUser | [45-48] | 0 |  |  |
| src/Services/UserService.cs | UserService | ProcessLogin | [62] | 0 | 50% (1/2) | [0:jump 50%] |
| src/Controllers/AuthController.cs | AuthController | Login | [23,25-27] | 0 |  |  |
```

### Full Coverage

When all code is covered:

```
Code coverage OK
```

## Exit Codes

| Code | Meaning |
|------|---------|
| 0 | Success (includes when all code is fully covered) |
| 1 | Error (no files specified, file not found, invalid XML, no files matched glob pattern) |

## Agent Skill Setup

This CLI is designed to be invoked by AI coding agents as part of an automated coverage analysis workflow. To enable this, you can set up a **skill** — a small configuration that teaches the agent when and how to run the CLI.

The repository includes a ready-made skill in `.claude/skills/get-code-coverage/` that you can use directly or adapt for your own agent setup.

### Skill Structure

```
.claude/skills/get-code-coverage/
├── SKILL.md                          # Skill definition (metadata + instructions)
└── scripts/
    └── get-code-coverage.sh          # Shell script the agent executes
```

### How It Works

The skill consists of two parts:

**1. Skill definition (`SKILL.md`)**

A markdown file with YAML front matter that tells the agent what this skill does and when to use it. It has three fields:

- `name` — identifier for the skill
- `description` — tells the agent when to invoke it (e.g. when the user asks about uncovered code, coverage gaps, or missing tests)
- `allowed-tools` — scopes which tools the agent may use (in this case, only the specific shell script)

The markdown body below the front matter provides the agent with instructions: what the skill does and how to run the script. See the included [SKILL.md](.claude/skills/get-code-coverage/SKILL.md) for the full example.

**2. Shell script (`scripts/get-code-coverage.sh`)**

The script that performs the actual work:

```bash
#!/usr/bin/env bash

REPO_ROOT="$(git rev-parse --show-toplevel)"

# Delete any existing code coverage results.
rm -rf "$REPO_ROOT/src/code_coverage" > /dev/null 2>&1

# Run tests with code coverage collection.
dotnet test "$REPO_ROOT/src" --collect:"XPlat Code Coverage" --results-directory "$REPO_ROOT/src/code_coverage" > /dev/null 2>&1

# Generate missing code coverage information.
cover report "$REPO_ROOT"/src/code_coverage/**/coverage.cobertura.xml
```

It runs the test suite with coverage collection, then feeds the results into the CLI. The agent receives only the final coverage report output — a compact summary of uncovered lines ready for action.

### Setting Up the Skill in Your Project

1. **Install the CLI** (see [Installation](#installation) above).

2. **Copy the skill directory** into your project's agent configuration folder. For Claude Code, this is `.claude/skills/`:

   ```bash
   # From within your project root
   mkdir -p .claude/skills/get-code-coverage/scripts
   ```

3. **Create the skill definition** (`.claude/skills/get-code-coverage/SKILL.md`) and **shell script** (`scripts/get-code-coverage.sh`) following the templates above, adjusting paths to match your project layout:
   - Update the `dotnet test` path to point to your solution or test project
   - Update the `cover report` glob to match where your coverage files are generated
   - Update `--results-directory` if you prefer a different output location

4. **Make the script executable:**

   ```bash
   chmod +x .claude/skills/get-code-coverage/scripts/get-code-coverage.sh
   ```

5. **Customize the skill to fit your workflow.** The included skill is a starting point. Consider adjusting the `description` in `SKILL.md` to match the terminology your team uses.

Once configured, the agent will automatically invoke the skill whenever a user asks about code coverage, uncovered lines, or missing tests — no manual intervention required.

---

# Part 2: Contributing

## Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)

Verify your installation:

```bash
dotnet --version
# Should output 10.x.x
```

## Getting Started

### Clone and Build

```bash
git clone https://github.com/pauldentener/code_coverage_reporter_cli.git
cd code_coverage_reporter_cli

# Build the solution
cd src
dotnet build

# Run tests
dotnet test
```

### Local Development Setup

To make the `cover` command available in your terminal during development:

```bash
# From the repository root
source ./setup-cover.sh
```

This adds the compiled CLI to your PATH for the current session.

### Development Workflow

```bash
cd src

# Format code (required before commits)
dotnet format

# Build
dotnet build

# Run tests
dotnet test

# Run tests with coverage
dotnet test --collect:"XPlat Code Coverage" --results-directory ./code_coverage
```

### Generating Coverage for This Project

To generate and view coverage for this project itself:

```bash
cd src

# Run tests with coverage collection
dotnet test --collect:"XPlat Code Coverage" --results-directory ./code_coverage

# Use the CLI to report missing coverage
source ../setup-cover.sh
cover report "./code_coverage/**/coverage.cobertura.xml"
```

## Tech Stack

| Technology | Purpose |
|------------|---------|
| .NET 10 | Runtime and SDK |
| C# (latest) | Language |
| Spectre.Console | Rich terminal output and CLI framework |
| Microsoft.Extensions.DependencyInjection | Dependency injection |
| Microsoft.Extensions.FileSystemGlobbing | Glob pattern matching for file resolution |
| xUnit | Testing framework |
| NSubstitute | Mocking library |
| Coverlet | Code coverage collection |

## Development Standards

- **Code Coverage**: 100% test coverage required for all code
- **Testing**: xUnit with NSubstitute for mocking
- **Code Style**: Enforced via `.editorconfig`, `dotnet format`, and .NET analyzers (all warnings treated as errors)
- **Architecture**: SOLID principles, dependency injection, immutable models
- **Naming**: Test methods use `MethodName_Scenario_ExpectedResult` pattern

## Project Structure

```
/
├── README.md
├── CLAUDE.md                           # AI assistant instructions
├── setup-cover.sh                      # Development setup script
├── src/
│   ├── CodeCoverageReporter.sln        # Solution file
│   ├── Directory.Build.props           # Shared build configuration (analyzers, warnings)
│   ├── Directory.Build.targets         # Shared build targets
│   ├── Directory.Packages.props        # Central package management
│   ├── global.json                     # .NET SDK version pin
│   ├── .editorconfig                   # Code style rules
│   │
│   ├── CodeCoverageReporter.CLI/       # CLI application
│   │   ├── Program.cs                  # Entry point
│   │   ├── Commands/
│   │   │   ├── DefaultCommand.cs       # Banner/version display
│   │   │   └── ReportCommand.cs        # Report generation
│   │   └── Infrastructure/             # DI and console abstractions
│   │
│   ├── CodeCoverageReporter.CLI.Tests/ # CLI tests
│   │   └── TestData/                   # Sample Cobertura XML files
│   │
│   ├── CodeCoverageReporter.Cobertura/ # Core library
│   │   ├── Models/                     # Immutable data models
│   │   ├── Parsing/                    # XML parsing
│   │   ├── IO/                         # File operations
│   │   ├── Merging/                    # Report merging
│   │   ├── Reporting/                  # Coverage extraction
│   │   ├── Exporting/                  # Output formatters
│   │   ├── Paths/                      # Path transformation utilities
│   │   └── ExtensionMethods/           # DI service registration
│   │
│   └── CodeCoverageReporter.Cobertura.Tests/
│
└── tickets/                            # Feature tickets and plans
```

## License

This project is licensed under the [MIT No Attribution License (MIT-0)](LICENSE). You are free to use, modify, and redistribute this software without attribution.
