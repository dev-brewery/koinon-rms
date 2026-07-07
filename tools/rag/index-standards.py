#!/usr/bin/env python3
"""
Index the governing standards into a dedicated Qdrant collection.

The architect-review gate judges proposals against the canon —
docs/reference/*.md (conventions, api-contracts, entity-mappings, ...) and
docs/adr/*.md — so those documents must be retrievable by relevance. They
live in their own collection (koinon-standards) rather than koinon-code
because that collection's layer/type payload schema is code-shaped and
drives rag_search filters.

Mirrors index-codebase.py: same Qdrant server, same embedding path
(nomic-embed-text, 768-dim via the model gateway), same recreate-collection
semantics, same stable-ID idiom.

Usage:
    python3 tools/rag/index-standards.py
"""
import hashlib
import re
import sys
from pathlib import Path

# Windows consoles default to cp1252, which cannot print the status emoji.
if sys.stdout.encoding and sys.stdout.encoding.lower().replace("-", "") != "utf8":
    sys.stdout.reconfigure(encoding="utf-8")
from qdrant_client import QdrantClient
from qdrant_client.models import PointStruct, Distance, VectorParams

# Import shared utilities
from utils import (
    PROJECT_ROOT,
    CHUNK_SIZE,
    QDRANT_URL,
    get_embeddings,
    OLLAMA_MODEL,
    VECTOR_SIZE,
)

STANDARDS_COLLECTION = "koinon-standards"
STANDARDS_GLOBS = ("docs/reference/*.md", "docs/adr/*.md")
# Not standards: the ADR template is placeholder text, the audit report is a
# point-in-time snapshot, and the work breakdown is planning — indexing them
# would pollute retrieval with non-canon matches.
EXCLUDE_FILES = {
    "docs/adr/template.md",
    "docs/adr/README.md",
    "docs/reference/index-audit-report.md",
    "docs/reference/work-breakdown.md",
}


def determine_doc_type(rel_path: str) -> str:
    """Classify a standards document from its path (posix-normalized)."""
    if rel_path.startswith("docs/adr/"):
        return "adr"
    name = Path(rel_path).name
    if name == "conventions.md":
        return "convention"
    if name == "api-contracts.md":
        return "api-contract"
    if name == "entity-mappings.md":
        return "entity-mapping"
    return "reference"


def chunk_markdown(content: str) -> list[tuple[str, str]]:
    """Split markdown into (section_heading, chunk) pairs.

    Sections break on #/##/### headings so retrieval returns a focused rule,
    not an arbitrary slice; oversized sections are size-capped at CHUNK_SIZE
    like chunk_file does.
    """
    sections: list[tuple[str, str]] = []
    heading = ""
    buf: list[str] = []
    for line in content.splitlines(keepends=True):
        m = re.match(r"^(#{1,3})\s+(.+)", line)
        if m:
            if "".join(buf).strip():
                sections.append((heading, "".join(buf)))
            heading = m.group(2).strip()
            buf = [line]
        else:
            buf.append(line)
    if "".join(buf).strip():
        sections.append((heading, "".join(buf)))

    chunks: list[tuple[str, str]] = []
    for heading, text in sections:
        for i in range(0, len(text), CHUNK_SIZE):
            piece = text[i:i + CHUNK_SIZE]
            if piece.strip():
                chunks.append((heading, piece))
    return chunks


def index_standard(client: QdrantClient, file_path: Path) -> int:
    """Index a single standards document into Qdrant with metadata."""
    try:
        content = file_path.read_text(encoding="utf-8")
    except Exception as e:
        print(f"  ⚠️  Warning: Could not read {file_path}: {e}")
        return 0

    # Posix-normalized so payloads are byte-identical across Windows/Linux.
    rel_path = file_path.relative_to(PROJECT_ROOT).as_posix()
    doc_type = determine_doc_type(rel_path)

    chunks = chunk_markdown(content)
    if not chunks:
        return 0

    try:
        embeddings = get_embeddings([text for _, text in chunks])
    except Exception as e:
        print(f"  ❌ Error getting embeddings for {rel_path}: {e}")
        return 0

    points = []
    for i, ((section, text), embedding) in enumerate(zip(chunks, embeddings)):
        hash_input = f"standards:{rel_path}:chunk-{i}".encode()
        point_id = int(hashlib.md5(hash_input).hexdigest()[:16], 16)

        points.append(PointStruct(
            id=point_id,
            vector=embedding,
            payload={
                "path": rel_path,
                "doc_type": doc_type,
                "section": section,
                "content": text,
                "chunk_index": i,
            }
        ))

    if points:
        client.upsert(collection_name=STANDARDS_COLLECTION, points=points, wait=True)

    return len(points)


def main():
    print("=" * 60)
    print("STANDARDS RAG INDEXING")
    print("=" * 60)

    print(f"\nConnecting to Qdrant ({QDRANT_URL})...")
    client = QdrantClient(url=QDRANT_URL)

    print(f"Using embedding model: {OLLAMA_MODEL} ({VECTOR_SIZE} dimensions)")

    print(f"\nRecreating collection '{STANDARDS_COLLECTION}'...")
    try:
        client.delete_collection(collection_name=STANDARDS_COLLECTION)
    except Exception:
        pass  # Collection might not exist

    client.create_collection(
        collection_name=STANDARDS_COLLECTION,
        vectors_config=VectorParams(size=VECTOR_SIZE, distance=Distance.COSINE)
    )

    print("\nFinding standards documents...")
    doc_files = sorted(
        (
            p for pattern in STANDARDS_GLOBS
            for p in PROJECT_ROOT.glob(pattern)
            if p.relative_to(PROJECT_ROOT).as_posix() not in EXCLUDE_FILES
        ),
        key=lambda p: p.relative_to(PROJECT_ROOT).as_posix(),
    )
    print(f"Found {len(doc_files)} standards documents")

    print("\nIndexing documents...")
    total_chunks = 0
    for file_path in doc_files:
        n = index_standard(client, file_path)
        total_chunks += n
        print(f"  {file_path.relative_to(PROJECT_ROOT).as_posix()}: {n} chunks")

    print(f"\n✅ Indexing complete!")
    print(f"  Documents indexed: {len(doc_files)}")
    print(f"  Total chunks: {total_chunks}")
    print(f"  Collection: {STANDARDS_COLLECTION}")


if __name__ == "__main__":
    main()
