# MCP Quick Test (After Restart)

**Use this for rapid validation after restarting Claude Code**

## Quick Smoke Test (5 minutes)

Run these commands in order:

### 1. Memory MCP
```
mcp__memory__read_graph
```
Expected: Returns JSON (empty or with existing entities)

### 2. Filesystem MCP
```
mcp__filesystem__list_directory with path: "<PROJECT_ROOT>"
```
Expected: Lists project root files/directories

### 3. Postgres MCP
```
mcp__postgres__query with sql: "SELECT 1 as test;"
```
Expected: Returns result with test=1

### 4. GitHub MCP
```
mcp__github__list_issues with owner: "dev-brewery", repo: "koinon-rms", state: "open"
```
Expected: Lists open issues

### 5. Koinon-Dev MCP
```
mcp__koinon-dev__validate_naming with type: "database", names: ["test_table"]
```
Expected: Returns VALID for snake_case

---

## All Pass? ✅

If all 5 tests pass, MCPs are working correctly!

**Note**: Sequential-thinking MCP removed due to schema bug (see MCP-TESTING-BATTERY.md for details)

## Any Fail? ❌

1. Check `.mcp.json` paths are correct
2. Verify `tools/mcp-servers/node_modules/` exists
3. Run full testing battery: See `MCP-TESTING-BATTERY.md`
4. Check error messages in Claude Code output

---

**Full Testing**: See [MCP-TESTING-BATTERY.md](./MCP-TESTING-BATTERY.md)
