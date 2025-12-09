# MCP Testing Battery

**Purpose**: Comprehensive testing checklist for validating all MCP servers work correctly after restart.

**Branch**: `story/mcp-utilization-improvements`

**Test Date**: ___________  
**Tested By**: ___________  
**Result**: ⬜ PASS / ⬜ FAIL

---

## Pre-Test Checklist

Before running tests, verify:

- [ ] Claude Code has been restarted
- [ ] Session verification completed (if hooks require it)
- [ ] PostgreSQL container is running: `docker ps | grep koinon-postgres`
- [ ] All MCP packages installed: `ls tools/mcp-servers/node_modules/@modelcontextprotocol/`
- [ ] `.mcp.json` exists with correct paths (not using example template)

---

## Test 1: Memory MCP (Knowledge Graph)

**MCP Server**: `memory`  
**Package**: `@modelcontextprotocol/server-memory` (v2025.11.25)  
**Entry Point**: `tools/mcp-servers/node_modules/@modelcontextprotocol/server-memory/dist/index.js`

### Test 1.1: Create Entity

**Command**:
```
Create a test entity in the memory MCP:
mcp__memory__create_entities with entities: [{"name": "TestProject", "entityType": "project", "observations": ["This is a test project for MCP validation", "Created during token optimization sprint"]}]
```

**Expected Result**:
- Success response
- Entity "TestProject" created in knowledge graph
- No errors

**Actual Result**: ____________________________

**Status**: ⬜ PASS / ⬜ FAIL

### Test 1.2: Read Graph

**Command**:
```
Read the entire memory graph:
mcp__memory__read_graph
```

**Expected Result**:
- Returns JSON with nodes and edges
- Contains "TestProject" entity created in 1.1
- Shows observations from 1.1

**Actual Result**: ____________________________

**Status**: ⬜ PASS / ⬜ FAIL

### Test 1.3: Search Nodes

**Command**:
```
Search for the test entity:
mcp__memory__search_nodes with query: "TestProject"
```

**Expected Result**:
- Finds "TestProject" entity
- Returns matching observations

**Actual Result**: ____________________________

**Status**: ⬜ PASS / ⬜ FAIL

### Test 1.4: Delete Entity

**Command**:
```
Clean up test entity:
mcp__memory__delete_entities with entityNames: ["TestProject"]
```

**Expected Result**:
- Entity deleted successfully
- Subsequent read_graph should not contain "TestProject"

**Actual Result**: ____________________________

**Status**: ⬜ PASS / ⬜ FAIL

---

## Test 2: Filesystem MCP

**MCP Server**: `filesystem`
**Package**: `@modelcontextprotocol/server-filesystem` (v2025.11.25)
**Entry Point**: `tools/mcp-servers/node_modules/@modelcontextprotocol/server-filesystem/dist/index.js`
**Root Path**: `<PROJECT_ROOT>`

### Test 2.1: List Directory

**Command**:
```
List the tools directory:
mcp__filesystem__list_directory with path: "<PROJECT_ROOT>/tools"
```

**Expected Result**:
- Returns list of directories/files in tools/
- Should include: mcp-servers, mcp-koinon-dev, db-init, gemini-context
- Each item marked as [FILE] or [DIR]

**Actual Result**: ____________________________

**Status**: ⬜ PASS / ⬜ FAIL

### Test 2.2: Read File

**Command**:
```
Read the MCP servers package.json:
mcp__filesystem__read_text_file with path: "<PROJECT_ROOT>/tools/mcp-servers/package.json"
```

**Expected Result**:
- Returns file contents as text
- Should show dependencies for memory, filesystem
- Version: 1.0.0

**Actual Result**: ____________________________

**Status**: ⬜ PASS / ⬜ FAIL

### Test 2.3: Get File Info

**Command**:
```
Get metadata about README:
mcp__filesystem__get_file_info with path: "<PROJECT_ROOT>/README.md"
```

**Expected Result**:
- Returns file metadata (size, modified time, type, permissions)
- Type should be "file"

**Actual Result**: ____________________________

**Status**: ⬜ PASS / ⬜ FAIL

### Test 2.4: Search Files

**Command**:
```
Search for markdown files in docs:
mcp__filesystem__search_files with path: "<PROJECT_ROOT>/docs", pattern: "*.md"
```

**Expected Result**:
- Returns list of .md files in docs directory
- Should include architecture.md, entity-mappings.md, etc.

**Actual Result**: ____________________________

**Status**: ⬜ PASS / ⬜ FAIL

---

## Test 3: Sequential Thinking MCP (REMOVED - BROKEN)

**MCP Server**: `sequential-thinking` ❌ REMOVED
**Package**: `@modelcontextprotocol/server-sequential-thinking` (v2025.11.25)
**Status**: **NOT FUNCTIONAL - Schema Bug**

### Issue Identified

**Root Cause**: The sequential-thinking MCP server has a schema format bug that prevents tool registration in Claude Code.

**Technical Details**:
- Server uses raw Zod fields in `inputSchema`/`outputSchema` instead of wrapping in `z.object()`
- This prevents Claude Code from serializing the schema to JSON Schema
- Result: Tools never appear in the tool list despite server running successfully

**Comparison**:
- ❌ Sequential-thinking: `inputSchema: { thought: z.string(), ... }` (broken)
- ✅ Memory/Filesystem: `inputSchema: z.object({ ... })` (works)

**Resolution**: Removed from `.mcp.json` and `.mcp.json.example`

**Reported**: Schema bug should be reported to: https://github.com/modelcontextprotocol/servers/issues

### Tests Skipped

Tests 3.1 and 3.2 are skipped since the MCP is non-functional.

---

## Test 4: Postgres MCP (Existing - npx)

**MCP Server**: `postgres`  
**Method**: npx (deprecated package, still functional)  
**Connection**: `postgresql://koinon:koinon@localhost:5432/koinon`

### Test 4.1: Query Schema

**Command**:
```
Query the person table schema:
mcp__postgres__query with sql: "SELECT column_name, data_type FROM information_schema.columns WHERE table_name = 'person' ORDER BY ordinal_position LIMIT 5;"
```

**Expected Result**:
- Returns first 5 columns from person table
- Should include: id, guid, id_key, first_name, last_name (or similar)
- Data types shown

**Actual Result**: ____________________________

**Status**: ⬜ PASS / ⬜ FAIL

### Test 4.2: Count Records

**Command**:
```
Count records in person table:
mcp__postgres__query with sql: "SELECT COUNT(*) as person_count FROM person;"
```

**Expected Result**:
- Returns count of records
- Should be 0 or more (depends on seed data)

**Actual Result**: ____________________________

**Status**: ⬜ PASS / ⬜ FAIL

---

## Test 5: GitHub MCP (Existing - Custom Wrapper)

**MCP Server**: `github`  
**Wrapper Script**: `.claude/scripts/github-mcp.sh`  
**Repository**: `dev-brewery/koinon-rms`

### Test 5.1: List Issues

**Command**:
```
List open issues:
mcp__github__list_issues with owner: "dev-brewery", repo: "koinon-rms", state: "open"
```

**Expected Result**:
- Returns list of open issues
- Each issue has title, number, state, labels

**Actual Result**: ____________________________

**Status**: ⬜ PASS / ⬜ FAIL

### Test 5.2: Search Issues

**Command**:
```
Search for token-related issues:
mcp__github__search_issues with q: "repo:dev-brewery/koinon-rms token"
```

**Expected Result**:
- Returns issues mentioning "token"
- Includes this MCP optimization work if issue exists

**Actual Result**: ____________________________

**Status**: ⬜ PASS / ⬜ FAIL

---

## Test 6: Koinon-Dev MCP (Custom Validation)

**MCP Server**: `koinon-dev`  
**Entry Point**: `tools/mcp-koinon-dev/dist/index.js`  
**Type**: Custom TypeScript MCP server

### Test 6.1: Validate Naming Convention

**Command**:
```
Validate database naming (snake_case):
mcp__koinon-dev__validate_naming with type: "database", names: ["person_table", "group_member", "InvalidCamelCase"]
```

**Expected Result**:
- person_table: VALID (snake_case)
- group_member: VALID (snake_case)
- InvalidCamelCase: INVALID (should be snake_case)

**Actual Result**: ____________________________

**Status**: ⬜ PASS / ⬜ FAIL

### Test 6.2: Validate API Route

**Command**:
```
Validate API routes use IdKey:
mcp__koinon-dev__validate_routes with routes: ["/api/v1/people/{idKey}", "/api/v1/person/123"]
```

**Expected Result**:
- /api/v1/people/{idKey}: VALID (uses IdKey)
- /api/v1/person/123: INVALID (uses integer ID)

**Actual Result**: ____________________________

**Status**: ⬜ PASS / ⬜ FAIL

### Test 6.3: Get Architecture Guidance

**Command**:
```
Get guidance on entity design:
mcp__koinon-dev__get_architecture_guidance with topic: "entity"
```

**Expected Result**:
- Returns entity design patterns
- Mentions base Entity class, IEntity interface
- References IdKey pattern

**Actual Result**: ____________________________

**Status**: ⬜ PASS / ⬜ FAIL

---

## Test 7: Token Efficiency Validation

### Test 7.1: Session Startup Token Count

**Procedure**:
1. Note token count at session start (after MCP loading)
2. Compare to baseline: ~400 tokens (npx) vs ~35 tokens (local)

**Expected Result**:
- Tokens used for MCP startup: < 50 tokens
- 91% reduction from npx baseline

**Actual Tokens Used**: ____________________________

**Status**: ⬜ PASS / ⬜ FAIL

### Test 7.2: No npx Downloads

**Command**:
```bash
# Check for recent npx downloads
ls -lt ~/.npm/_cacache/content-v2/sha512/ | head -20
```

**Expected Result**:
- No new downloads for @modelcontextprotocol packages
- Memory, filesystem, sequential-thinking served from local

**Actual Result**: ____________________________

**Status**: ⬜ PASS / ⬜ FAIL

---

## Test 8: Error Handling

### Test 8.1: Invalid Path (Filesystem)

**Command**:
```
Try to read non-existent file:
mcp__filesystem__read_text_file with path: "<PROJECT_ROOT>/this-does-not-exist.txt"
```

**Expected Result**:
- Returns error message
- Does not crash MCP server
- Error is clear and actionable

**Actual Result**: ____________________________

**Status**: ⬜ PASS / ⬜ FAIL

### Test 8.2: Invalid SQL (Postgres)

**Command**:
```
Run invalid SQL query:
mcp__postgres__query with sql: "SELECT * FROM nonexistent_table;"
```

**Expected Result**:
- Returns SQL error
- Does not crash MCP server
- Error mentions table does not exist

**Actual Result**: ____________________________

**Status**: ⬜ PASS / ⬜ FAIL

---

## Overall Test Results

**Total Tests**: 19 (down from 21 - sequential-thinking removed)
**Passed**: _____ / 19
**Failed**: _____ / 19
**Success Rate**: _____%

**Note**: Sequential-thinking MCP tests (3.1, 3.2) removed due to schema bug in server.

### Failed Tests (if any)

List any failed tests and reasons:

1. ________________________________________
2. ________________________________________
3. ________________________________________

### Issues Discovered

Document any issues found during testing:

1. ________________________________________
2. ________________________________________
3. ________________________________________

---

## Rollback Procedure (If Tests Fail)

If critical tests fail, revert MCP configuration:

```bash
# 1. Restore original .mcp.json (with npx)
git restore .mcp.json

# 2. Or manually update .mcp.json to use npx:
# Change "command": "node" back to "command": "npx"
# Change args from local paths back to package names

# 3. Restart Claude Code

# 4. Document failure in GitHub issue
```

---

## Sign-Off

**Tester Signature**: ___________________  
**Date**: ___________________  
**Overall Status**: ⬜ APPROVED FOR MERGE / ⬜ NEEDS FIXES

---

## Notes

Add any additional observations, performance notes, or recommendations:

_____________________________________________
_____________________________________________
_____________________________________________
