# Ollama MCP Server - Root Cause Analysis & Fix

**Date:** 2025-12-12
**Analyst:** Claude Opus 4.5
**Status:** Root cause identified, fix ready

---

## Executive Summary

The Ollama MCP server tools are not appearing in Claude Code because the wrapper script redirects stdout to a log file, breaking MCP's stdio-based communication protocol.

---

## Root Cause Analysis

### The Bug

**File:** `/home/mbrewer/.claude/scripts/ollama-mcp.sh` (line 28)

```bash
exec "$NODE_EXE" "$MCP_SERVER_WIN" >> "$LOG_FILE" 2>&1
```

This line redirects **both stdout AND stderr** to the log file.

### Why This Breaks MCP

MCP (Model Context Protocol) uses **stdio** for bidirectional communication:

```
Claude Code (WSL2)  <--stdin/stdout-->  MCP Server (Windows Node.js)
```

When the wrapper redirects stdout:

1. Server starts successfully
2. Server outputs MCP initialization JSON with tool definitions
3. **JSON goes to log file instead of Claude Code**
4. Claude Code receives nothing on stdout
5. MCP handshake fails silently
6. Tools don't appear

### Evidence

The debug log (`docs/ollama-gemini/ollama-mcp-debug.log`) shows the server IS working:

```json
{"result":{"protocolVersion":"2025-06-18","capabilities":{"tools":{"ollama_generate":{...}}}}}
```

This JSON should go to Claude Code, but it's captured in the log file instead.

---

## Secondary Issue (Not Blocking)

### Ollama Binding Address

**Current state:** Ollama listens on `127.0.0.1:11434` (Windows localhost only)

```
TCP    127.0.0.1:11434        0.0.0.0:0              LISTENING
```

**Why it doesn't matter for this setup:**
- The MCP server runs on Windows (via `node.exe`)
- Node.js connects to Ollama on Windows localhost
- Both processes are on Windows, so `127.0.0.1` works fine

**When it would matter:**
- If the MCP server ran natively in WSL2
- WSL2 and Windows have separate network stacks
- WSL2 cannot reach Windows' `127.0.0.1`

**Recommendation:** Still configure Ollama to bind to `0.0.0.0` for flexibility:

```typescript
// In startOllamaProcess()
const ollamaProcess = spawn('ollama', ['serve'], {
  detached: true,
  stdio: 'ignore',
  env: { ...process.env, OLLAMA_HOST: '0.0.0.0:11434' }  // Add this
});
```

---

## The Fix

### Option A: Minimal Fix (Recommended)

Only redirect stderr to preserve MCP communication:

**File:** `/home/mbrewer/.claude/scripts/ollama-mcp.sh`

```bash
#!/bin/bash
# Wrapper to invoke Windows Node.js MCP server from WSL2

LOG_FILE="/home/mbrewer/projects/koinon-rms/docs/ollama-gemini/ollama-mcp-debug.log"

echo "--- Starting Ollama MCP Server at $(date) ---" > "$LOG_FILE"

NODE_EXE="/mnt/c/Program Files/nodejs/node.exe"
MCP_SERVER_WSL="/mnt/g/repos/wsl-mcp-ollama/dist/index.js"
MCP_SERVER_WIN=$(wslpath -w "$MCP_SERVER_WSL")

export OLLAMA_HOST="http://localhost:11434"

# FIX: Only redirect stderr (2>>) - stdout must stay connected for MCP
exec "$NODE_EXE" "$MCP_SERVER_WIN" 2>> "$LOG_FILE"
```

### Option B: No Logging (Cleanest)

Remove logging entirely since the server uses `console.error()` internally:

```bash
#!/bin/bash
NODE_EXE="/mnt/c/Program Files/nodejs/node.exe"
MCP_SERVER_WIN=$(wslpath -w "/mnt/g/repos/wsl-mcp-ollama/dist/index.js")
export OLLAMA_HOST="http://localhost:11434"
exec "$NODE_EXE" "$MCP_SERVER_WIN"
```

### Option C: Separate Log Streams

Keep both stdout and a debug log by using process substitution:

```bash
#!/bin/bash
LOG_FILE="/home/mbrewer/projects/koinon-rms/docs/ollama-gemini/ollama-mcp-debug.log"
NODE_EXE="/mnt/c/Program Files/nodejs/node.exe"
MCP_SERVER_WIN=$(wslpath -w "/mnt/g/repos/wsl-mcp-ollama/dist/index.js")
export OLLAMA_HOST="http://localhost:11434"

# Stderr to log, stdout untouched for MCP
exec "$NODE_EXE" "$MCP_SERVER_WIN" 2> >(tee -a "$LOG_FILE" >&2)
```

---

## Verification Steps

After applying the fix:

1. **Restart Claude Code** (MCP servers start on session init)

2. **Check for tools:**
   ```
   # In Claude Code, the following tools should appear:
   mcp__ollama__ollama_generate
   mcp__ollama__ollama_analyze_logs
   mcp__ollama__ollama_review_code
   mcp__ollama__ollama_chat
   ```

3. **Test a tool call:**
   ```
   Use ollama_generate to write a hello world in Python
   ```

4. **Check debug log for errors:**
   ```bash
   tail -20 /home/mbrewer/projects/koinon-rms/docs/ollama-gemini/ollama-mcp-debug.log
   ```

---

## Files Involved

| File | Location | Purpose |
|------|----------|---------|
| `ollama-mcp.sh` | `/home/mbrewer/.claude/scripts/` | WSL2 wrapper (THE BUG) |
| `.mcp.json` | `/home/mbrewer/projects/koinon-rms/.claude/` | MCP server config |
| `index.ts` | `/mnt/g/repos/wsl-mcp-ollama/src/` | MCP server source |
| `ollama-client.ts` | `/mnt/g/repos/wsl-mcp-ollama/src/` | Ollama API client |

---

## Architecture Diagram

```
┌─────────────────────────────────────────────────────────────────┐
│ WSL2                                                            │
│                                                                 │
│  ┌──────────────┐     stdin      ┌─────────────────────────┐   │
│  │ Claude Code  │ ─────────────► │ ollama-mcp.sh wrapper   │   │
│  │              │ ◄───────────── │                         │   │
│  └──────────────┘     stdout     └───────────┬─────────────┘   │
│                       (BROKEN!)              │                  │
│                                              │ exec             │
└──────────────────────────────────────────────┼──────────────────┘
                                               │
                                               ▼
┌──────────────────────────────────────────────────────────────────┐
│ Windows                                                          │
│                                                                  │
│  ┌─────────────────────────┐         ┌─────────────────────┐    │
│  │ Node.js MCP Server      │  HTTP   │ Ollama Server       │    │
│  │ (wsl-mcp-ollama)        │ ──────► │ localhost:11434     │    │
│  │                         │         │                     │    │
│  └─────────────────────────┘         └─────────────────────┘    │
│                                               │                  │
│                                               ▼                  │
│                                      ┌─────────────────┐        │
│                                      │ NVIDIA 1080 Ti  │        │
│                                      │ (GPU inference) │        │
│                                      └─────────────────┘        │
└──────────────────────────────────────────────────────────────────┘
```

---

## Conclusion

**Single line fix required:** Change `>> "$LOG_FILE" 2>&1` to `2>> "$LOG_FILE"`

The stdout redirection was added for debugging but inadvertently broke MCP communication. The fix preserves debug logging while restoring the stdio channel needed for MCP.
