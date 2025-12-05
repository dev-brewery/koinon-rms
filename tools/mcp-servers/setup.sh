#!/bin/bash

# MCP Servers Setup Script for Koinon RMS
# This script installs and configures all MCP servers for multi-agent development

set -e

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
PROJECT_ROOT="$(cd "$SCRIPT_DIR/../.." && pwd)"
MCP_KOINON_DIR="$PROJECT_ROOT/tools/mcp-koinon-dev"

echo "=========================================="
echo "Koinon RMS MCP Servers Setup"
echo "=========================================="
echo ""

# Check Node.js version
echo "Checking Node.js version..."
NODE_VERSION=$(node --version | cut -d'v' -f2 | cut -d'.' -f1)
if [ "$NODE_VERSION" -lt 20 ]; then
    echo "❌ Node.js version 20 or higher required. Current: $(node --version)"
    exit 1
fi
echo "✅ Node.js version: $(node --version)"
echo ""

# Check npm
echo "Checking npm..."
if ! command -v npm &> /dev/null; then
    echo "❌ npm not found"
    exit 1
fi
echo "✅ npm version: $(npm --version)"
echo ""

# Install MCP server dependencies
echo "Installing MCP server dependencies..."
cd "$SCRIPT_DIR"
npm install
echo "✅ MCP servers installed"
echo ""

# Build custom Koinon dev server
echo "Building custom Koinon RMS dev server..."
cd "$MCP_KOINON_DIR"
npm install
npm run build
echo "✅ Custom server built"
echo ""

# Check Docker services
echo "Checking Docker services..."
if docker ps | grep -q "koinon-postgres"; then
    echo "✅ PostgreSQL container running"
else
    echo "⚠️  PostgreSQL container not running"
    echo "   Start with: docker-compose up -d"
fi

if docker ps | grep -q "koinon-redis"; then
    echo "✅ Redis container running"
else
    echo "⚠️  Redis container not running"
    echo "   Start with: docker-compose up -d"
fi
echo ""

# Check environment variables
echo "Checking environment variables..."
if [ -z "$GITHUB_TOKEN" ]; then
    echo "⚠️  GITHUB_TOKEN not set"
    echo "   GitHub MCP server will not work without it"
    echo "   Create token at: https://github.com/settings/tokens"
    echo "   Then: export GITHUB_TOKEN='your_token_here'"
else
    echo "✅ GITHUB_TOKEN is set"
fi
echo ""

# Test PostgreSQL connection
echo "Testing PostgreSQL connection..."
if psql "postgresql://koinon:koinon@localhost:5432/koinon" -c "SELECT 1" &> /dev/null; then
    echo "✅ PostgreSQL connection successful"
else
    echo "⚠️  Cannot connect to PostgreSQL"
    echo "   Ensure Docker services are running: docker-compose up -d"
fi
echo ""

# Display configuration
echo "=========================================="
echo "Configuration Summary"
echo "=========================================="
echo ""
echo "Project Root: $PROJECT_ROOT"
echo "MCP Servers: $SCRIPT_DIR"
echo "Custom Server: $MCP_KOINON_DIR/dist/index.js"
echo ""

# Display next steps
echo "=========================================="
echo "Next Steps"
echo "=========================================="
echo ""
echo "1. Add the following to your Claude Code configuration:"
echo "   (Usually at ~/.config/claude-code/config.json or similar)"
echo ""
cat "$SCRIPT_DIR/claude-code-config.json"
echo ""
echo "2. If not already done, start Docker services:"
echo "   cd $PROJECT_ROOT"
echo "   docker-compose up -d"
echo ""
echo "3. Set GITHUB_TOKEN environment variable:"
echo "   export GITHUB_TOKEN='your_github_token'"
echo ""
echo "4. Restart Claude Code to load MCP servers"
echo ""
echo "=========================================="
echo "Setup Complete!"
echo "=========================================="
