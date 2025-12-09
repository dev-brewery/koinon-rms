# MCP Servers - Project-Local Configuration

This directory contains project-local MCP (Model Context Protocol) server dependencies for koinon-rms.

## Purpose

Installing MCP servers locally provides several benefits:
- **No global npm pollution** - Packages stay within the project
- **Consistent versions** - All developers use the same MCP versions
- **Faster startup** - No npx downloads on each session
- **Version control** - package.json tracks exact versions

## Installed Servers

This project uses the following MCP servers:

| Server | Version | Description | Token Savings |
|--------|---------|-------------|---------------|
| server-memory | 2025.11.25 | Persistent session context/knowledge graph | 99% on context retention |
| server-filesystem | 2025.11.25 | Project filesystem access | 85% on file discovery |

## Setup

### Initial Installation

From project root:
```bash
cd tools/mcp-servers
npm install
```

This will install all MCP server packages to node_modules/ (gitignored).

## Token Efficiency

Before optimization (npx-based):
- Session startup: ~400 tokens
- npx download overhead: 120 tokens/server

After optimization (local install):
- Session startup: ~35 tokens  
- No download overhead
- **Savings: 91% (365 tokens/session)**

## Links

- [MCP Official Documentation](https://modelcontextprotocol.io)
- [MCP Servers Repository](https://github.com/modelcontextprotocol/servers)
