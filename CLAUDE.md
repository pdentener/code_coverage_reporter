# CLAUDE.md
This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Repository Overview
This repository contains **Code Coverage Reporter**, a .NET 10 (C#) CLI tool for generating code coverage reports. The solution uses Spectre.Console for rich terminal output and follows modern .NET patterns.

Anything file that you create in this repo MUST follow a fixed layout:
- All c# source files and (global) dotnet development related files go in /src root (e.g., *.cs, *.sln, Directory.Build.props, Directory.Packages.props, .editorconfig, global.json).
- Each project *always* gets its own folder
    - Put all project code and assets under that folder (e.g., Program.cs, Controllers/, Services/, Tests/, etc.).
- When adding a new project: always create a new /src/<ProjectName>/ folder—never mix project files in /src root.
- Before writing files: verify the path matches this structure; if not, move/rename to comply.

## Projects

### Project CLI: Code Coverage Reporter CLI (CodeCoverageReporter.CLI.csproj in src/CodeCoverageReporter.CLI)

Command-line interface for generating code coverage reports. Executable: `cover`

**Commands:** Default (banner/version), `report` (generates missing coverage reports from Cobertura XML)

**Key Dependencies:** Spectre.Console, Spectre.Console.Cli, Microsoft.Extensions.DependencyInjection

See `src/CodeCoverageReporter.CLI/CLAUDE.md` for detailed project documentation.

### Project CLI.Tests: CLI Unit & Integration Tests (CodeCoverageReporter.CLI.Tests.csproj in src/CodeCoverageReporter.CLI.Tests)

Test suite for the CLI project with 100% code coverage target.

**Framework:** xUnit, NSubstitute (mocking), Spectre.Console.Testing

See `src/CodeCoverageReporter.CLI.Tests/CLAUDE.md` for detailed testing documentation.

### Project Cobertura: Cobertura Library (CodeCoverageReporter.Cobertura.csproj in src/CodeCoverageReporter.Cobertura)

Standalone class library for loading, parsing, merging, and exporting Cobertura XML coverage files.

**Key Components:** Parsing (`ICoberturaParser`), File I/O (`ICoverageFileReader`), Merging (`ICoverageMerger`), Extraction (`IMissingCoverageExtractor`), Exporting (`ICoverageExporter`)

**Key Dependencies:** Microsoft.Extensions.DependencyInjection.Abstractions, Microsoft.Extensions.FileSystemGlobbing

See `src/CodeCoverageReporter.Cobertura/CLAUDE.md` for detailed project documentation.

### Project Cobertura.Tests: Cobertura Unit Tests (CodeCoverageReporter.Cobertura.Tests.csproj in src/CodeCoverageReporter.Cobertura.Tests)

Test suite for the Cobertura library with 100% code coverage target.

**Framework:** xUnit, NSubstitute (mocking)

## Running the cover CLI

The `cover` tool is installed as a .NET global tool. After building, use the `/reinstall-global-tool` skill to pick up local code changes.

```bash
# Install globally (first time)
dotnet pack src/CodeCoverageReporter.CLI/CodeCoverageReporter.CLI.csproj -o ./nupkg
dotnet tool install --global --add-source ./nupkg CodeCoverageReporterCLI

# Show banner and version
cover
cover --version

# Generate missing coverage report
cover report <files> [options]
```

**Options:**
- `--output table|json|markdown` - Output format (default: table)
- `--limit N` - Maximum rows to output
- `--exclude <pattern>` - Glob patterns to exclude files (repeatable)
- `--verbose` - Processing info to stderr
- `--base-path <dir>` - Base directory for relative paths
- `--absolute-paths` - Show full absolute paths

**Examples:**
```bash
cover report coverage.cobertura.xml
cover report "**/*.cobertura.xml" --output json --limit 20
cover report coverage.xml --exclude "**/*.g.cs" --verbose
```

See `src/CodeCoverageReporter.CLI/CLAUDE.md` for full command documentation.

## Implementation Plans
Your work in this repo is organized around **tickets** and their corresponding **implementation plans**.

- A **ticket** describes a single component, feature, or deliverable.
- A **plan** breaks that ticket into an **ordered checklist** of actionable steps. We create plans together.

**Location**
- Each ticket lives in: `./tickets/<Ticket_Name>/ticket.md`
- Plans live alongside it in: `./tickets/<Ticket_Name>/`

**Plan Naming Convention**
- **Complete ticket implementation**: Use `plan.md` when the plan covers the entire scope of the ticket
- **Partial/focused implementation**: Use descriptive names (e.g., `plan-parsing.md`, `plan-cli-integration.md`, `plan-output-formats.md`) when the plan covers only a specific part or phase of the ticket

**Folder layout examples**

```
tickets
├── feature-authentication
│   ├── ticket.md
│   └── plan.md                    # Complete implementation
├── feature-reporting
│   ├── ticket.md
│   ├── plan-parsing.md            # Part 1: Data parsing
│   ├── plan-querying.md           # Part 2: Query engine
│   └── plan-output.md             # Part 3: Output formats
```

Work is always requested in relation to a specific plan. *If a single plan can't be located, immediately abort and say "I can't locate the relevant plan to work on."*

### Implementation plan structure

Ensure that at the end of each plan you create, a summary is added.

## Summary
[ ] Step 1: Title
[ ] Step 2: Title
<additional steps here>

### Implementation plan execution

### Step status tokens
While you execute a plan, each step in an implementation plan must always be in exactly one state:

- Not started: [ ]
- In progress: 🔄
- Complete: ✅

It is your responsibility to keep the status of the steps in the plan in sync with what you're currently doing.

### Step update rules

- Do not change step numbering or step text.
- Only change the leading status token.
- Only one step may be 🔄 at any time!

### Execution rules

1. Only execute explicitly requested steps
- Execute only the step(s) the user instructs you to execute.
- Do not start, continue, or switch to any other step unless the user explicitly requests it.

2. “Execute the next step” special case
- If the user says execute the next step, interpret it as: execute the first step in the checklist that is not ✅.

3. Starting a requested step
- Before doing any work for a step, change its status token to 🔄.
- If another step is already 🔄, do not start the requested step; instead, report that a different step is currently in progress and leave the checklist unchanged.

4. Completing a requested step
- As soon as a step’s work is finished, change its status token to ✅.

5. Multiple requested steps
- If the user requests multiple steps, execute them strictly in the order the user specified.

6. Resume behavior
- If the user requests a step that is already 🔄, continue that step and keep it 🔄 until finished, then set it to ✅.
- If the user requests a step that is ✅, do not redo it unless the user explicitly asks to redo it.

### Required output (every response)
At the end of every response, always include:

1. The full updated summary checklist from the plan (all steps with current status tokens)

"Required output applies when performing plan execution work (i.e., when a plan step is being executed)."

### Plan Completion Validation
When ALL steps in a plan are marked ✅, you MUST run the full validation cycle before reporting completion:

```bash
dotnet format && dotnet build && dotnet test
```

- If any command fails, fix the issues before marking the plan complete
- Only report the plan as complete after all three commands succeed
- This is mandatory - never skip this final validation

## Development Standards
**CRITICAL for Claude Code**: These standards are non-negotiable. Every file you create or modify must comply. If a task conflicts with these standards, explain the conflict and ask for guidance before proceeding.

### Core Principles
Write idiomatic, production-quality C# for modern dotnet. Default to current best practices and conventions for the latest stable .NET/C# unless specified otherwise. Optimize first for correctness, clarity, and maintainability; avoid unnecessary complexity.

When there's a legacy approach and a modern approach, always prefer the modern .NET way.

### Dependency Injection
- **Always use .NET DI infrastructure**: Use `IServiceCollection`, `IServiceProvider`, and constructor injection for wiring up dependencies
- **Prefer injection over instantiation**: Inject dependencies rather than using `new` or static methods for services
- **Exception for simple helpers**: Classes that don't warrant DI overhead (e.g., `StringBuilder`, configuration builders, simple DTOs) can be instantiated directly
- **Service registration pattern for class libraries**:
  1. Add an `ExtensionMethods/` folder to each class library
  2. Create `ServiceCollectionExtensions.cs` with `Add{LibraryName}()` extension method
  3. Call the extension method from `Program.cs` in the entry point project

**Example:**
```csharp
// In SomeLibrary/ExtensionMethods/ServiceCollectionExtensions.cs
public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddSomeLibrary(this IServiceCollection services)
    {
        services.AddSingleton<IMyService, MyService>();
        return services;
    }
}

// In Program.cs
services.AddSomeLibrary();
```

### Engineering Principles
Follow modern .NET and software engineering best practices, emphasizing:

- **Simplicity**: KISS, YAGNI, Explicit over Implicit
- **Solid architecture**: SOLID principles, Separation of Concerns, High Cohesion/Low Coupling
- **Composition**: Prefer composition over inheritance; depend on abstractions (Dependency Inversion)
- **Code quality**: DRY, Single Source of Truth, Boy Scout Rule, Refactor Mercilessly
- **Predictability**: Principle of Least Astonishment, Law of Demeter
- **Safety**: Defensive Programming, Fail Fast, Immutability when possible, Principle of Least Privilege
- **Design patterns**: Apply appropriate design patterns and cloud-native patterns where applicable
- **Standards**: Design by Contract, Convention over Configuration, Open/Closed Principle

When tradeoffs conflict, prefer simplicity and readability. If there's ambiguity, make reasonable assumptions, state them briefly, and pick the simplest approach.

### Testing Requirements *CRITICAL*
- **100% code coverage required**: Every line of code MUST be covered by unit tests
- Write tests for all new code before committing
- Follow the Test Pyramid: emphasize unit tests, fewer integration tests, minimal e2e
- Run all tests locally before pushing
- Tests must be fast, isolated, and deterministic
- Use meaningful test names that describe the scenario and expected outcome
- **Test classes MUST be `public`**: xUnit requires test classes to be `public` (use `public sealed class`). Never use `internal` for test classes - the build will fail with xUnit1000.

#### Acceptable Use of `[ExcludeFromCodeCoverage]`
While 100% code coverage is required, the `ExcludeFromCodeCoverageAttribute` may be applied in specific scenarios where unit tests provide no meaningful value:

**Allowed scenarios:**
- **Custom exception constructors**: Constructors that only forward arguments to a base constructor with no additional logic (e.g., `public MyException(string message) : base(message) { }`)
- **Trivial auto-properties**: Simple getter/setter properties with no logic beyond storing/returning a value
- **ToString() for debugging**: Override implementations used only for debugging/logging, not business logic
- **Required no-op interface implementations**: Empty method bodies required by an interface but intentionally unused in this implementation
- **Platform-specific code paths**: Code that cannot be executed in the test environment due to platform constraints
- **Compiler-generated boilerplate**: Generated code (e.g., record equality members) when not part of business logic

**NOT allowed - these must always be tested:**
- Error handling or exception paths
- Null checks or guard clauses
- Default/fallback branches in switch statements
- Any code containing business logic or conditional logic

**Requirement:** When using `[ExcludeFromCodeCoverage]`, add a brief comment explaining why the exclusion is justified.

### Dependency Management
- **Prefer NuGet packages over custom code**: Always search NuGet.org before implementing functionality
- Use established packages with good maintenance records
- Document why you chose a package or chose to implement custom code
- Use Central Package Management (CPM) for all NuGet packages

## Development Commands

### Quick Actions
- `dotnet format` - Formats the code to match editorconfig settings.
- `dotnet build` - Builds the solution.
- `dotnet test` - Runs unit tests.

*important* Always run a dotnet format before a dotnet build.
*important* Always run a dotnet test after a dotnet build.

