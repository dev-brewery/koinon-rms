# MCP Servers Quick Start Guide

Get up and running with MCP servers for Koinon RMS in 5 minutes.

## Prerequisites

- ✅ Node.js 20+ installed
- ✅ Docker installed and running
- ✅ Claude Code installed

## Installation (5 minutes)

### Step 1: Start Infrastructure (30 seconds)

```bash
cd /home/mbrewer/projects/koinon-rms
docker-compose up -d
```

Verify:
```bash
docker ps | grep koinon
# Should show koinon-postgres and koinon-redis running
```

---

### Step 2: Install MCP Servers (2 minutes)

```bash
cd /home/mbrewer/projects/koinon-rms/tools/mcp-servers
npm install
```

---

### Step 3: Build Custom Server (1 minute)

```bash
cd /home/mbrewer/projects/koinon-rms/tools/mcp-koinon-dev
npm install
npm run build
```

Verify:
```bash
ls dist/index.js
# Should show the compiled server
```

---

### Step 4: Setup GitHub Token (1 minute)

1. Create token: https://github.com/settings/tokens
2. Grant `repo` scope
3. Export:
   ```bash
   export GITHUB_TOKEN="your_token_here"
   ```

Add to your shell profile for persistence:
```bash
echo 'export GITHUB_TOKEN="your_token_here"' >> ~/.bashrc
source ~/.bashrc
```

---

### Step 5: Configure Claude Code (30 seconds)

Add to your Claude Code configuration file:

**Linux/WSL:** `~/.config/claude-code/config.json`
**macOS:** `~/Library/Application Support/Claude Code/config.json`
**Windows:** `%APPDATA%\Claude Code\config.json`

```json
{
  "mcpServers": {
    "postgres": {
      "command": "npx",
      "args": [
        "-y",
        "@modelcontextprotocol/server-postgres",
        "postgresql://koinon:koinon@localhost:5432/koinon"
      ]
    },
    "memory": {
      "command": "npx",
      "args": ["-y", "@modelcontextprotocol/server-memory"]
    },
    "github": {
      "command": "npx",
      "args": ["-y", "@modelcontextprotocol/server-github"],
      "env": {
        "GITHUB_TOKEN": "${GITHUB_TOKEN}"
      }
    },
    "filesystem": {
      "command": "npx",
      "args": [
        "-y",
        "@modelcontextprotocol/server-filesystem",
        "/home/mbrewer/projects/koinon-rms"
      ]
    },
    "koinon-dev": {
      "command": "node",
      "args": [
        "/home/mbrewer/projects/koinon-rms/tools/mcp-koinon-dev/dist/index.js"
      ],
      "env": {
        "KOINON_PROJECT_ROOT": "/home/mbrewer/projects/koinon-rms"
      }
    }
  }
}
```

**Note:** Update paths if your project is in a different location!

---

### Step 6: Restart Claude Code

Restart Claude Code to load the new configuration.

---

## Verification

Test that everything works:

```bash
cd /home/mbrewer/projects/koinon-rms/tools/mcp-servers
./test-servers.sh
```

Should see all green checkmarks ✅

---

## Quick Reference

### What Each Server Does

| Server | Purpose | Example Use |
|--------|---------|-------------|
| **postgres** | Query database | "Show all tables in the database" |
| **memory** | Remember context | "Remember that WU-1.1.1 is complete" |
| **github** | Manage issues/PRs | "Create an issue for missing tests" |
| **filesystem** | Find files | "Find all Entity classes" |
| **koinon-dev** | Validate code | "Check if these names follow conventions" |

---

### Common Commands

**Validate naming:**
```
Agent: "Use koinon-dev to validate these database names: person, GroupMember, person_email"
```

**Query database:**
```
Agent: "Use postgres to list all tables in the public schema"
```

**Store decision:**
```
Agent: "Use memory to store that we decided to use IdKey for all routes"
```

**Create issue:**
```
Agent: "Use github to create an issue titled 'Add unit tests for Person entity'"
```

**Find files:**
```
Agent: "Use filesystem to find all .cs files in the Domain project"
```

---

## Troubleshooting

### Problem: PostgreSQL connection failed

**Fix:**
```bash
docker-compose up -d postgres
```

### Problem: GitHub authentication failed

**Fix:**
```bash
export GITHUB_TOKEN="your_token"
# Add to ~/.bashrc for persistence
```

### Problem: Custom server not found

**Fix:**
```bash
cd /home/mbrewer/projects/koinon-rms/tools/mcp-koinon-dev
npm run build
```

### Problem: Servers not loading in Claude Code

**Fix:**
1. Check configuration file location
2. Verify JSON syntax (use JSONLint)
3. Ensure all paths are absolute
4. Restart Claude Code

---

## What's Next?

- Read full documentation: `tools/mcp-servers/README.md`
- See usage examples: `tools/mcp-servers/EXAMPLES.md`
- Check custom server docs: `tools/mcp-koinon-dev/README.md`
- Review complete report: `MCP-SERVERS-REPORT.md`

---

## Support

**Common Issues:** See `tools/mcp-servers/README.md` → Troubleshooting section

**Project Conventions:** See `CLAUDE.md`

**Test Suite:** Run `./test-servers.sh` from `tools/mcp-servers/`

---

## Automated Setup

For automatic installation, run:

```bash
cd /home/mbrewer/projects/koinon-rms/tools/mcp-servers
./setup.sh
```

This will:
- ✅ Check prerequisites
- ✅ Install all packages
- ✅ Build custom server
- ✅ Verify Docker services
- ✅ Check environment variables
- ✅ Display configuration instructions

---

**Time to setup:** ~5 minutes
**Difficulty:** Easy
**Status:** Production ready
