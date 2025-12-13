#!/bin/bash
# This script deploys the Ollama MCP server files to their correct locations.
set -e # Exit immediately if a command fails

echo "Starting the deployment of the Ollama MCP Server files..."

# --- Configuration ---
# Source directory for the restored files
SRC_DIR="./docs/ollama-gemini"

# Destination directory for the Node.js server project
# This assumes the project lives at /mnt/g/repos/wsl-mcp-ollama
DEST_PROJECT_DIR="/mnt/g/repos/wsl-mcp-ollama"

# Destination directories for the Claude Code configuration
CLAUDE_CONFIG_DIR="/home/mbrewer/projects/koinon-rms/.claude"
CLAUDE_SCRIPTS_DIR="/home/mbrewer/.claude/scripts"

# --- Deployment Steps ---

# 1. Copy Node.js project files
echo "Copying files to $DEST_PROJECT_DIR..."
# Ensure destination directory exists
mkdir -p "$DEST_PROJECT_DIR"
cp "$SRC_DIR/package.json" "$DEST_PROJECT_DIR/package.json"
cp "$SRC_DIR/tsconfig.json" "$DEST_PROJECT_DIR/tsconfig.json"

# Remove old src directory if it exists and create a fresh copy
rm -rf "$DEST_PROJECT_DIR/src"
cp -r "$SRC_DIR/src" "$DEST_PROJECT_DIR/"
echo "Node.js project files copied."

# 2. Create .claude directories if they don't exist
echo "Ensuring .claude directories exist..."
mkdir -p "$CLAUDE_CONFIG_DIR"
mkdir -p "$CLAUDE_SCRIPTS_DIR"
echo ".claude directories are present."

# 3. Copy MCP config
echo "Copying MCP configuration..."
cp "$SRC_DIR/mcp.json" "$CLAUDE_CONFIG_DIR/.mcp.json"
echo "MCP configuration copied."

# 4. Copy and set permissions on the wrapper script
echo "Copying wrapper script..."
cp "$SRC_DIR/ollama-mcp.sh" "$CLAUDE_SCRIPTS_DIR/ollama-mcp.sh"
echo "Setting execute permissions on the wrapper script..."
chmod +x "$CLAUDE_SCRIPTS_DIR/ollama-mcp.sh"
echo "Wrapper script is ready."

echo ""
echo "--- Deployment Complete ---"
echo ""
echo "Next Steps:"
echo "1. Run 'npm install' from your Windows terminal in G:\\repos\\wsl-mcp-ollama to install dependencies."
echo "2. Run 'npm run build' in the same directory to compile the server."
echo "3. Manually start the Ollama service on Windows (e.g., via the Start Menu or 'ollama serve' in PowerShell)."
echo "4. Restart your Claude Code session. The Ollama agent should now load correctly."
