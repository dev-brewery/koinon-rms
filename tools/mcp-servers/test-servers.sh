#!/bin/bash

# MCP Servers Testing Script
# Tests each MCP server to ensure it's properly configured

set -e

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
PROJECT_ROOT="$(cd "$SCRIPT_DIR/../.." && pwd)"

echo "=========================================="
echo "Testing MCP Servers"
echo "=========================================="
echo ""

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
NC='\033[0m' # No Color

# Test counter
PASSED=0
FAILED=0

# Helper function to test command
test_command() {
    local name=$1
    local cmd=$2

    echo -n "Testing $name... "

    if eval "$cmd" &> /dev/null; then
        echo -e "${GREEN}✅ PASS${NC}"
        ((PASSED++))
    else
        echo -e "${RED}❌ FAIL${NC}"
        ((FAILED++))
    fi
}

# Test Node.js and npm
echo "=== Prerequisites ==="
test_command "Node.js" "node --version"
test_command "npm" "npm --version"
test_command "npx" "npx --version"
echo ""

# Test PostgreSQL MCP Server package
echo "=== MCP Server Packages ==="
test_command "PostgreSQL server package" "npm list @modelcontextprotocol/server-postgres 2>&1 | grep -q postgres"
test_command "Memory server package" "npm list @modelcontextprotocol/server-memory 2>&1 | grep -q memory"
test_command "GitHub server package" "npm list @modelcontextprotocol/server-github 2>&1 | grep -q github"
test_command "Filesystem server package" "npm list @modelcontextprotocol/server-filesystem 2>&1 | grep -q filesystem"
echo ""

# Test custom Koinon server
echo "=== Custom Koinon Server ==="
test_command "Custom server built" "test -f $PROJECT_ROOT/tools/mcp-koinon-dev/dist/index.js"
test_command "Custom server package.json" "test -f $PROJECT_ROOT/tools/mcp-koinon-dev/package.json"
echo ""

# Test Docker services
echo "=== Docker Services ==="
test_command "PostgreSQL container" "docker ps | grep -q koinon-postgres"
test_command "Redis container" "docker ps | grep -q koinon-redis"
echo ""

# Test database connection
echo "=== Database Connectivity ==="
if command -v psql &> /dev/null; then
    test_command "PostgreSQL connection" "psql 'postgresql://koinon:koinon@localhost:5432/koinon' -c 'SELECT 1' 2>&1 | grep -q '1 row'"
else
    echo -e "${YELLOW}⚠️  psql not installed - skipping PostgreSQL connection test${NC}"
fi

if command -v redis-cli &> /dev/null; then
    test_command "Redis connection" "redis-cli -h localhost -p 6379 ping 2>&1 | grep -q PONG"
else
    echo -e "${YELLOW}⚠️  redis-cli not installed - skipping Redis connection test${NC}"
fi
echo ""

# Test environment variables
echo "=== Environment Variables ==="
if [ -n "$GITHUB_TOKEN" ]; then
    echo -e "${GREEN}✅ GITHUB_TOKEN is set${NC}"
    ((PASSED++))
else
    echo -e "${YELLOW}⚠️  GITHUB_TOKEN not set${NC}"
    echo "   GitHub MCP server will not work"
    echo "   Set with: export GITHUB_TOKEN='your_token'"
fi
echo ""

# Summary
echo "=========================================="
echo "Test Summary"
echo "=========================================="
echo -e "Passed: ${GREEN}$PASSED${NC}"
if [ $FAILED -gt 0 ]; then
    echo -e "Failed: ${RED}$FAILED${NC}"
else
    echo -e "Failed: $FAILED"
fi
echo ""

if [ $FAILED -eq 0 ]; then
    echo -e "${GREEN}All tests passed! MCP servers are ready.${NC}"
    exit 0
else
    echo -e "${RED}Some tests failed. Please review the output above.${NC}"
    exit 1
fi
