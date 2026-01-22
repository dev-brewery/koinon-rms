#!/usr/bin/env python3
"""
Natural language architecture queries using RAG.

Usage:
    python3 tools/rag/query.py "show all authentication flows"
    python3 tools/rag/query.py "find components that don't use hooks"
    python3 tools/rag/query.py "trace person creation from API to database"

This tool allows you to ask questions about your codebase architecture
using natural language.
"""
import sys
from qdrant_client import QdrantClient
from sentence_transformers import SentenceTransformer


# Initialize model and client
model = SentenceTransformer('BAAI/bge-small-en-v1.5')
client = QdrantClient(url="http://host.docker.internal:6333")


def query_architecture(query: str, limit: int = 10):
    """Query codebase architecture with natural language."""
    query_vector = model.encode(query)

    results = client.search(
        collection_name="koinon-code",
        query_vector=query_vector,
        limit=limit
    )

    print(f"\n🔍 Query: {query}")
    print("=" * 60)

    if not results:
        print("\nNo results found.")
        print("Make sure the codebase has been indexed:")
        print("  python3 tools/rag/index-codebase.py")
        return

    for i, hit in enumerate(results, 1):
        print(f"\n{i}. {hit.payload['path']} (score: {hit.score:.3f})")
        print(f"   Type: {hit.payload.get('type', 'unknown')}")
        print(f"   Layer: {hit.payload.get('layer', 'unknown')}")
        snippet = hit.payload['content'][:150]
        print(f"   Snippet: {snippet}...")


def main():
    if len(sys.argv) < 2:
        print("Usage: python3 tools/rag/query.py 'your question here'")
        print("\nExamples:")
        print("  python3 tools/rag/query.py 'show all authentication flows'")
        print("  python3 tools/rag/query.py 'find components that don\\'t use hooks'")
        print("  python3 tools/rag/query.py 'trace person creation from API to database'")
        sys.exit(1)

    query = " ".join(sys.argv[1:])

    try:
        query_architecture(query)
    except Exception as e:
        print(f"\n❌ Error querying codebase: {e}")
        print("\nMake sure:")
        print("  1. Qdrant is running (docker-compose up -d)")
        print("  2. Codebase has been indexed (python3 tools/rag/index-codebase.py)")
        sys.exit(1)


if __name__ == "__main__":
    main()
