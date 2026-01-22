/**
 * RAG Client for Koinon RMS MCP Server
 *
 * Provides semantic code search using Qdrant vector database and Ollama embeddings.
 * Matches constants from tools/rag/utils.py for consistency.
 *
 * All functions implement graceful degradation - they return empty results
 * with warnings when infrastructure is unavailable, never throwing errors
 * that would block agent workflows.
 */

// Constants matching tools/rag/utils.py
const COLLECTION_NAME = 'koinon-code';
const OLLAMA_URL = 'http://host.docker.internal:11434/api/embed';
const OLLAMA_MODEL = 'nomic-embed-text';
const VECTOR_SIZE = 768;
const QDRANT_URL = 'http://localhost:6333';
const REQUEST_TIMEOUT = 5000; // 5 seconds

// Types
export interface RagSearchResult {
  path: string;
  layer: string;
  type: string;
  score: number;
  snippet: string;
}

export interface RagSearchResponse {
  success: boolean;
  results: RagSearchResult[];
  warning?: string;
  query: string;
  filters: {
    layer?: string;
    type?: string;
  };
}

export interface RagIndexStatus {
  healthy: boolean;
  qdrant_available: boolean;
  ollama_available: boolean;
  collection_name: string;
  chunks_count: number;
  vector_size: number;
  warning?: string;
}

export interface RagImpactResult {
  success: boolean;
  file_path: string;
  change_description?: string;
  semantic_matches: RagSearchResult[];
  related_tests: RagSearchResult[];
  warning?: string;
}

/**
 * Get embedding vector from Ollama for a query string.
 * Uses 'search_query:' prefix for query embeddings (matching Python utils).
 */
async function getEmbedding(text: string): Promise<number[] | null> {
  try {
    const controller = new AbortController();
    const timeoutId = setTimeout(() => controller.abort(), REQUEST_TIMEOUT);

    const response = await fetch(OLLAMA_URL, {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({
        model: OLLAMA_MODEL,
        input: [`search_query: ${text}`]
      }),
      signal: controller.signal
    });

    clearTimeout(timeoutId);

    if (!response.ok) {
      console.error(`Ollama API error: ${response.status} ${response.statusText}`);
      return null;
    }

    const data = await response.json() as { embeddings?: number[][] };
    return data.embeddings?.[0] || null;
  } catch (error) {
    if (error instanceof Error && error.name === 'AbortError') {
      console.error('Ollama request timed out');
    } else {
      console.error('Ollama embedding error:', error);
    }
    return null;
  }
}

/**
 * Check if Qdrant is available.
 */
async function isQdrantAvailable(): Promise<boolean> {
  try {
    const controller = new AbortController();
    const timeoutId = setTimeout(() => controller.abort(), REQUEST_TIMEOUT);

    const response = await fetch(`${QDRANT_URL}/health`, {
      signal: controller.signal
    });

    clearTimeout(timeoutId);
    return response.ok;
  } catch {
    return false;
  }
}

/**
 * Check if Ollama is available.
 */
async function isOllamaAvailable(): Promise<boolean> {
  try {
    const controller = new AbortController();
    const timeoutId = setTimeout(() => controller.abort(), REQUEST_TIMEOUT);

    const response = await fetch('http://host.docker.internal:11434/api/tags', {
      signal: controller.signal
    });

    clearTimeout(timeoutId);
    return response.ok;
  } catch {
    return false;
  }
}

/**
 * Semantic code search using RAG.
 *
 * @param query - Natural language query (e.g., "person validation with email rules")
 * @param filterLayer - Filter by architectural layer (Domain|Application|Infrastructure|API|Frontend|all)
 * @param filterType - Filter by code type (Entity|DTO|Service|Controller|Component|Hook|all)
 * @param limit - Maximum results to return (default: 10)
 */
export async function searchRag(
  query: string,
  filterLayer?: string,
  filterType?: string,
  limit: number = 10
): Promise<RagSearchResponse> {
  // Check Qdrant availability
  if (!await isQdrantAvailable()) {
    return {
      success: false,
      results: [],
      warning: 'Qdrant unavailable - fall back to grep/glob',
      query,
      filters: { layer: filterLayer, type: filterType }
    };
  }

  // Get embedding from Ollama
  const embedding = await getEmbedding(query);
  if (!embedding) {
    return {
      success: false,
      results: [],
      warning: 'Ollama unavailable for embeddings - fall back to grep/glob',
      query,
      filters: { layer: filterLayer, type: filterType }
    };
  }

  // Build Qdrant filter
  const must: Array<{ key: string; match: { value: string } }> = [];
  if (filterLayer && filterLayer !== 'all') {
    must.push({ key: 'layer', match: { value: filterLayer } });
  }
  if (filterType && filterType !== 'all') {
    must.push({ key: 'type', match: { value: filterType } });
  }

  const filter = must.length > 0 ? { must } : undefined;

  try {
    const controller = new AbortController();
    const timeoutId = setTimeout(() => controller.abort(), REQUEST_TIMEOUT);

    const response = await fetch(`${QDRANT_URL}/collections/${COLLECTION_NAME}/points/search`, {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({
        vector: embedding,
        filter,
        limit,
        with_payload: true
      }),
      signal: controller.signal
    });

    clearTimeout(timeoutId);

    if (!response.ok) {
      return {
        success: false,
        results: [],
        warning: `Qdrant search failed: ${response.status} ${response.statusText}`,
        query,
        filters: { layer: filterLayer, type: filterType }
      };
    }

    const data = await response.json() as { result?: Array<{ payload?: Record<string, unknown>; score?: number }> };
    const results: RagSearchResult[] = (data.result || []).map((r) => ({
      path: String(r.payload?.path || 'unknown'),
      layer: String(r.payload?.layer || 'unknown'),
      type: String(r.payload?.type || 'unknown'),
      score: r.score || 0,
      snippet: String(r.payload?.content || '').substring(0, 300)
    }));

    return {
      success: true,
      results,
      query,
      filters: { layer: filterLayer, type: filterType }
    };
  } catch (error) {
    return {
      success: false,
      results: [],
      warning: `Qdrant search error: ${error instanceof Error ? error.message : 'unknown error'}`,
      query,
      filters: { layer: filterLayer, type: filterType }
    };
  }
}

/**
 * Get RAG index status and health information.
 */
export async function getRagStatus(): Promise<RagIndexStatus> {
  const qdrantAvailable = await isQdrantAvailable();
  const ollamaAvailable = await isOllamaAvailable();

  if (!qdrantAvailable || !ollamaAvailable) {
    return {
      healthy: false,
      qdrant_available: qdrantAvailable,
      ollama_available: ollamaAvailable,
      collection_name: COLLECTION_NAME,
      chunks_count: 0,
      vector_size: VECTOR_SIZE,
      warning: `${!qdrantAvailable ? 'Qdrant' : ''}${!qdrantAvailable && !ollamaAvailable ? ' and ' : ''}${!ollamaAvailable ? 'Ollama' : ''} unavailable`
    };
  }

  try {
    const controller = new AbortController();
    const timeoutId = setTimeout(() => controller.abort(), REQUEST_TIMEOUT);

    const response = await fetch(`${QDRANT_URL}/collections/${COLLECTION_NAME}`, {
      signal: controller.signal
    });

    clearTimeout(timeoutId);

    if (!response.ok) {
      return {
        healthy: false,
        qdrant_available: true,
        ollama_available: true,
        collection_name: COLLECTION_NAME,
        chunks_count: 0,
        vector_size: VECTOR_SIZE,
        warning: `Collection ${COLLECTION_NAME} not found - run: python3 tools/rag/index-codebase.py`
      };
    }

    const data = await response.json() as { result?: { points_count?: number; config?: { params?: { vectors?: { size?: number } } } } };
    const collection = data.result;

    return {
      healthy: true,
      qdrant_available: true,
      ollama_available: true,
      collection_name: COLLECTION_NAME,
      chunks_count: collection?.points_count || 0,
      vector_size: collection?.config?.params?.vectors?.size || VECTOR_SIZE
    };
  } catch (error) {
    return {
      healthy: false,
      qdrant_available: qdrantAvailable,
      ollama_available: ollamaAvailable,
      collection_name: COLLECTION_NAME,
      chunks_count: 0,
      vector_size: VECTOR_SIZE,
      warning: `Failed to get collection info: ${error instanceof Error ? error.message : 'unknown error'}`
    };
  }
}

/**
 * RAG-enhanced impact analysis.
 * Finds semantically related code and tests for a given file.
 *
 * @param filePath - File path to analyze
 * @param changeDescription - Optional description of planned changes for better matching
 * @param includeTests - Whether to search for related tests (default: true)
 */
export async function getRagImpactAnalysis(
  filePath: string,
  changeDescription?: string,
  includeTests: boolean = true
): Promise<RagImpactResult> {
  // Extract entity/component name from file path
  const entityMatch = filePath.match(/\/([^/]+?)(?:\.cs|\.ts|\.tsx)$/);
  const entityName = entityMatch ? entityMatch[1].replace(/Controller|Service|Dto|Repository/, '') : '';

  // Build semantic query based on file path and change description
  let semanticQuery = entityName;
  if (changeDescription) {
    semanticQuery = `${entityName} ${changeDescription}`;
  }

  // Determine layer from file path
  let layer: string | undefined;
  if (filePath.includes('Koinon.Domain')) layer = 'Domain';
  else if (filePath.includes('Koinon.Application')) layer = 'Application';
  else if (filePath.includes('Koinon.Infrastructure')) layer = 'Infrastructure';
  else if (filePath.includes('Koinon.Api')) layer = 'API';
  else if (filePath.includes('src/web')) layer = 'Frontend';

  // Search for semantically related code
  const semanticResults = await searchRag(semanticQuery, undefined, undefined, 15);

  // Search for related tests if requested
  let testResults: RagSearchResult[] = [];
  if (includeTests && semanticResults.success) {
    const testQuery = `${entityName} test spec`;
    const testSearch = await searchRag(testQuery, undefined, undefined, 10);
    if (testSearch.success) {
      testResults = testSearch.results.filter(r =>
        r.path.includes('.test.') ||
        r.path.includes('.spec.') ||
        r.path.includes('/tests/') ||
        r.path.includes('Tests.cs')
      );
    }
  }

  // Filter out the source file itself from results
  const filteredResults = semanticResults.results.filter(r =>
    !r.path.endsWith(filePath.split('/').pop() || '')
  );

  return {
    success: semanticResults.success,
    file_path: filePath,
    change_description: changeDescription,
    semantic_matches: filteredResults,
    related_tests: testResults,
    warning: semanticResults.warning
  };
}
