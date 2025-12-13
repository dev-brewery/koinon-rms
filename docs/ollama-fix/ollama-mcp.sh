#!/bin/bash
# Wrapper to invoke Windows Node.js MCP server from WSL2
# Sets OLLAMA_HOST=0.0.0.0 for Windows process

set -e

NODE_EXE_WSL="/mnt/c/Program Files/nodejs/node.exe"
MCP_SERVER_WSL="/mnt/g/repos/wsl-mcp-ollama/dist/index.js"
POWERSHELL_EXE="/mnt/c/Windows/System32/WindowsPowerShell/v1.0/powershell.exe"
ENSURE_OLLAMA_PS1="/mnt/c/Users/crazy/.claude/scripts/ensure-ollama.ps1"

# Ensure Ollama is running with 0.0.0.0 binding
if [ -f "$ENSURE_OLLAMA_PS1" ]; then
    ENSURE_OLLAMA_WIN=$(wslpath -w "$ENSURE_OLLAMA_PS1")
    "$POWERSHELL_EXE" -ExecutionPolicy Bypass -File "$ENSURE_OLLAMA_WIN" >/dev/null 2>&1 || true
fi

# Verify MCP server exists
if [ ! -f "$MCP_SERVER_WSL" ]; then
    echo "ERROR: MCP server not found at: $MCP_SERVER_WSL" >&2
    exit 1
fi

# Convert paths to Windows format
NODE_EXE_WIN=$(wslpath -w "$NODE_EXE_WSL")
MCP_SERVER_WIN=$(wslpath -w "$MCP_SERVER_WSL")

# Run Windows Node with OLLAMA_HOST set via PowerShell
exec "$POWERSHELL_EXE" -NoProfile -Command "\$env:OLLAMA_HOST='http://0.0.0.0:11434'; & '$NODE_EXE_WIN' '$MCP_SERVER_WIN'"
