#!/usr/bin/env bash

REPO_ROOT="$(git rev-parse --show-toplevel)"

# Delete any existing code coverage results.
rm -rf "$REPO_ROOT/src/code_coverage" > /dev/null 2>&1

# Run tests with code coverage collection.
dotnet test "$REPO_ROOT/src" --collect:"XPlat Code Coverage" --results-directory "$REPO_ROOT/src/code_coverage" > /dev/null 2>&1

# Generate missing code coverage information.
cover report "$REPO_ROOT"/src/code_coverage/**/coverage.cobertura.xml
