#!/bin/bash
# Wrapper script for GitHub MCP server
# Maps GITHUB_TOKEN to GITHUB_PERSONAL_ACCESS_TOKEN

export GITHUB_PERSONAL_ACCESS_TOKEN="${GITHUB_TOKEN}"
exec npx -y @modelcontextprotocol/server-github "$@"
