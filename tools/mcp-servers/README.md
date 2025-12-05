# MCP Servers for Koinon RMS

This directory contains configuration and documentation for Model Context Protocol (MCP) servers used in the Koinon RMS project for multi-agent development.

## Overview

MCP servers provide specialized capabilities to AI agents, enabling them to interact with databases, APIs, filesystems, and project-specific validation tools.

## Installed Servers

### 1. PostgreSQL MCP Server
**Package:** `@modelcontextprotocol/server-postgres@0.6.2`
**Status:** ✅ Installed (Deprecated but functional)
**Purpose:** Query and inspect the PostgreSQL database

**Configuration:**
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
    }
  }
}
```

**Usage:**
- Query database schema
- Inspect table structures
- Run SQL queries for debugging
- Validate data integrity

**Note:** This package is deprecated but still functional. Consider migrating to alternative when available.

---

### 2. Memory/Knowledge Graph MCP Server
**Package:** `@modelcontextprotocol/server-memory@2025.11.25`
**Status:** ✅ Installed
**Purpose:** Persist architectural decisions and project knowledge across agent sessions

**Configuration:**
```json
{
  "mcpServers": {
    "memory": {
      "command": "npx",
      "args": ["-y", "@modelcontextprotocol/server-memory"]
    }
  }
}
```

**Usage:**
- Store architectural decisions
- Remember work unit completion status
- Track technical debt items
- Maintain project context between sessions

---

### 3. GitHub MCP Server
**Package:** `@modelcontextprotocol/server-github@2025.4.8`
**Status:** ✅ Installed (Deprecated but functional)
**Purpose:** Interact with GitHub repository

**Configuration:**
```json
{
  "mcpServers": {
    "github": {
      "command": "npx",
      "args": ["-y", "@modelcontextprotocol/server-github"],
      "env": {
        "GITHUB_TOKEN": "${GITHUB_TOKEN}"
      }
    }
  }
}
```

**Environment Variables Required:**
- `GITHUB_TOKEN`: GitHub personal access token with repo permissions

**Usage:**
- Create and manage issues
- Review pull requests
- Access repository metadata
- Manage project boards

**Setup:**
```bash
# Create GitHub token at: https://github.com/settings/tokens
# Add to environment:
export GITHUB_TOKEN="your_token_here"
```

**Note:** This package is deprecated. Consider migrating when alternative available.

---

### 4. Filesystem MCP Server
**Package:** `@modelcontextprotocol/server-filesystem@2025.11.25`
**Status:** ✅ Installed
**Purpose:** Advanced filesystem operations and search

**Configuration:**
```json
{
  "mcpServers": {
    "filesystem": {
      "command": "npx",
      "args": [
        "-y",
        "@modelcontextprotocol/server-filesystem",
        "/home/mbrewer/projects/koinon-rms"
      ]
    }
  }
}
```

**Usage:**
- Search across files with advanced patterns
- Read and write files
- Directory operations
- File metadata inspection

---

### 5. Koinon RMS Development Server (Custom)
**Package:** `@koinon/mcp-dev-server@1.0.0`
**Status:** ✅ Custom Implementation
**Purpose:** Project-specific validation and guidance

**Configuration:**
```json
{
  "mcpServers": {
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

**Features:**
- Validates naming conventions
- Checks IdKey usage in routes
- Validates clean architecture dependencies
- Detects anti-patterns
- Provides architectural guidance

See `../mcp-koinon-dev/README.md` for detailed documentation.

---

## Servers NOT Available

### SQLite MCP Server
**Status:** ❌ Not found in npm registry
**Alternative:** Use PostgreSQL server for database operations

### HTTP/Fetch MCP Server
**Status:** ❌ `@cloudflare/mcp-server-fetch` not found
**Alternative:** Use built-in HTTP capabilities or implement custom wrapper

### Docker MCP Server
**Status:** ❌ No official Docker MCP server found
**Alternative:** Use CLI commands via bash execution

### Redis MCP Server
**Status:** ❌ No official Redis MCP server found
**Alternative:** Use CLI commands (`redis-cli`) or implement custom wrapper if needed

### .NET/NuGet MCP Server
**Status:** ❌ Not found
**Alternative:** Use filesystem server to read `.csproj` files and bash for `dotnet` commands

### npm Registry MCP Server
**Status:** ❌ Not found
**Alternative:** Use bash commands for `npm` operations

---

## Complete Claude Code Configuration

Create or update your Claude Code configuration file with all available servers:

**Location:** Typically `~/.config/claude-code/config.json` or similar

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

---

## Setup Instructions

### Prerequisites

1. **Node.js and npm** installed (v20+)
2. **Docker services running:**
   ```bash
   cd /home/mbrewer/projects/koinon-rms
   docker-compose up -d
   ```

3. **GitHub Token** (for GitHub server):
   ```bash
   export GITHUB_TOKEN="your_github_personal_access_token"
   ```

### Installation Steps

1. **Install MCP server dependencies:**
   ```bash
   cd /home/mbrewer/projects/koinon-rms/tools/mcp-servers
   npm install
   ```

2. **Build custom Koinon dev server:**
   ```bash
   cd /home/mbrewer/projects/koinon-rms/tools/mcp-koinon-dev
   npm install
   npm run build
   ```

3. **Configure Claude Code:**
   - Add the configuration JSON above to your Claude Code settings
   - Ensure paths are absolute and match your system
   - Set environment variables (especially GITHUB_TOKEN)

4. **Test server connections:**
   ```bash
   # Test PostgreSQL server
   npx -y @modelcontextprotocol/server-postgres postgresql://koinon:koinon@localhost:5432/koinon

   # Test custom Koinon server
   node /home/mbrewer/projects/koinon-rms/tools/mcp-koinon-dev/dist/index.js
   ```

---

## Usage Examples

### PostgreSQL Server
```
Query: Show all tables in the database
Tool: postgres query
SQL: SELECT tablename FROM pg_tables WHERE schemaname = 'public';
```

### Memory Server
```
Store: Remember that WU-1.1.1 is completed
Tool: memory store
Key: work-unit-1.1.1
Value: Completed on 2025-12-05
```

### GitHub Server
```
Create issue for missing test coverage
Tool: github create_issue
Title: "Add unit tests for Person entity"
Body: "Coverage analysis shows Person entity lacks tests..."
```

### Filesystem Server
```
Search for all Entity classes
Tool: filesystem search
Pattern: "*.cs"
Contains: "public class.*: Entity"
```

### Koinon Dev Server
```
Validate route naming
Tool: validate_routes
Routes: ["/api/v1/people/{idKey}", "/api/person/123"]
```

---

## Troubleshooting

### Server Not Starting

**Issue:** MCP server fails to start
**Solutions:**
- Check that all dependencies are installed: `npm install`
- Verify Node.js version: `node --version` (should be v20+)
- Check paths in configuration are absolute
- Ensure Docker services are running for database servers

### PostgreSQL Connection Failed

**Issue:** Cannot connect to database
**Solutions:**
- Start Docker services: `docker-compose up -d`
- Verify PostgreSQL is running: `docker ps | grep postgres`
- Test connection: `psql postgresql://koinon:koinon@localhost:5432/koinon`
- Check firewall settings

### GitHub Server Authentication Failed

**Issue:** GitHub operations fail
**Solutions:**
- Verify GITHUB_TOKEN is set: `echo $GITHUB_TOKEN`
- Check token permissions at https://github.com/settings/tokens
- Ensure token has `repo` scope

### Custom Server Not Found

**Issue:** Koinon dev server not loading
**Solutions:**
- Rebuild server: `cd tools/mcp-koinon-dev && npm run build`
- Check dist folder exists: `ls tools/mcp-koinon-dev/dist/`
- Verify path in configuration is correct

---

## Development

### Adding New MCP Servers

1. Research available servers on npm
2. Test server locally
3. Add to `package.json` dependencies
4. Update this README with configuration
5. Add to complete configuration example

### Updating Custom Server

1. Modify `tools/mcp-koinon-dev/src/index.ts`
2. Rebuild: `npm run build`
3. Update README with new features
4. Test with Claude Code

---

## Security Considerations

- **Never commit** tokens or passwords to git
- Use environment variables for sensitive data
- Rotate GitHub tokens regularly
- Limit database user permissions for development
- Review MCP server code before using in production

---

## Performance Tips

- Use memory server to avoid re-querying information
- Cache database schema information
- Batch filesystem operations
- Limit SQL query result sets

---

## Future Enhancements

Potential custom MCP servers to implement:

1. **Redis MCP Server**
   - Inspect cache keys
   - Monitor cache hit rates
   - Manage sessions

2. **Docker MCP Server**
   - Container management
   - Log inspection
   - Health checks

3. **API Testing Server**
   - Test endpoints
   - Validate responses
   - Load testing

4. **.NET Project Server**
   - NuGet package management
   - Project reference analysis
   - Dependency graph generation

---

## Resources

- [MCP Specification](https://spec.modelcontextprotocol.io/)
- [MCP SDK Documentation](https://github.com/modelcontextprotocol/typescript-sdk)
- [Official MCP Servers](https://github.com/modelcontextprotocol/servers)
- [Claude Code Documentation](https://docs.anthropic.com/claude/docs)

---

## Support

For issues or questions:
1. Check troubleshooting section above
2. Review server-specific README files
3. Check MCP server GitHub repositories
4. Consult project CLAUDE.md for conventions
