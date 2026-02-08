---
name: reinstall-global-tool
description: Force-reinstall the cover global CLI tool by packing, uninstalling, and reinstalling.
allowed-tools: Bash
---

## Overview
Packs the CLI project and force-reinstalls the `cover` global tool to pick up the latest code changes. This avoids the issue where `dotnet tool update` skips reinstallation when the package version is unchanged.

## Steps

Run the following command from the repository root:

```bash
dotnet pack src/CodeCoverageReporter.CLI/CodeCoverageReporter.CLI.csproj -o ./nupkg && dotnet tool uninstall --global codecoveragereportercli && dotnet tool install --global --add-source ./nupkg CodeCoverageReporterCLI
```
