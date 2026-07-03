"""
Shared utilities for RAG indexing system.

Used by both index-codebase.py and reindex-changes.py.
"""
import os
from pathlib import Path

import requests


# Constants
# ALL RAG endpoints live on the team's inference server — no localhost
# dependencies (ADR 0005). Env vars override for exceptional setups only.
PROJECT_ROOT = Path(__file__).parent.parent.parent
COLLECTION_NAME = "koinon-code"
CHUNK_SIZE = 1000  # Characters per chunk
RAG_HOST = os.environ.get("RAG_HOST", "192.168.1.225")
QDRANT_URL = os.environ.get("QDRANT_URL", f"http://{RAG_HOST}:6333")
# Embeddings come from the model gateway on :4000 (OpenAI-compatible
# /v1/embeddings, also proxies the chat models).
EMBEDDINGS_URL = (
    os.environ.get("EMBEDDINGS_URL")
    or os.environ.get("OLLAMA_URL")  # legacy name, still honored
    or f"http://{RAG_HOST}:4000"
).rstrip("/")
OLLAMA_MODEL = "nomic-embed-text"
VECTOR_SIZE = 768  # nomic-embed-text produces 768-dimensional vectors


def get_embeddings(texts, prefix="search_document"):
    """Embed texts against the inference server, speaking whichever protocol
    it exposes: Ollama-style /api/embed first, then OpenAI-style
    /v1/embeddings (llama.cpp --embeddings, TEI, vLLM). The index and all
    queries MUST use the same model (nomic-embed-text, 768-dim)."""
    inputs = [f"{prefix}: {t}" for t in texts]

    resp = requests.post(
        f"{EMBEDDINGS_URL}/api/embed",
        json={"model": OLLAMA_MODEL, "input": inputs},
        timeout=120,
    )
    if resp.ok:
        return resp.json()["embeddings"]

    resp2 = requests.post(
        f"{EMBEDDINGS_URL}/v1/embeddings",
        json={"model": OLLAMA_MODEL, "input": inputs},
        timeout=120,
    )
    if resp2.ok:
        return [d["embedding"] for d in resp2.json()["data"]]

    raise Exception(
        f"No embeddings API at {EMBEDDINGS_URL} "
        f"(/api/embed -> {resp.status_code}, /v1/embeddings -> {resp2.status_code}). "
        f"The inference server must expose nomic-embed-text via Ollama or an "
        f"OpenAI-compatible endpoint."
    )


def get_embedding(text, prefix="search_query"):
    """Embed a single query string."""
    return get_embeddings([text], prefix=prefix)[0]

# Exclusions
EXCLUDE_DIRS = {
    'node_modules', 'bin', 'obj', '.git', 'dist', 'venv-rag',
    '.venv', '__pycache__', 'htmlcov', '.pytest_cache', '.claude'
}

FILE_EXTENSIONS = {'.cs', '.ts', '.tsx', '.js', '.jsx'}


def determine_layer(file_path: str) -> str:
    """Determine architectural layer from file path."""
    if '/Koinon.Domain/' in file_path:
        return 'Domain'
    elif '/Koinon.Application/' in file_path:
        return 'Application'
    elif '/Koinon.Infrastructure/' in file_path:
        return 'Infrastructure'
    elif '/Koinon.Api/' in file_path:
        return 'API'
    elif '/src/web/' in file_path:
        return 'Frontend'
    else:
        return 'Other'


def determine_type(file_path: str) -> str:
    """Determine code type from file path."""
    if '/Entities/' in file_path and file_path.endswith('.cs'):
        return 'Entity'
    elif '/DTOs/' in file_path or 'Dto.cs' in file_path:
        return 'DTO'
    elif 'Controller.cs' in file_path:
        return 'Controller'
    elif 'Service.cs' in file_path or '/Services/' in file_path:
        return 'Service'
    elif 'Repository.cs' in file_path or '/Repositories/' in file_path:
        return 'Repository'
    elif file_path.endswith(('.tsx', '.jsx')) and '/components/' in file_path.lower():
        return 'Component'
    elif file_path.endswith('.ts') and '/hooks/' in file_path.lower():
        return 'Hook'
    else:
        return 'Other'


def chunk_file(content: str) -> list[str]:
    """Split file content into chunks."""
    chunks = []
    for i in range(0, len(content), CHUNK_SIZE):
        chunk = content[i:i + CHUNK_SIZE]
        if chunk.strip():  # Skip empty chunks
            chunks.append(chunk)
    return chunks
