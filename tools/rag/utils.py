"""
Shared utilities for RAG indexing system.

Used by both index-codebase.py and reindex-changes.py.
"""
from pathlib import Path


# Constants
PROJECT_ROOT = Path(__file__).parent.parent.parent
COLLECTION_NAME = "koinon-code"
CHUNK_SIZE = 1000  # Characters per chunk
OLLAMA_URL = "http://host.docker.internal:11434/api/embed"
OLLAMA_MODEL = "nomic-embed-text"
VECTOR_SIZE = 768  # nomic-embed-text produces 768-dimensional vectors

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
