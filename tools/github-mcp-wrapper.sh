#!/bin/bash
# Wrapper script for GitHub MCP server
# Maps GITHUB_TOKEN to GITHUB_PERSONAL_ACCESS_TOKEN

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
PROJECT_ROOT="$(cd "$SCRIPT_DIR/.." && pwd)"

# Source .devenv if it exists and GITHUB_TOKEN not already set
if [ -z "$GITHUB_TOKEN" ] && [ -f "$PROJECT_ROOT/.devenv" ]; then
    source "$PROJECT_ROOT/.devenv"
fi

export GITHUB_PERSONAL_ACCESS_TOKEN="${GITHUB_TOKEN}"
exec npx -y @modelcontextprotocol/server-github "$@"
