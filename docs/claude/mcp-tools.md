# MCP Tools Reference

## Available MCP Servers

| Server | Purpose | Token Savings |
|--------|---------|---------------|
| `postgres` | Query DB instead of reading files | 70-99% |
| `memory` | Session context (must read at start) | - |
| `github` | Issues, PRs (owner: dev-brewery) | - |
| `koinon-dev` | RAG search, API graph, naming validation | - |
| `token-optimizer` | Smart caching for file operations | 60-90% |

## RAG-Powered Code Discovery

Semantic code search using Qdrant vector database and Ollama embeddings (nomic-embed-text).

### rag_search
```python
mcp__koinon-dev__rag_search(
    query="entity with family relationship and audit fields",
    filter_layer="Domain",  # Domain|Application|Infrastructure|API|Frontend|all
    filter_type="Entity",   # Entity|DTO|Service|Controller|Component|Hook|Other|all
    limit=5
)
```

### rag_impact_analysis
```python
mcp__koinon-dev__rag_impact_analysis(
    file_path="src/Koinon.Domain/Entities/Person.cs",
    change_description="adding email validation",
    include_tests=True
)
```

### rag_index_status
```python
mcp__koinon-dev__rag_index_status()
```

RAG unavailability never blocks work. Fall back to grep/glob.

## Graph Query Tools

### query_api_graph
| Operation | Required Args |
|-----------|---------------|
| `get_controller_pattern` | entityName |
| `get_entity_chain` | entityName |
| `list_inconsistencies` | none |
| `validate_new_controller` | entityName |

### get_implementation_template
Types: entity, dto, service, controller

### get_impact_analysis
Analyzes affected files and work units for a given file path.

## Token Optimizer

| Standard Tool | Smart Alternative | Savings |
|--------------|-------------------|---------|
| `Read` | `mcp__token-optimizer__smart_read` | 80% |
| `Grep` | `mcp__token-optimizer__smart_grep` | 80% |
| `Glob` | `mcp__token-optimizer__smart_glob` | 75% |

Fallback: try smart tool first, standard tool on failure, auto-degrades after 3 failures.

## Graph Baseline System

Baseline at `tools/graph/graph-baseline.json`. Commands:
```bash
npm run graph:validate   # Validate current code against baseline
npm run graph:update     # Regenerate after structural changes
```

Update baseline when adding: entities, DTOs, endpoints, components, or renaming fields.
