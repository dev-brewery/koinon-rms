#!/usr/bin/env python3
"""
Smart RAG indexing with metadata using Ollama API.

Indexes the entire codebase into Qdrant with structured metadata for filtering.
Uses Ollama's nomic-embed-text model (768 dimensions) via API.

Usage:
    python3 tools/rag/index-codebase.py

This indexes all relevant code files with metadata:
- path: File path
- layer: Domain | Application | Infrastructure | API | Frontend
- type: Entity | DTO | Controller | Service | Component | Hook
- content: Code chunk
"""
import os
import hashlib
import requests
from pathlib import Path
from qdrant_client import QdrantClient
from qdrant_client.models import PointStruct, Distance, VectorParams

# Import shared utilities
from utils import (
    PROJECT_ROOT,
    COLLECTION_NAME,
    CHUNK_SIZE,
    OLLAMA_URL,
    OLLAMA_MODEL,
    VECTOR_SIZE,
    EXCLUDE_DIRS,
    FILE_EXTENSIONS,
    determine_layer,
    determine_type,
    chunk_file
)


def get_embeddings(texts):
    """Get embeddings from Ollama API."""
    # Prefix for better retrieval (as done in rag-indexer.js)
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


def index_file(client: QdrantClient, file_path: Path):
    """Index a single file into Qdrant with metadata."""
    try:
        content = file_path.read_text(encoding='utf-8')
    except Exception as e:
        print(f"  ⚠️  Warning: Could not read {file_path}: {e}")
        return 0

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
        # Generate stable UUID from file path and chunk index
        hash_input = f"{rel_path}:chunk-{i}".encode()
        point_id = int(hashlib.md5(hash_input).hexdigest()[:16], 16)

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

    # Upsert to Qdrant
    if points:
        client.upsert(collection_name=COLLECTION_NAME, points=points, wait=True)

    return len(points)


def main():
    print("=" * 60)
    print("SMART RAG INDEXING (Ollama API)")
    print("=" * 60)

    # Initialize client
    print(f"\nConnecting to Qdrant (host.docker.internal:6333)...")
    client = QdrantClient(url="http://host.docker.internal:6333")

    print(f"Using Ollama model: {OLLAMA_MODEL} ({VECTOR_SIZE} dimensions)")

    # Create or recreate collection
    print(f"\nRecreating collection '{COLLECTION_NAME}'...")
    try:
        client.delete_collection(collection_name=COLLECTION_NAME)
    except Exception:
        pass  # Collection might not exist

    client.create_collection(
        collection_name=COLLECTION_NAME,
        vectors_config=VectorParams(size=VECTOR_SIZE, distance=Distance.COSINE)
    )

    # Find all code files
    print("\nFinding code files...")
    code_files = []

    for ext in FILE_EXTENSIONS:
        for file_path in PROJECT_ROOT.rglob(f"*{ext}"):
            # Skip excluded directories
            if any(excluded in file_path.parts for excluded in EXCLUDE_DIRS):
                continue

            code_files.append(file_path)

    print(f"Found {len(code_files)} code files")

    # Index files
    print("\nIndexing files...")
    total_chunks = 0

    for i, file_path in enumerate(code_files, 1):
        chunks = index_file(client, file_path)
        total_chunks += chunks

        if i % 10 == 0:
            print(f"  Indexed {i}/{len(code_files)} files ({total_chunks} chunks)")

    print(f"\n✅ Indexing complete!")
    print(f"  Files indexed: {len(code_files)}")
    print(f"  Total chunks: {total_chunks}")
    print(f"  Collection: {COLLECTION_NAME}")


if __name__ == "__main__":
    main()
