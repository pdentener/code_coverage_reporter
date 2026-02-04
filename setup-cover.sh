#!/usr/bin/env bash

SCRIPT_DIR="$( cd "$( dirname "${BASH_SOURCE[0]}" )" && pwd )"
CLI_BIN="$SCRIPT_DIR/src/CodeCoverageReporter.CLI/bin/Debug/net10.0"

if [[ ":$PATH:" != *":$CLI_BIN:"* ]]; then
    export PATH="$CLI_BIN:$PATH"
fi
