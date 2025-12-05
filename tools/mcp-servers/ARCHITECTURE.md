# MCP Servers Architecture for Koinon RMS

## System Overview

```
┌─────────────────────────────────────────────────────────────────────┐
│                         Claude Code Agent                            │
│                    (Multi-Agent Development)                         │
└────────────┬────────────────────────────────────────────────────────┘
             │
             │ MCP Protocol (stdio)
             │
┌────────────┴────────────────────────────────────────────────────────┐
│                      MCP Server Layer                                │
├──────────────┬──────────────┬──────────────┬──────────────┬─────────┤
│              │              │              │              │         │
│  PostgreSQL  │    Memory    │   GitHub     │  Filesystem  │ Koinon  │
│   Server     │    Server    │   Server     │   Server     │   Dev   │
│              │              │              │              │ Server  │
└──────┬───────┴──────┬───────┴──────┬───────┴──────┬───────┴────┬────┘
       │              │              │              │            │
       │              │              │              │            │
┌──────▼──────┐┌──────▼──────┐┌──────▼──────┐┌──────▼──────┐┌─▼──────┐
│ PostgreSQL  ││   Memory    ││   GitHub    ││   Project   ││ Custom │
│  Database   ││   Storage   ││     API     ││   Files     ││ Logic  │
│             ││             ││             ││             ││        │
│ koinon:5432 ││  (local)    ││ (cloud)     ││ (local FS)  ││(in-mem)│
└─────────────┘└─────────────┘└─────────────┘└─────────────┘└────────┘
```

## Data Flow

### Example: Validate Entity Implementation

```
Agent Request
     │
     ├──> 1. koinon-dev.validate_naming("Person")
     │         └──> Response: {"valid": true}
     │
     ├──> 2. koinon-dev.detect_antipatterns(code)
     │         └──> Response: {"patterns": []}
     │
     ├──> 3. postgres.query("SELECT * FROM person LIMIT 1")
     │         └──> Response: {table structure}
     │
     ├──> 4. filesystem.search("Person.cs")
     │         └──> Response: [file paths]
     │
     └──> 5. memory.store("entity-person-validated", {...})
               └──> Response: {"stored": true}

Agent Response: "Entity validation complete ✅"
```

## MCP Server Details

### 1. PostgreSQL Server

```
┌─────────────────────────────────────────┐
│      PostgreSQL MCP Server              │
├─────────────────────────────────────────┤
│ Package: @modelcontextprotocol/        │
│          server-postgres@0.6.2          │
│ Status:  Deprecated but functional      │
│ Port:    N/A (uses stdio)               │
├─────────────────────────────────────────┤
│ Capabilities:                           │
│  • Execute SQL queries                  │
│  • Inspect schema                       │
│  • Validate constraints                 │
│  • Debug data                           │
├─────────────────────────────────────────┤
│ Connection:                             │
│  postgresql://koinon:koinon@           │
│  localhost:5432/koinon                  │
└─────────────────────────────────────────┘
          │
          ▼
┌─────────────────────────────────────────┐
│    Docker Container: koinon-postgres    │
│    PostgreSQL 16 Alpine                 │
│    Port: 5432                           │
└─────────────────────────────────────────┘
```

### 2. Memory Server

```
┌─────────────────────────────────────────┐
│        Memory MCP Server                │
├─────────────────────────────────────────┤
│ Package: @modelcontextprotocol/        │
│          server-memory@2025.11.25       │
│ Status:  Active                         │
│ Storage: Local filesystem               │
├─────────────────────────────────────────┤
│ Capabilities:                           │
│  • Store key-value pairs                │
│  • Retrieve by key                      │
│  • List all keys                        │
│  • Delete keys                          │
├─────────────────────────────────────────┤
│ Use Cases:                              │
│  • Work unit tracking                   │
│  • Architectural decisions              │
│  • Technical debt tracking              │
│  • Session context                      │
└─────────────────────────────────────────┘
```

### 3. GitHub Server

```
┌─────────────────────────────────────────┐
│         GitHub MCP Server               │
├─────────────────────────────────────────┤
│ Package: @modelcontextprotocol/        │
│          server-github@2025.4.8         │
│ Status:  Deprecated but functional      │
│ Auth:    GITHUB_TOKEN env var           │
├─────────────────────────────────────────┤
│ Capabilities:                           │
│  • Create/list issues                   │
│  • Create/review PRs                    │
│  • Add comments                         │
│  • Manage labels                        │
│  • Search code                          │
├─────────────────────────────────────────┤
│ Required Scopes:                        │
│  • repo (full repository access)        │
└─────────────────────────────────────────┘
          │
          ▼
┌─────────────────────────────────────────┐
│         GitHub API (cloud)              │
│         api.github.com                  │
└─────────────────────────────────────────┘
```

### 4. Filesystem Server

```
┌─────────────────────────────────────────┐
│       Filesystem MCP Server             │
├─────────────────────────────────────────┤
│ Package: @modelcontextprotocol/        │
│          server-filesystem@2025.11.25   │
│ Status:  Active                         │
│ Root:    /home/mbrewer/projects/        │
│          koinon-rms                     │
├─────────────────────────────────────────┤
│ Capabilities:                           │
│  • Search files (glob patterns)         │
│  • Read files                           │
│  • Write files                          │
│  • List directories                     │
│  • Get file metadata                    │
├─────────────────────────────────────────┤
│ Security:                               │
│  • Scoped to project directory          │
│  • No access outside project            │
└─────────────────────────────────────────┘
```

### 5. Koinon Dev Server (Custom)

```
┌─────────────────────────────────────────────────────────┐
│         Koinon RMS Development Server                   │
├─────────────────────────────────────────────────────────┤
│ Package: @koinon/mcp-dev-server@1.0.0                   │
│ Status:  Custom - Production Ready                      │
│ Language: TypeScript → JavaScript (ES2022)              │
│ Framework: @modelcontextprotocol/sdk@1.24.3             │
├─────────────────────────────────────────────────────────┤
│ Tools (5):                                              │
│  ├─ validate_naming                                     │
│  │   └─ Types: database, csharp, typescript, route     │
│  ├─ validate_routes                                     │
│  │   └─ Checks: IdKey usage, versioning                │
│  ├─ validate_dependencies                               │
│  │   └─ Validates: Clean architecture rules            │
│  ├─ detect_antipatterns                                 │
│  │   └─ Detects: Legacy patterns, violations           │
│  └─ get_architecture_guidance                           │
│      └─ Topics: entity, api, database, frontend, etc.  │
├─────────────────────────────────────────────────────────┤
│ Resources (4):                                          │
│  • koinon://docs/architecture                           │
│  • koinon://docs/conventions                            │
│  • koinon://docs/entity-design                          │
│  • koinon://docs/api-design                             │
├─────────────────────────────────────────────────────────┤
│ Prompts (3):                                            │
│  • review_entity                                        │
│  • review_api_endpoint                                  │
│  • check_work_unit                                      │
├─────────────────────────────────────────────────────────┤
│ Validation Rules:                                       │
│  Database:  snake_case (person, group_member)           │
│  C#:        PascalCase (Person, FirstName)              │
│  TypeScript: camelCase/PascalCase                       │
│  Routes:    /api/v1/{idKey}                             │
├─────────────────────────────────────────────────────────┤
│ Architecture Rules:                                     │
│  Domain:         No dependencies                        │
│  Application:    → Domain                               │
│  Infrastructure: → Domain, Application                  │
│  Api:            → Domain, Application, Infrastructure  │
└─────────────────────────────────────────────────────────┘
```

## Communication Protocol

### MCP Protocol Flow

```
┌──────────────┐                      ┌──────────────┐
│ Claude Code  │                      │  MCP Server  │
│   (Client)   │                      │              │
└──────┬───────┘                      └──────┬───────┘
       │                                     │
       │  1. Initialize Connection           │
       ├────────────────────────────────────>│
       │                                     │
       │  2. List Available Tools            │
       ├────────────────────────────────────>│
       │<────────────────────────────────────┤
       │  {tools: [...]}                     │
       │                                     │
       │  3. Call Tool                       │
       ├────────────────────────────────────>│
       │  {tool: "validate_naming", ...}     │
       │                                     │
       │                    4. Execute Tool  │
       │                    (internal logic) │
       │                                     │
       │  5. Return Result                   │
       │<────────────────────────────────────┤
       │  {valid: true, issues: []}          │
       │                                     │
       │  6. Close (optional)                │
       ├────────────────────────────────────>│
       │                                     │
```

### Message Format

**Tool Call Request:**
```json
{
  "jsonrpc": "2.0",
  "id": 1,
  "method": "tools/call",
  "params": {
    "name": "validate_naming",
    "arguments": {
      "type": "database",
      "names": ["person", "group_member"]
    }
  }
}
```

**Tool Call Response:**
```json
{
  "jsonrpc": "2.0",
  "id": 1,
  "result": {
    "content": [
      {
        "type": "text",
        "text": "{\"valid\": true, \"issues\": []}"
      }
    ]
  }
}
```

## Multi-Agent Workflow

### Scenario: Implement New Entity

```
┌─────────────────────────────────────────────────────────────────┐
│ Agent 1: Architect                                              │
│  ├─ memory.retrieve("entity-design-patterns")                   │
│  ├─ koinon-dev.get_architecture_guidance("entity")              │
│  ├─ Define: Person entity structure                             │
│  └─ memory.store("entity-person-design", {...})                 │
└─────────────────────────────────────────────────────────────────┘
                              │
                              ▼
┌─────────────────────────────────────────────────────────────────┐
│ Agent 2: Implementation                                         │
│  ├─ memory.retrieve("entity-person-design")                     │
│  ├─ filesystem.read("Entity.cs") // base class                  │
│  ├─ Implement: Person.cs                                        │
│  ├─ koinon-dev.validate_naming(["Person", "FirstName"])         │
│  ├─ koinon-dev.detect_antipatterns(code)                        │
│  └─ filesystem.write("Person.cs")                               │
└─────────────────────────────────────────────────────────────────┘
                              │
                              ▼
┌─────────────────────────────────────────────────────────────────┐
│ Agent 3: Database                                               │
│  ├─ koinon-dev.validate_naming(["person", "first_name"])        │
│  ├─ Create migration                                            │
│  ├─ postgres.query("CREATE TABLE person ...")                   │
│  ├─ postgres.query("SELECT * FROM person LIMIT 1")              │
│  └─ Verify schema                                               │
└─────────────────────────────────────────────────────────────────┘
                              │
                              ▼
┌─────────────────────────────────────────────────────────────────┐
│ Agent 4: Quality Assurance                                      │
│  ├─ filesystem.read("Person.cs")                                │
│  ├─ koinon-dev.review_entity(code)                              │
│  ├─ koinon-dev.detect_antipatterns(code)                        │
│  ├─ postgres.query("DESC person")                               │
│  └─ If issues: github.create_issue(...)                         │
│     Else: memory.store("entity-person-complete", {...})         │
└─────────────────────────────────────────────────────────────────┘
                              │
                              ▼
┌─────────────────────────────────────────────────────────────────┐
│ Agent 5: Integration                                            │
│  ├─ memory.retrieve("entity-person-complete")                   │
│  ├─ git add, commit, push                                       │
│  ├─ github.create_pull_request(...)                             │
│  └─ memory.store("work-unit-1.2.1", {status: "complete"})       │
└─────────────────────────────────────────────────────────────────┘
```

## Server Startup Sequence

```
1. Claude Code Starts
   │
   ├─> Reads config.json
   │   └─> Finds mcpServers configuration
   │
   ├─> For each server:
   │   │
   │   ├─> Spawns process:
   │   │   • postgres:    npx @modelcontextprotocol/server-postgres ...
   │   │   • memory:      npx @modelcontextprotocol/server-memory
   │   │   • github:      npx @modelcontextprotocol/server-github
   │   │   • filesystem:  npx @modelcontextprotocol/server-filesystem ...
   │   │   • koinon-dev:  node tools/mcp-koinon-dev/dist/index.js
   │   │
   │   ├─> Establishes stdio connection
   │   │   (stdin/stdout for communication)
   │   │
   │   ├─> Sends initialize request
   │   │   → Server responds with capabilities
   │   │
   │   ├─> Lists available tools
   │   │   → Server responds with tool definitions
   │   │
   │   └─> Server ready for use
   │
   └─> All servers running
       Claude Code ready for agent requests
```

## Error Handling

### Connection Failures

```
Agent Request
     │
     ├─> Server not responding
     │   └─> Retry (3 attempts)
     │       ├─> Success → Continue
     │       └─> Fail → Report error to agent
     │
     ├─> Server returns error
     │   └─> Parse error message
     │       ├─> Validation error → Inform agent
     │       ├─> Auth error → Check credentials
     │       └─> Unknown → Log and report
     │
     └─> Timeout
         └─> Kill process
             └─> Restart server
```

### Graceful Degradation

```
┌─────────────────────────────────────────────────────────┐
│ If PostgreSQL server fails:                             │
│  • Agent can still use filesystem server                │
│  • Agent can still validate code                        │
│  • Agent notified of database unavailability            │
│  • Manual SQL via psql remains available                │
└─────────────────────────────────────────────────────────┘

┌─────────────────────────────────────────────────────────┐
│ If GitHub server fails:                                 │
│  • Agent can still complete code work                   │
│  • Manual git/GitHub operations remain available        │
│  • Issue creation deferred to manual step               │
└─────────────────────────────────────────────────────────┘

┌─────────────────────────────────────────────────────────┐
│ If Koinon dev server fails:                             │
│  • Agent can still develop code                         │
│  • Manual validation via documentation                  │
│  • Rebuild server: npm run build                        │
└─────────────────────────────────────────────────────────┘
```

## Performance Characteristics

### Latency (Typical)

| Server | Operation | Latency |
|--------|-----------|---------|
| PostgreSQL | Simple query | 10-50ms |
| PostgreSQL | Complex join | 50-200ms |
| Memory | Store | 5-10ms |
| Memory | Retrieve | 5-10ms |
| GitHub | List issues | 200-500ms |
| GitHub | Create issue | 300-800ms |
| Filesystem | Search | 50-200ms |
| Filesystem | Read file | 10-50ms |
| Koinon Dev | Validate | 1-5ms |
| Koinon Dev | Detect patterns | 5-20ms |

### Resource Usage

| Server | Memory | CPU | Disk I/O |
|--------|--------|-----|----------|
| PostgreSQL | Low | Low | Medium |
| Memory | Low | Low | Low |
| GitHub | Low | Low | None |
| Filesystem | Low | Low-Med | Medium |
| Koinon Dev | Low | Low | None |

## Security Architecture

### Isolation Boundaries

```
┌─────────────────────────────────────────────────────────┐
│ Claude Code Process                                     │
│  ├─ Subprocess: postgres server                         │
│  │  └─ Network: localhost:5432 only                     │
│  ├─ Subprocess: memory server                           │
│  │  └─ Storage: Local filesystem (isolated)             │
│  ├─ Subprocess: github server                           │
│  │  └─ Network: api.github.com (HTTPS)                  │
│  │  └─ Auth: Token (env var, not in config)             │
│  ├─ Subprocess: filesystem server                       │
│  │  └─ Scope: Project directory only                    │
│  │  └─ No access to parent directories                  │
│  └─ Subprocess: koinon-dev server                       │
│     └─ No external connections                          │
│     └─ Pure computation                                 │
└─────────────────────────────────────────────────────────┘
```

### Credential Management

```
✅ Good:
  • GitHub token in environment variable
  • Database password in env/config (not code)
  • Tokens not logged
  • No credentials in git

❌ Avoid:
  • Hardcoded tokens
  • Tokens in config.json (use ${ENV_VAR})
  • Sharing tokens in chat
  • Production credentials in development
```

## Deployment Topology

### Development Environment

```
Developer Machine
├── Docker Desktop / Docker Engine
│   ├── koinon-postgres (container)
│   │   └── Port 5432 → localhost:5432
│   └── koinon-redis (container)
│       └── Port 6379 → localhost:6379
│
├── Claude Code
│   ├── MCP Server: postgres → localhost:5432
│   ├── MCP Server: memory → local storage
│   ├── MCP Server: github → api.github.com
│   ├── MCP Server: filesystem → /project/path
│   └── MCP Server: koinon-dev → in-process
│
└── Project Files
    └── /home/mbrewer/projects/koinon-rms
```

### CI/CD Integration (Future)

```
GitHub Actions Runner
├── Docker Containers (ephemeral)
│   ├── postgres (test database)
│   └── redis (test cache)
│
├── MCP Servers (headless mode)
│   ├── postgres → test database
│   ├── memory → temporary storage
│   └── koinon-dev → validation
│
└── Automated Tests
    └── MCP server integration tests
```

## Extension Points

### Adding New Validation Rules

```typescript
// In tools/mcp-koinon-dev/src/index.ts

function detectAntiPatterns(code: string): { patterns: string[] } {
  const patterns: string[] = [];

  // Add new rule here:
  if (/YOUR_PATTERN/.test(code)) {
    patterns.push('CATEGORY: Your message');
  }

  return { patterns };
}
```

### Creating New Custom Server

```bash
# 1. Create directory
mkdir tools/mcp-your-server

# 2. Initialize
cd tools/mcp-your-server
npm init -y

# 3. Install SDK
npm install @modelcontextprotocol/sdk

# 4. Create src/index.ts
# (Use koinon-dev as template)

# 5. Build
npm run build

# 6. Add to config.json
```

## Monitoring and Logging

### Server Logs

Each MCP server writes to stderr:

```bash
# View all server logs
claudecode --debug

# Or check system logs
journalctl -u claudecode  # systemd
~/Library/Logs/Claude Code/  # macOS
```

### Health Checks

```bash
# Check server processes
ps aux | grep mcp-server

# Check database connection
psql postgresql://koinon:koinon@localhost:5432/koinon -c "SELECT 1"

# Check file access
ls /home/mbrewer/projects/koinon-rms

# Check GitHub auth
curl -H "Authorization: token $GITHUB_TOKEN" https://api.github.com/user
```

---

**Architecture Version:** 1.0
**Last Updated:** December 5, 2025
**Status:** Production Ready
