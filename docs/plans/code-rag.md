# Code-RAG MCP Implementation Plan

## Overview

This document describes how to set up and configure the code-rag MCP server using `@mhalder/qdrant-mcp-server` for semantic code search with Qdrant and Ollama embeddings.

## Architecture

```
┌─────────────────┐     ┌─────────────────┐     ┌─────────────────┐
│   Claude Code   │────▶│  code-rag MCP   │────▶│     Qdrant      │
│   (WSL2)        │     │  (Node.js)      │     │  (localhost)    │
└─────────────────┘     └────────┬────────┘     └─────────────────┘
                                 │
                                 ▼
                        ┌─────────────────┐
                        │     Ollama      │
                        │  (Windows host) │
                        └─────────────────┘
```

## Components

| Component | Location | Purpose |
|-----------|----------|---------|
| **Qdrant** | `localhost:6333` | Vector database storing code embeddings |
| **Ollama** | `host.docker.internal:11434` | Embedding generation via `nomic-embed-text` |
| **MCP Server** | `@mhalder/qdrant-mcp-server@1.5.0` | MCP protocol bridge |

## Prerequisites

### 1. Qdrant (Docker)

Add to `docker-compose.yml` if not present:

```yaml
qdrant:
  image: qdrant/qdrant:latest
  container_name: qdrant
  ports:
    - "6333:6333"
  volumes:
    - qdrant_storage:/qdrant/storage
  restart: unless-stopped

volumes:
  qdrant_storage:
```

Start: `docker-compose up -d qdrant`

### 2. Ollama with Embedding Model

On Windows (or host machine):

```powershell
# Install Ollama (if not installed)
# https://ollama.ai/download

# Pull the embedding model (274MB)
ollama pull nomic-embed-text
```

Verify: `curl http://localhost:11434/api/tags | grep nomic`

### 3. MCP Server Package

Already installed in project:

```bash
cd tools/mcp-servers
npm install @mhalder/qdrant-mcp-server
```

## Configuration

### `.mcp.json` Entry

```json
{
  "mcpServers": {
    "code-rag": {
      "command": "node",
      "args": [
        "/home/mbrewer/projects/koinon-rms/tools/mcp-servers/node_modules/@mhalder/qdrant-mcp-server/build/index.js"
      ],
      "env": {
        "QDRANT_URL": "http://localhost:6333",
        "EMBEDDING_PROVIDER": "ollama",
        "EMBEDDING_BASE_URL": "http://host.docker.internal:11434"
      }
    }
  }
}
```

### Environment Variables

| Variable | Value | Description |
|----------|-------|-------------|
| `QDRANT_URL` | `http://localhost:6333` | Qdrant server endpoint |
| `EMBEDDING_PROVIDER` | `ollama` | Use Ollama for embeddings |
| `EMBEDDING_BASE_URL` | `http://host.docker.internal:11434` | Ollama API (Windows from WSL2) |

### WSL2 Networking Note

WSL2 cannot reach Windows `localhost` directly. Use `host.docker.internal` to reach Windows services from WSL2.

## Available Tools

After successful startup, the MCP server provides:

| Tool | Description |
|------|-------------|
| `index_codebase` | Index source files into Qdrant vector store |
| `search_code` | Semantic search across indexed codebase |
| `list_collections` | List all Qdrant collections |
| `delete_collection` | Remove a collection |

## Usage Examples

### Index the Codebase

```
Use the code-rag MCP to index the src/ directory
```

### Search for Code

```
Search the codebase for "authentication middleware"
```

### Check Collections

```
List all code-rag collections
```

## Troubleshooting

### Error: "fetch failed"

**Cause**: Ollama not reachable
**Fix**: Ensure Ollama is running and use `host.docker.internal` instead of `localhost`

### Error: "Model 'nomic-embed-text' not found"

**Cause**: Embedding model not installed
**Fix**: Run `ollama pull nomic-embed-text` on Windows

### Error: "Connection refused to 6333"

**Cause**: Qdrant not running
**Fix**: Run `docker-compose up -d qdrant`

## Performance Notes

- **nomic-embed-text**: 137M parameter model, ~274MB download
- **Indexing speed**: ~100 files/minute (depends on file sizes)
- **Query latency**: <100ms for typical searches
- **Collection size**: koinon-rms-code collection holds the full codebase

## References

- [qdrant-mcp-server GitHub](https://github.com/mhalder/qdrant-mcp-server)
- [Qdrant Documentation](https://qdrant.tech/documentation/)
- [Ollama Embedding Models](https://ollama.ai/library/nomic-embed-text)
- [MCP Protocol Specification](https://modelcontextprotocol.io/)
