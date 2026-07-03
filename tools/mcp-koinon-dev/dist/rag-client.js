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
// Constants matching tools/rag/utils.py.
// ALL RAG endpoints live on the team's inference server — no localhost
// dependencies (ADR 0005). The embeddings client speaks both protocols
// (Ollama /api/embed and OpenAI /v1/embeddings) so whichever server-side
// embedder is enabled works without a repo change. Env vars override for
// exceptional setups only.
const COLLECTION_NAME = 'koinon-code';
const RAG_HOST = process.env.RAG_HOST || '192.168.1.225';
// Embeddings are served by the model gateway on :4000 (OpenAI-compatible
// /v1/embeddings; the Ollama-protocol attempt below is a harmless fast miss).
const EMBEDDINGS_BASE_URL = (process.env.EMBEDDINGS_URL || process.env.OLLAMA_URL || `http://${RAG_HOST}:4000`).replace(/\/+$/, '');
const OLLAMA_MODEL = 'nomic-embed-text';
const VECTOR_SIZE = 768;
const QDRANT_URL = process.env.QDRANT_URL || `http://${RAG_HOST}:6333`;
const REQUEST_TIMEOUT = 5000; // 5 seconds
/**
 * Get embedding vector from the inference server. nomic prefixes:
 * 'search_query:' for queries, 'search_document:' when storing documents
 * (matching Python utils). Tries the Ollama protocol (/api/embed) first,
 * then OpenAI-compatible (/v1/embeddings). Index and queries must share
 * the model: nomic-embed-text, 768-dim.
 */
async function getEmbedding(text, prefix = 'search_query') {
    const input = `${prefix}: ${text}`;
    const post = async (url, body) => {
        const controller = new AbortController();
        const timeoutId = setTimeout(() => controller.abort(), REQUEST_TIMEOUT);
        try {
            return await fetch(url, {
                method: 'POST',
                headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify(body),
                signal: controller.signal
            });
        }
        catch (error) {
            if (error instanceof Error && error.name === 'AbortError') {
                console.error(`Embeddings request timed out: ${url}`);
            }
            else {
                console.error(`Embeddings request failed: ${url}`, error);
            }
            return null;
        }
        finally {
            clearTimeout(timeoutId);
        }
    };
    // Ollama protocol
    const ollamaRes = await post(`${EMBEDDINGS_BASE_URL}/api/embed`, {
        model: OLLAMA_MODEL,
        input: [input]
    });
    if (ollamaRes?.ok) {
        const data = await ollamaRes.json();
        if (data.embeddings?.[0])
            return data.embeddings[0];
    }
    // OpenAI-compatible protocol
    const openaiRes = await post(`${EMBEDDINGS_BASE_URL}/v1/embeddings`, {
        model: OLLAMA_MODEL,
        input: [input]
    });
    if (openaiRes?.ok) {
        const data = await openaiRes.json();
        if (data.data?.[0]?.embedding)
            return data.data[0].embedding;
    }
    console.error(`No embeddings API at ${EMBEDDINGS_BASE_URL} ` +
        `(/api/embed -> ${ollamaRes?.status ?? 'unreachable'}, ` +
        `/v1/embeddings -> ${openaiRes?.status ?? 'unreachable'})`);
    return null;
}
/**
 * Check if Qdrant is available.
 */
async function isQdrantAvailable() {
    try {
        const controller = new AbortController();
        const timeoutId = setTimeout(() => controller.abort(), REQUEST_TIMEOUT);
        // Qdrant serves /healthz (a probe of /health 404s and reads as "down")
        const response = await fetch(`${QDRANT_URL}/healthz`, {
            signal: controller.signal
        });
        clearTimeout(timeoutId);
        return response.ok;
    }
    catch {
        return false;
    }
}
/**
 * Check if an embeddings API is available (either protocol).
 */
async function isOllamaAvailable() {
    const probe = async (path) => {
        try {
            const controller = new AbortController();
            const timeoutId = setTimeout(() => controller.abort(), REQUEST_TIMEOUT);
            const response = await fetch(`${EMBEDDINGS_BASE_URL}${path}`, {
                signal: controller.signal
            });
            clearTimeout(timeoutId);
            return response.ok;
        }
        catch {
            return false;
        }
    };
    return (await probe('/api/tags')) || (await probe('/v1/models'));
}
/**
 * Semantic code search using RAG.
 *
 * @param query - Natural language query (e.g., "person validation with email rules")
 * @param filterLayer - Filter by architectural layer (Domain|Application|Infrastructure|API|Frontend|all)
 * @param filterType - Filter by code type (Entity|DTO|Service|Controller|Component|Hook|all)
 * @param limit - Maximum results to return (default: 10)
 */
export async function searchRag(query, filterLayer, filterType, limit = 10) {
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
    const must = [];
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
        const data = await response.json();
        const results = (data.result || []).map((r) => ({
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
    }
    catch (error) {
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
export async function getRagStatus() {
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
        const data = await response.json();
        const collection = data.result;
        return {
            healthy: true,
            qdrant_available: true,
            ollama_available: true,
            collection_name: COLLECTION_NAME,
            chunks_count: collection?.points_count || 0,
            vector_size: collection?.config?.params?.vectors?.size || VECTOR_SIZE
        };
    }
    catch (error) {
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
export async function getRagImpactAnalysis(filePath, changeDescription, includeTests = true) {
    // Extract entity/component name from file path
    const entityMatch = filePath.match(/\/([^/]+?)(?:\.cs|\.ts|\.tsx)$/);
    const entityName = entityMatch ? entityMatch[1].replace(/Controller|Service|Dto|Repository/, '') : '';
    // Build semantic query based on file path and change description
    let semanticQuery = entityName;
    if (changeDescription) {
        semanticQuery = `${entityName} ${changeDescription}`;
    }
    // Determine layer from file path
    let layer;
    if (filePath.includes('Koinon.Domain'))
        layer = 'Domain';
    else if (filePath.includes('Koinon.Application'))
        layer = 'Application';
    else if (filePath.includes('Koinon.Infrastructure'))
        layer = 'Infrastructure';
    else if (filePath.includes('Koinon.Api'))
        layer = 'API';
    else if (filePath.includes('src/web'))
        layer = 'Frontend';
    // Search for semantically related code
    const semanticResults = await searchRag(semanticQuery, undefined, undefined, 15);
    // Search for related tests if requested
    let testResults = [];
    if (includeTests && semanticResults.success) {
        const testQuery = `${entityName} test spec`;
        const testSearch = await searchRag(testQuery, undefined, undefined, 10);
        if (testSearch.success) {
            testResults = testSearch.results.filter(r => r.path.includes('.test.') ||
                r.path.includes('.spec.') ||
                r.path.includes('/tests/') ||
                r.path.includes('Tests.cs'));
        }
    }
    // Filter out the source file itself from results
    const filteredResults = semanticResults.results.filter(r => !r.path.endsWith(filePath.split('/').pop() || ''));
    return {
        success: semanticResults.success,
        file_path: filePath,
        change_description: changeDescription,
        semantic_matches: filteredResults,
        related_tests: testResults,
        warning: semanticResults.warning
    };
}
// ---------------------------------------------------------------------------
// Institutional lessons store (ADR 0005): team knowledge lives in an INDEXED
// location — a dedicated Qdrant collection on the inference server — never
// as a blob committed to the repo. Rules still belong in conventions.md/ADRs;
// this holds the experiential layer: gotchas, why-decisions, debugging
// lessons, searchable semantically by every agent and dev.
// ---------------------------------------------------------------------------
const LESSONS_COLLECTION = process.env.LESSONS_COLLECTION || 'koinon-lessons';
// Single-flight: concurrent lesson_add calls share one ensure operation so
// they can't race each other creating the collection (Qdrant 409s the losers).
let lessonsCollectionReady = null;
function ensureLessonsCollection() {
    if (!lessonsCollectionReady) {
        lessonsCollectionReady = (async () => {
            try {
                const existing = await fetch(`${QDRANT_URL}/collections/${LESSONS_COLLECTION}`);
                if (existing.ok)
                    return true;
                const created = await fetch(`${QDRANT_URL}/collections/${LESSONS_COLLECTION}`, {
                    method: 'PUT',
                    headers: { 'Content-Type': 'application/json' },
                    body: JSON.stringify({ vectors: { size: VECTOR_SIZE, distance: 'Cosine' } })
                });
                if (created.ok)
                    return true;
                // Lost a cross-process create race (409): existing collection is success.
                const recheck = await fetch(`${QDRANT_URL}/collections/${LESSONS_COLLECTION}`);
                return recheck.ok;
            }
            catch {
                return false;
            }
        })().then((ok) => {
            if (!ok)
                lessonsCollectionReady = null; // allow retry on next call
            return ok;
        });
    }
    return lessonsCollectionReady;
}
/**
 * Record an institutional lesson in the indexed store. Use for experiential
 * knowledge that cost real time (gotchas, root causes, why-decisions) — NOT
 * for rules, which belong in docs/reference/conventions.md via an ADR.
 */
export async function addLesson(text, topic) {
    if (!(await isQdrantAvailable())) {
        return { success: false, warning: `Qdrant unavailable at ${QDRANT_URL} - lesson NOT recorded` };
    }
    if (!(await ensureLessonsCollection())) {
        return { success: false, warning: `Could not create/access '${LESSONS_COLLECTION}' collection` };
    }
    const vector = await getEmbedding(text, 'search_document');
    if (!vector) {
        return { success: false, warning: 'Embeddings unavailable - lesson NOT recorded' };
    }
    const id = crypto.randomUUID();
    const res = await fetch(`${QDRANT_URL}/collections/${LESSONS_COLLECTION}/points?wait=true`, {
        method: 'PUT',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({
            points: [{
                    id,
                    vector,
                    payload: { text, topic, date: new Date().toISOString().slice(0, 10) }
                }]
        })
    });
    if (!res.ok) {
        return { success: false, warning: `Qdrant upsert failed: ${res.status} ${res.statusText}` };
    }
    return { success: true, id };
}
/**
 * Semantic search over the institutional lessons store. Run at session start
 * with your task keywords, and before debugging anything that smells like a
 * known trap.
 */
export async function searchLessons(query, limit = 5) {
    if (!(await isQdrantAvailable())) {
        return { success: false, results: [], warning: `Qdrant unavailable at ${QDRANT_URL}` };
    }
    const vector = await getEmbedding(query);
    if (!vector) {
        return { success: false, results: [], warning: 'Embeddings unavailable - fall back to docs/reference + ADRs' };
    }
    const res = await fetch(`${QDRANT_URL}/collections/${LESSONS_COLLECTION}/points/search`, {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ vector, limit, with_payload: true })
    });
    if (!res.ok) {
        // Collection may simply not exist yet — that's an empty store, not an error
        return { success: true, results: [], warning: `No lessons store yet ('${LESSONS_COLLECTION}' missing or unreadable)` };
    }
    const data = await res.json();
    return {
        success: true,
        results: (data.result || []).map((p) => ({
            id: String(p.id),
            text: String(p.payload?.text ?? ''),
            topic: String(p.payload?.topic ?? ''),
            date: String(p.payload?.date ?? ''),
            score: p.score
        }))
    };
}
//# sourceMappingURL=rag-client.js.map