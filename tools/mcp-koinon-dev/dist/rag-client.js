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
const STANDARDS_COLLECTION = process.env.STANDARDS_COLLECTION || 'koinon-standards';
const RAG_HOST = process.env.RAG_HOST || '192.168.1.225';
// Embeddings are served by the model gateway on :4000 (OpenAI-compatible
// /v1/embeddings; the Ollama-protocol attempt below is a harmless fast miss).
const EMBEDDINGS_BASE_URL = (process.env.EMBEDDINGS_URL || process.env.OLLAMA_URL || `http://${RAG_HOST}:4000`).replace(/\/+$/, '');
const OLLAMA_MODEL = 'nomic-embed-text';
const VECTOR_SIZE = 768;
const QDRANT_URL = process.env.QDRANT_URL || `http://${RAG_HOST}:6333`;
const REQUEST_TIMEOUT = 5000; // 5 seconds — Qdrant health/search, always fast
// Embedding calls tolerate the gateway's lazy cold-load: a warm call is ~100ms,
// but the first-after-idle call blocks (or 400s) while nomic-embed-text loads.
// Graceful persistence — a patient timeout across several backed-off attempts —
// absorbs that, instead of failing at 5s and making semantic search look "down"
// (the recurring misdiagnosis that made every session fall back to grep).
const EMBED_TIMEOUT = Number(process.env.EMBED_TIMEOUT) || 20000;
const EMBED_MAX_ATTEMPTS = Number(process.env.EMBED_ATTEMPTS) || 4;
const EMBED_BACKOFF_MS = Number(process.env.EMBED_BACKOFF_MS) || 1500;
/**
 * Get embedding vector from the inference server. nomic prefixes:
 * 'search_query:' for queries, 'search_document:' when storing documents
 * (matching Python utils). Tries the Ollama protocol (/api/embed) first,
 * then OpenAI-compatible (/v1/embeddings). Index and queries must share
 * the model: nomic-embed-text, 768-dim.
 */
async function getEmbedding(text, prefix = 'search_query') {
    const input = `${prefix}: ${text}`;
    const post = async (url, body, timeoutMs) => {
        const controller = new AbortController();
        const timeoutId = setTimeout(() => controller.abort(), timeoutMs);
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
    // One protocol-flexible attempt: Ollama /api/embed (some deployments) then
    // OpenAI-compatible /v1/embeddings (the koinon gateway). Resolves to the
    // vector, or a status string describing why this attempt missed.
    const attempt = async () => {
        const ollamaRes = await post(`${EMBEDDINGS_BASE_URL}/api/embed`, { model: OLLAMA_MODEL, input: [input] }, EMBED_TIMEOUT);
        if (ollamaRes?.ok) {
            const data = await ollamaRes.json();
            if (data.embeddings?.[0])
                return data.embeddings[0];
        }
        const openaiRes = await post(`${EMBEDDINGS_BASE_URL}/v1/embeddings`, { model: OLLAMA_MODEL, input: [input] }, EMBED_TIMEOUT);
        if (openaiRes?.ok) {
            const data = await openaiRes.json();
            if (data.data?.[0]?.embedding)
                return data.data[0].embedding;
        }
        return `/api/embed -> ${ollamaRes?.status ?? 'unreachable'}, /v1/embeddings -> ${openaiRes?.status ?? 'unreachable'}`;
    };
    // Graceful persistence: a cold gateway 400s or stalls while loading the model,
    // then serves in ~100ms. Retry with growing backoff so the first-after-idle
    // call recovers rather than making the whole semantic layer report "down".
    let lastStatus = 'unreachable';
    for (let i = 1; i <= EMBED_MAX_ATTEMPTS; i++) {
        const result = await attempt();
        if (Array.isArray(result))
            return result;
        lastStatus = result;
        if (i < EMBED_MAX_ATTEMPTS) {
            await new Promise((r) => setTimeout(r, EMBED_BACKOFF_MS * i));
        }
    }
    console.error(`Embeddings unavailable after ${EMBED_MAX_ATTEMPTS} patient attempts at ${EMBEDDINGS_BASE_URL} ` +
        `(last: ${lastStatus}). The gateway did not warm up — a REAL failure, not a cold start; ` +
        `semantic search must NOT be silently skipped.`);
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
 * Semantic search over the standards corpus, including product/refinement
 * decisions indexed as doc_type=product-decision inside koinon-standards.
 */
export async function searchStandards(query, scope = 'all', limit = 10) {
    if (!await isQdrantAvailable()) {
        return { success: false, collection: STANDARDS_COLLECTION, scope, query, results: [], warning: `Qdrant unavailable at ${QDRANT_URL}` };
    }
    const embedding = await getEmbedding(query);
    if (!embedding) {
        return { success: false, collection: STANDARDS_COLLECTION, scope, query, results: [], warning: 'Embeddings unavailable - fall back to docs/reference, docs/adr, and docs/product/decisions' };
    }
    const docTypesByScope = {
        all: undefined,
        rules: ['convention', 'api-contract', 'entity-mapping', 'reference'],
        adrs: ['adr'],
        product_decisions: ['product-decision']
    };
    const docTypes = docTypesByScope[scope];
    const filter = docTypes ? { must: [{ key: 'doc_type', match: { any: docTypes } }] } : undefined;
    try {
        const controller = new AbortController();
        const timeoutId = setTimeout(() => controller.abort(), REQUEST_TIMEOUT);
        const response = await fetch(`${QDRANT_URL}/collections/${STANDARDS_COLLECTION}/points/search`, {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify({ vector: embedding, filter, limit, with_payload: true }),
            signal: controller.signal
        });
        clearTimeout(timeoutId);
        if (!response.ok) {
            return {
                success: false,
                collection: STANDARDS_COLLECTION,
                scope,
                query,
                results: [],
                warning: `Standards search failed: ${response.status} ${response.statusText}. Run npm run rag:index:standards if the collection is missing.`
            };
        }
        const data = await response.json();
        const results = (data.result || []).map((r) => {
            const payload = r.payload || {};
            const applies = payload.applies_to;
            return {
                path: String(payload.path || 'unknown'),
                doc_type: String(payload.doc_type || 'unknown'),
                section: String(payload.section || ''),
                score: r.score || 0,
                snippet: String(payload.content || '').substring(0, 500),
                decision_id: payload.decision_id ? String(payload.decision_id) : undefined,
                decision_type: payload.decision_type ? String(payload.decision_type) : undefined,
                status: payload.status ? String(payload.status) : undefined,
                applies_to: Array.isArray(applies) ? applies.map(String) : undefined,
                date: payload.date ? String(payload.date) : undefined
            };
        });
        return { success: true, collection: STANDARDS_COLLECTION, scope, query, results };
    }
    catch (error) {
        return {
            success: false,
            collection: STANDARDS_COLLECTION,
            scope,
            query,
            results: [],
            warning: `Standards search error: ${error instanceof Error ? error.message : 'unknown error'}`
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