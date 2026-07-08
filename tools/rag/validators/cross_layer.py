"""
Cross-layer validators - check relationships between architectural layers.

Validates that Entity -> DTO -> Service -> Controller chains are complete.
"""
# All endpoints + the embedding protocol live in utils (network server
# defaults, no localhost dependencies — ADR 0005)
from utils import QDRANT_URL, get_embedding

# Optional RAG dependency - graceful degradation if unavailable
try:
    from qdrant_client import QdrantClient
    RAG_AVAILABLE = True
except ImportError:
    RAG_AVAILABLE = False
    QdrantClient = None

# Global client instance
client = QdrantClient(url=QDRANT_URL) if RAG_AVAILABLE else None


def validate_dto_coverage():
    """
    Every Entity should have a corresponding DTO.

    DTOs provide API layer isolation from domain entities.
    """
    if not RAG_AVAILABLE:
        print("⚠️  Skipping (RAG unavailable)")
        return []

    # Find all entities
    entity_results = client.scroll(
        collection_name="koinon-code",
        scroll_filter={
            "must": [{"key": "type", "match": {"value": "Entity"}}]
        },
        limit=100
    )[0]

    # Find all DTOs
    dto_results = client.scroll(
        collection_name="koinon-code",
        scroll_filter={
            "must": [{"key": "type", "match": {"value": "DTO"}}]
        },
        limit=100
    )[0]

    # Extract entity names (e.g., "Person" from "Person.cs")
    entity_names = set()
    for hit in entity_results:
        path = hit.payload['path']
        if '/Entities/' in path and path.endswith('.cs'):
            name = path.split('/')[-1].replace('.cs', '')
            entity_names.add(name)

    # Extract DTO names (e.g., "Person" from "PersonDto.cs")
    dto_names = set()
    for hit in dto_results:
        path = hit.payload['path']
        if 'Dto.cs' in path:
            name = path.split('/')[-1].replace('Dto.cs', '')
            dto_names.add(name)

    # Find entities without DTOs
    violations = []
    missing_dtos = entity_names - dto_names

    for entity_name in missing_dtos:
        violations.append({
            'severity': 'MEDIUM',
            'file': f'Entity: {entity_name}',
            'message': f'Entity "{entity_name}" has no corresponding DTO - DTOs provide API isolation',
            'snippet': ''
        })

    return violations


def validate_controller_uses_services():
    """
    Controllers should inject Services, not Repositories.

    Repositories are infrastructure concerns, controllers use services.
    """
    if not RAG_AVAILABLE:
        print("⚠️  Skipping (RAG unavailable)")
        return []

    query_vector = get_embedding(
        "controller constructor with repository injection instead of service"
    )

    results = client.query_points(
        collection_name="koinon-code",
        query=query_vector,
        query_filter={
            "must": [{"key": "type", "match": {"value": "Controller"}}]
        },
        limit=50,
        score_threshold=0.7
    ).points

    violations = []
    for hit in results:
        content = hit.payload['content']
        # Check for repository injection patterns
        if 'Repository' in content and 'private readonly' in content:
            # Make sure it's not a comment
            if '//' not in content.split('Repository')[0].split('\n')[-1]:
                violations.append({
                    'severity': 'HIGH',
                    'file': hit.payload['path'],
                    'score': hit.score,
                    'message': 'Controller injects Repository directly - should use Service layer',
                    'snippet': hit.payload['content'][:200]
                })

    return violations
