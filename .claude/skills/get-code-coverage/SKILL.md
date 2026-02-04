---
name: get-code-coverage
description: Gets code coverage details for source files and lines of code. Use whenever the user asks about uncovered code, coverage gaps, missing coverage, missing tests, or improvements to code coverage.
allowed-tools: Bash(.claude/skills/get-code-coverage/scripts/get-code-coverage.sh)
---

# Get Code Coverage

Gets missing code coverage information. Each row returned represents a line in a code file that has missing coverage.

*note*: only uncovered lines are returned.

## Instructions

Run the following script:

```bash
.claude/skills/get-code-coverage/scripts/get-code-coverage.sh
```