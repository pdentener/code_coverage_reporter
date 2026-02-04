#!/usr/bin/env bash

# Delete any existing code coverage results.
rm -rf ./src/code_coverage > /dev/null 2>&1

# Run tests with code coverage collection.
dotnet test ./src --collect:"XPlat Code Coverage" --results-directory ./src/code_coverage > /dev/null 2>&1

# Generate missing code coverage information.
./src/CodeCoverageReporter.CLI/bin/Debug/net10.0/cover report ./src/code_coverage/**/coverage.cobertura.xml