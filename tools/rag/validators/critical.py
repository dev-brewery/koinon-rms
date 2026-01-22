"""
Critical architecture validators - PRIMARY BLOCKERS.

These validators MUST pass or the PR is blocked.
They detect semantic violations that regex cannot catch.
"""
import requests
from qdrant_client import QdrantClient
from .helpers import has_business_logic, is_n_plus_one_pattern, extract_api_call


# Constants
OLLAMA_URL = "http://host.docker.internal:11434/api/embed"
OLLAMA_MODEL = "nomic-embed-text"

# Global client instance (initialized once, reused across validators)
client = QdrantClient(url="http://host.docker.internal:6333")


def get_embedding(text):
    """Get embedding from Ollama API for a single text."""
    response = requests.post(
        OLLAMA_URL,
        json={
            "model": OLLAMA_MODEL,
            "input": [f"search_query: {text}"]  # Query prefix for search
        }
    )

    if not response.ok:
        raise Exception(f"Ollama API error: {response.text}")

    data = response.json()
    return data['embeddings'][0]


def validate_no_business_logic_in_controllers():
    """
    Controllers should ONLY handle HTTP concerns, no business logic.

    Business logic belongs in services, not controllers.
    """
    query_vector = get_embedding(
        "controller method with calculations, loops over data, or business rules"
    )

    results = client.query_points(
        collection_name="koinon-code",
        query=query_vector,
        query_filter={"must": [{"key": "layer", "match": {"value": "API"}}]},
        limit=50,
        score_threshold=0.7
    ).points

    violations = []
    for hit in results:
        if has_business_logic(hit.payload['content']):
            violations.append({
                'severity': 'HIGH',
                'file': hit.payload['path'],
                'score': hit.score,
                'message': 'Controller contains business logic - move to service layer',
                'snippet': hit.payload['content'][:200]
            })

    return violations


def validate_no_direct_api_calls_in_components():
    """
    React components must NOT make direct API calls.

    Use hooks/services instead (separation of concerns).
    """
    query_vector = get_embedding(
        "React component making HTTP request with fetch or axios"
    )

    results = client.query_points(
        collection_name="koinon-code",
        query=query_vector,
        query_filter={"must": [{"key": "layer", "match": {"value": "Frontend"}}]},
        limit=50,
        score_threshold=0.7
    ).points

    violations = []
    for hit in results:
        api_call = extract_api_call(hit.payload['content'])
        if api_call:
            violations.append({
                'severity': 'MEDIUM',
                'file': hit.payload['path'],
                'score': hit.score,
                'message': f'Component makes direct API call: {api_call} - use a hook instead',
                'snippet': hit.payload['content'][:200]
            })

    return violations


def detect_n_plus_one_queries():
    """
    Detect N+1 query patterns (loop with DB query inside).

    This is a performance anti-pattern.
    """
    query_vector = get_embedding(
        "foreach loop with database query or repository call inside"
    )

    results = client.query_points(
        collection_name="koinon-code",
        query=query_vector,
        query_filter={"must": [{"key": "layer", "match": {"any": ["Application", "Infrastructure", "API"]}}]},
        limit=50,
        score_threshold=0.7
    ).points

    violations = []
    for hit in results:
        if is_n_plus_one_pattern(hit.payload['content']):
            violations.append({
                'severity': 'HIGH',
                'file': hit.payload['path'],
                'score': hit.score,
                'message': 'N+1 query pattern detected - use Include() or join',
                'snippet': hit.payload['content'][:200]
            })

    return violations


def detect_missing_async():
    """
    Detect missing async/await on EF Core queries.

    All database calls must be async.
    """
    query_vector = get_embedding(
        "EF Core query without async or await like ToList FirstOrDefault Single"
    )

    results = client.query_points(
        collection_name="koinon-code",
        query=query_vector,
        query_filter={"must": [{"key": "layer", "match": {"any": ["Application", "Infrastructure"]}}]},
        limit=50,
        score_threshold=0.7
    ).points

    violations = []
    sync_patterns = ['.ToList()', '.FirstOrDefault()', '.First()', '.Single()', '.SingleOrDefault()']

    for hit in results:
        content = hit.payload['content']
        if any(pattern in content for pattern in sync_patterns):
            # Check if it's NOT followed by async (whitelist // SYNC OK comments)
            if 'await' not in content and '// SYNC OK' not in content:
                violations.append({
                    'severity': 'MEDIUM',
                    'file': hit.payload['path'],
                    'score': hit.score,
                    'message': 'Synchronous EF Core query detected - use async methods',
                    'snippet': hit.payload['content'][:200]
                })

    return violations
