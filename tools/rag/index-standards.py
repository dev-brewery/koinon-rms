#!/usr/bin/env python3
"""
Index the governing standards into a dedicated Qdrant collection.

The architect-review gate judges proposals against the canon —
docs/reference/*.md (conventions, api-contracts, entity-mappings, ...),
docs/adr/*.md, and durable product/refinement decisions under
docs/product/decisions/*.md — so those documents must be retrievable by
relevance. They live in their own collection (koinon-standards) rather than
koinon-code because that collection's layer/type payload schema is
code-shaped and drives rag_search filters.

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
STANDARDS_GLOBS = (
    "docs/reference/*.md",
    "docs/adr/*.md",
    "docs/product/decisions/*.md",
)
# Not standards: templates/placeholders, README indexes, audit snapshots, and
# work breakdown planning pollute retrieval with non-canon matches. Product
# decisions are included only once they are accepted/reviewable markdown docs.
EXCLUDE_FILES = {
    "docs/adr/template.md",
    "docs/adr/README.md",
    "docs/reference/index-audit-report.md",
    "docs/reference/work-breakdown.md",
    "docs/product/decisions/README.md",
    "docs/product/decisions/template.md",
}


def determine_doc_type(rel_path: str) -> str:
    """Classify a standards document from its path (posix-normalized)."""
    if rel_path.startswith("docs/product/decisions/"):
        return "product-decision"
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


def parse_frontmatter(content: str) -> dict[str, str | list[str]]:
    """Parse a tiny YAML-frontmatter subset used by product-decision docs.

    The indexer intentionally avoids a YAML dependency: only `key: value` and
    one-line bracket lists (`[a, b]`) are needed for deterministic retrieval
    payloads. Unknown/malformed lines are ignored; the source doc remains the
    authority.
    """
    if not content.startswith("---\n"):
        return {}
    try:
        _, raw, _ = content.split("---", 2)
    except ValueError:
        return {}
    meta: dict[str, str | list[str]] = {}
    for line in raw.splitlines():
        if ":" not in line or line.startswith(" "):
            continue
        key, value = line.split(":", 1)
        key = key.strip().replace("-", "_")
        value = value.strip().strip('"\'')
        if value.startswith("[") and value.endswith("]"):
            meta[key] = [x.strip().strip('"\'') for x in value[1:-1].split(",") if x.strip()]
        elif value:
            meta[key] = value
    return meta


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
    metadata = parse_frontmatter(content) if doc_type == "product-decision" else {}

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

        payload = {
            "path": rel_path,
            "doc_type": doc_type,
            "section": section,
            "content": text,
            "chunk_index": i,
        }
        if metadata:
            payload.update({
                "decision_id": metadata.get("id", ""),
                "decision_type": metadata.get("decision_type", ""),
                "status": metadata.get("status", ""),
                "applies_to": metadata.get("applies_to", []),
                "date": metadata.get("date", ""),
            })

        points.append(PointStruct(
            id=point_id,
            vector=embedding,
            payload=payload
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
