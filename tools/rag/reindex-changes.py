#!/usr/bin/env python3
"""
Incremental RAG reindexing - only changed files.

Reindexes only files that have changed since last commit.
Much faster than full reindex (5s vs 60s).

Usage:
    python3 tools/rag/reindex-changes.py

Called automatically by tools/rag/validate.py before running validators.
"""
import os
import sys
import subprocess
import hashlib
import requests
from pathlib import Path
from qdrant_client import QdrantClient
from qdrant_client.models import PointStruct

# Import shared utilities
from utils import (
    determine_layer,
    determine_type,
    chunk_file,
    COLLECTION_NAME,
    EXCLUDE_DIRS,
    FILE_EXTENSIONS,
    PROJECT_ROOT,
    OLLAMA_URL,
    OLLAMA_MODEL
)


def get_embeddings(texts):
    """Get embeddings from Ollama API."""
    # Prefix for better retrieval (as done in index-codebase.py)
    inputs = [f"search_document: {text}" for text in texts]

    response = requests.post(
        OLLAMA_URL,
        json={
            "model": OLLAMA_MODEL,
            "input": inputs
        }
    )

    if not response.ok:
        raise Exception(f"Ollama API error: {response.text}")

    data = response.json()
    return data['embeddings']


def get_changed_files() -> list[Path]:
    """
    Get list of changed files using git diff.

    Returns files that are:
    - Modified (M)
    - Added (A)
    - Renamed (R)

    Excludes deleted files.
    """
    try:
        # Get changed files in working directory
        result = subprocess.run(
            ['git', 'diff', '--name-only', '--diff-filter=MAR'],
            cwd=PROJECT_ROOT,
            capture_output=True,
            text=True,
            check=True
        )

        changed_files = result.stdout.strip().split('\n')

        # Also get staged files
        staged_result = subprocess.run(
            ['git', 'diff', '--cached', '--name-only', '--diff-filter=MAR'],
            cwd=PROJECT_ROOT,
            capture_output=True,
            text=True,
            check=True
        )

        staged_files = staged_result.stdout.strip().split('\n')

        # Combine and deduplicate
        all_files = set(changed_files + staged_files)
        all_files.discard('')  # Remove empty strings

        # Filter for relevant extensions and exclude directories
        relevant_files = []
        for file_path in all_files:
            path = PROJECT_ROOT / file_path

            # Check extension
            if path.suffix not in FILE_EXTENSIONS:
                continue

            # Check excluded directories
            if any(excluded in path.parts for excluded in EXCLUDE_DIRS):
                continue

            # Check file exists (not deleted)
            if path.exists():
                relevant_files.append(path)

        return relevant_files

    except subprocess.CalledProcessError:
        # Not in a git repo or other git error
        return []


def delete_file_from_index(client: QdrantClient, file_path: Path):
    """
    Delete all chunks for a file from the index.

    This ensures we don't have stale chunks if file was modified.
    """
    rel_path = str(file_path.relative_to(PROJECT_ROOT))

    # Delete all points with this path
    client.delete(
        collection_name=COLLECTION_NAME,
        points_selector={
            "filter": {
                "must": [
                    {"key": "path", "match": {"value": rel_path}}
                ]
            }
        }
    )


def reindex_file(client: QdrantClient, file_path: Path):
    """
    Reindex a single file.

    1. Delete existing chunks for this file
    2. Index new chunks
    """
    # Delete old chunks
    delete_file_from_index(client, file_path)

    # Index new chunks (reuse logic from index-codebase.py)
    try:
        content = file_path.read_text(encoding='utf-8')
    except (UnicodeDecodeError, PermissionError):
        return 0  # Skip binary or inaccessible files

    # Determine metadata
    rel_path = str(file_path.relative_to(PROJECT_ROOT))
    layer = determine_layer(rel_path)
    file_type = determine_type(rel_path)

    # Chunk file
    chunks = chunk_file(content)
    if not chunks:
        return 0

    # Get embeddings from Ollama
    try:
        embeddings = get_embeddings(chunks)
    except Exception as e:
        print(f"  ❌ Error getting embeddings for {rel_path}: {e}")
        return 0

    # Create points
    points = []
    for i, (chunk, embedding) in enumerate(zip(chunks, embeddings)):
        point_id = int(hashlib.md5(f"{rel_path}:chunk-{i}".encode()).hexdigest()[:16], 16)

        points.append(PointStruct(
            id=point_id,
            vector=embedding,
            payload={
                "path": rel_path,
                "layer": layer,
                "type": file_type,
                "content": chunk,
                "chunk_index": i
            }
        ))

    # Upsert points
    if points:
        client.upsert(collection_name=COLLECTION_NAME, points=points, wait=True)

    return len(points)


def main():
    print("=" * 60)
    print("INCREMENTAL RAG REINDEXING")
    print("=" * 60)

    # Get changed files
    print("\nFinding changed files...")
    changed_files = get_changed_files()

    if not changed_files:
        print("No changed files to reindex.")
        return

    print(f"Found {len(changed_files)} changed files")

    # Initialize client
    print(f"\nConnecting to Qdrant (host.docker.internal:6333)...")
    print(f"Using Ollama model: {OLLAMA_MODEL}")
    client = QdrantClient(url="http://host.docker.internal:6333")

    # Verify collection exists
    try:
        client.get_collection(collection_name=COLLECTION_NAME)
    except Exception:
        print(f"\n⚠️  Collection '{COLLECTION_NAME}' does not exist.")
        print("Run full indexing first:")
        print("  npm run rag:index")
        sys.exit(1)

    # Reindex changed files
    print("\nReindexing changed files...")
    total_chunks = 0

    for file_path in changed_files:
        chunks = reindex_file(client, file_path)
        total_chunks += chunks
        print(f"  ✓ {file_path.relative_to(PROJECT_ROOT)} ({chunks} chunks)")

    print(f"\n✅ Incremental reindexing complete!")
    print(f"  Files reindexed: {len(changed_files)}")
    print(f"  Total chunks: {total_chunks}")


if __name__ == "__main__":
    main()
