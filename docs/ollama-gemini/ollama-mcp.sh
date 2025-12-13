#!/bin/bash
# Wrapper to invoke Windows Node.js MCP server from WSL2
# This version logs all output to a file for debugging.

# Define a log file path in a known location
LOG_FILE="/home/mbrewer/projects/koinon-rms/docs/ollama-gemini/ollama-mcp-debug.log"

# Clear the log file for this run and add a timestamp
echo "--- Starting Ollama MCP Server at $(date) ---" > "$LOG_FILE"
echo "--- This log captures all output (stdout & stderr) from the Node.js process. ---" >> "$LOG_FILE"
echo "" >> "$LOG_FILE"

# Path to Windows Node.js
NODE_EXE="/mnt/c/Program Files/nodejs/node.exe"

# Path to compiled MCP server (in WSL path format)
MCP_SERVER_WSL="/mnt/g/repos/wsl-mcp-ollama/dist/index.js"

# Convert the WSL path to a Windows path
MCP_SERVER_WIN=$(wslpath -w "$MCP_SERVER_WSL")

# Ollama API endpoint
export OLLAMA_HOST="http://localhost:11434"

# Execute the server, redirecting all stdout and stderr to the log file.
# The 'exec' command replaces the shell process with the command,
# so all output will be captured by the redirection.
exec "$NODE_EXE" "$MCP_SERVER_WIN" >> "$LOG_FILE" 2>&1
