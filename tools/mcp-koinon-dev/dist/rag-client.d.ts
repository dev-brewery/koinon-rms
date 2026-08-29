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
export type StandardsScope = 'all' | 'rules' | 'adrs' | 'product_decisions';
export interface StandardsSearchResult {
    path: string;
    doc_type: string;
    section: string;
    score: number;
    snippet: string;
    decision_id?: string;
    decision_type?: string;
    status?: string;
    applies_to?: string[];
    date?: string;
}
export interface StandardsSearchResponse {
    success: boolean;
    collection: string;
    scope: StandardsScope;
    query: string;
    results: StandardsSearchResult[];
    warning?: string;
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
 * Semantic code search using RAG.
 *
 * @param query - Natural language query (e.g., "person validation with email rules")
 * @param filterLayer - Filter by architectural layer (Domain|Application|Infrastructure|API|Frontend|all)
 * @param filterType - Filter by code type (Entity|DTO|Service|Controller|Component|Hook|all)
 * @param limit - Maximum results to return (default: 10)
 */
export declare function searchRag(query: string, filterLayer?: string, filterType?: string, limit?: number): Promise<RagSearchResponse>;
/**
 * Semantic search over the standards corpus, including product/refinement
 * decisions indexed as doc_type=product-decision inside koinon-standards.
 */
export declare function searchStandards(query: string, scope?: StandardsScope, limit?: number): Promise<StandardsSearchResponse>;
/**
 * Get RAG index status and health information.
 */
export declare function getRagStatus(): Promise<RagIndexStatus>;
/**
 * RAG-enhanced impact analysis.
 * Finds semantically related code and tests for a given file.
 *
 * @param filePath - File path to analyze
 * @param changeDescription - Optional description of planned changes for better matching
 * @param includeTests - Whether to search for related tests (default: true)
 */
export declare function getRagImpactAnalysis(filePath: string, changeDescription?: string, includeTests?: boolean): Promise<RagImpactResult>;
export interface LessonResult {
    id: string;
    text: string;
    topic: string;
    date: string;
    score?: number;
}
export interface LessonAddResponse {
    success: boolean;
    id?: string;
    warning?: string;
}
export interface LessonSearchResponse {
    success: boolean;
    results: LessonResult[];
    warning?: string;
}
/**
 * Record an institutional lesson in the indexed store. Use for experiential
 * knowledge that cost real time (gotchas, root causes, why-decisions) — NOT
 * for rules, which belong in docs/reference/conventions.md via an ADR.
 */
export declare function addLesson(text: string, topic: string): Promise<LessonAddResponse>;
/**
 * Semantic search over the institutional lessons store. Run at session start
 * with your task keywords, and before debugging anything that smells like a
 * known trap.
 */
export declare function searchLessons(query: string, limit?: number): Promise<LessonSearchResponse>;
//# sourceMappingURL=rag-client.d.ts.map