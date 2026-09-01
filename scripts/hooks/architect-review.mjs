#!/usr/bin/env node
// architect-review.mjs — the isolated architect-review subroutine (Phase 2.1).
//
// Input:  --files <a,b,...> --deduced "<diagnosis>" --proposed "<fix>" [--issue <N>]
// Output: a content-bound ruling written to .claude/approvals/<sha>.json and a
//         human-readable verdict on stdout.
// Exit:   0 APPROVED / APPROVED_WITH_CONDITIONS, 1 REJECTED,
//         2 bad invocation, 3 required infrastructure down (HARD STOP — quality
//         is the invariant; summon the owner, do not degrade).
//
// Brains (owner correction 2026-07-07): PRIMARY = Codex through the Claude Code
// bridge/plugin. The current executable shim uses `codex exec`; Plus/Pro/Team
// entitlements are not a Platform API key. FALLBACK = the local gateway
// (qwen36-dense), and a visible notice MUST be emitted when it activates.
// Both down → 3.
//
// The ruling is bound to sha256(file set + per-file content hash + diagnosis +
// proposal): edit anything and the sha no longer matches, so a stale or forged
// approval cannot unlock a different change.

import { createHash } from 'node:crypto';
import { readFileSync, writeFileSync, mkdirSync, existsSync } from 'node:fs';
import { execFileSync } from 'node:child_process';
import { dirname, join, resolve } from 'node:path';
import { fileURLToPath } from 'node:url';

// Repo root: ask git (works wherever this script file lives — scratchpad
// drafts included); fall back to script-relative (correct once installed at
// scripts/hooks/), overridable for tests.
function findRepoRoot() {
  if (process.env.KOINON_REPO_ROOT) return process.env.KOINON_REPO_ROOT;
  try {
    return execFileSync('git', ['rev-parse', '--show-toplevel'], { encoding: 'utf8' }).trim();
  } catch {
    return resolve(dirname(fileURLToPath(import.meta.url)), '..', '..');
  }
}
const REPO_ROOT = findRepoRoot();

// ---- env (process env wins; repo .env fills gaps — the agent cannot read
// .env by deny rule, but this script runs as its own process and can) --------
function loadEnv() {
  const env = { ...process.env };
  const envFile = join(REPO_ROOT, '.env');
  if (existsSync(envFile)) {
    for (const line of readFileSync(envFile, 'utf8').split(/\r?\n/)) {
      const m = line.match(/^\s*([A-Za-z_][A-Za-z0-9_]*)\s*=\s*(.*?)\s*$/);
      if (m && !(m[1] in env)) env[m[1]] = m[2].replace(/^["']|["']$/g, '');
    }
  }
  return env;
}
const ENV = loadEnv();
// Provenance (names only, never values): which .env this run actually read.
{
  const envFile = join(REPO_ROOT, '.env');
  const parsed = Object.keys(ENV).filter(k => k.startsWith('ARCHITECT') || k === 'RAG_HOST' || k === 'QDRANT_URL' || k === 'EMBEDDINGS_URL');
  console.error(`env: repo root ${REPO_ROOT}; .env ${existsSync(envFile) ? 'loaded' : 'absent'}; relevant keys: [${parsed.join(', ') || 'none'}]`);
}

const RAG_HOST = ENV.RAG_HOST || '192.168.1.225';
const QDRANT_URL = (ENV.QDRANT_URL || `http://${RAG_HOST}:6333`).replace(/\/$/, '');
const EMBEDDINGS_URL = (ENV.EMBEDDINGS_URL || `http://${RAG_HOST}:4000`).replace(/\/$/, '');
const TIMEOUT_MS = Number(ENV.ARCHITECT_TIMEOUT_MS || 600000);
const APPROVALS_DIR = ENV.APPROVALS_DIR || join(REPO_ROOT, '.claude', 'approvals');

// api: 'chat' (chat/completions), 'responses' (the Responses API — codex-family
// models reject chat/completions), or 'auto' (try chat, switch to responses when
// the error says the model needs it). Verified live 2026-07-07: /v1/responses
// exists at api.openai.com (401 without auth, not 404).
// Owner-corrected 2026-07-07: Plus/Pro plans grant no Platform API key, so the
// PRIMARY brain is the authenticated Codex CLI (provider 'codex'), invoked
// non-interactively with a read-only sandbox. 'http' keeps the OpenAI-compatible
// HTTP path for anyone with a real API key.
const PRIMARY = {
  label: 'primary',
  provider: (ENV.ARCHITECT_PROVIDER || 'codex').toLowerCase(),
  bin: ENV.ARCHITECT_CODEX_BIN || 'codex',
  codexModel: ENV.ARCHITECT_CODEX_MODEL || '',
  codexHome: ENV.ARCHITECT_CODEX_HOME || '',
  url: (ENV.ARCHITECT_URL || 'https://api.openai.com/v1').replace(/\/$/, ''),
  model: ENV.ARCHITECT_MODEL || '',
  key: ENV.ARCHITECT_API_KEY || '',
  api: (ENV.ARCHITECT_API || 'auto').toLowerCase(),
};
const FALLBACK = {
  label: 'fallback',
  provider: 'http',
  url: (ENV.ARCHITECT_FALLBACK_URL || `http://${RAG_HOST}:4000/v1`).replace(/\/$/, ''),
  model: ENV.ARCHITECT_FALLBACK_MODEL || 'qwen36-dense',
  key: ENV.ARCHITECT_FALLBACK_KEY || '',
  api: (ENV.ARCHITECT_FALLBACK_API || 'chat').toLowerCase(),
};

function die(code, msg) { console.error(msg); process.exit(code); }

function hardStop(detail) {
  die(3, [
    '════ ARCHITECT REVIEW: HARD STOP — REQUIRED INFRASTRUCTURE DOWN ════',
    detail,
    'No ruling was written. Development on this change HALTS here.',
    'Quality is the invariant (docs/claude/covenant.md): summon the owner to',
    'restore the architect brain / RAG — do not degrade, do not proceed.',
  ].join('\n'));
}

// ---- args -------------------------------------------------------------------
function parseArgs(argv) {
  const a = { files: [], deduced: '', proposed: '', mandates: '', issue: '', retrieveOnly: false };
  for (let i = 0; i < argv.length; i++) {
    const k = argv[i];
    if (k === '--files') a.files = argv[++i].split(',').map(s => s.trim()).filter(Boolean);
    else if (k === '--deduced') a.deduced = argv[++i];
    else if (k === '--proposed') a.proposed = argv[++i];
    else if (k === '--mandates') a.mandates = argv[++i];
    else if (k === '--issue') a.issue = argv[++i];
    else if (k === '--retrieve-only') a.retrieveOnly = true;
    else die(2, `Unknown argument: ${k}`);
  }
  if (!a.files.length || !a.deduced || !a.proposed) {
    die(2, 'Usage: architect-review.mjs --files a,b --deduced "<diagnosis>" --proposed "<fix>" [--issue N]');
  }
  return a;
}

export function retrievalQuery(deduced, proposed) {
  return `${deduced}\n${proposed}`;
}

export function buildUserContent({ fileHashes, deduced, proposed, mandates, criteria, standards, lessons }) {
  return [
    '## Files in this change', ...fileHashes.map(f => `- ${f.path} (${f.hash === 'NEW' ? 'new file' : 'existing'})`),
    '\n## Diagnosis (what the developer deduced)', deduced,
    '\n## Proposal (what the developer intends to do)', proposed,
    '\n## Acceptance criteria', criteria,
    ...(mandates ? ['\n## Dev-cycle mandates (compact canonical projection)', mandates] : []),
    '\n## Governing standards (retrieved by relevance — judge against THESE)',
    ...standards.map(h => `--- ${h.payload.path} § ${h.payload.section} (score ${h.score.toFixed(2)})\n${h.payload.content}`),
    ...(lessons.length ? ['\n## Institutional lessons', ...lessons.map(h => `- ${h.payload.text ?? h.payload.content ?? ''}`)] : []),
  ].join('\n');
}

// ---- retrieval (graceful & persistent: absorbs embed cold-start) -------------
async function post(url, body, headers = {}, timeoutMs = 30000) {
  const res = await fetch(url, {
    method: 'POST',
    headers: { 'content-type': 'application/json', ...headers },
    body: JSON.stringify(body),
    signal: AbortSignal.timeout(timeoutMs),
  });
  if (!res.ok) throw new Error(`${url} -> ${res.status} ${(await res.text()).slice(0, 200)}`);
  return res.json();
}

async function embed(text) {
  const delays = [0, 2000, 4000, 8000, 16000, 30000]; // patient: cold-load room
  let lastErr;
  for (const d of delays) {
    if (d) await new Promise(r => setTimeout(r, d));
    try {
      const j = await post(`${EMBEDDINGS_URL}/v1/embeddings`,
        { model: 'nomic-embed-text', input: [`search_query: ${text}`] }, {}, 45000);
      return j.data[0].embedding;
    } catch (e) { lastErr = e; console.error(`  (embed retry after: ${e.message.slice(0, 120)})`); }
  }
  hardStop(`Embedding gateway unreachable after ${delays.length} attempts: ${lastErr?.message}`);
}

async function qdrantSearch(collection, vector, limit, filter = null) {
  try {
    const body = { vector, limit, with_payload: true };
    if (filter) body.filter = filter;
    const j = await post(`${QDRANT_URL}/collections/${collection}/points/search`,
      body, {}, 30000);
    return j.result ?? [];
  } catch (e) {
    if (collection === 'koinon-standards') hardStop(`Qdrant unreachable / ${collection} missing: ${e.message}`);
    console.error(`  (non-fatal: ${collection} search failed: ${e.message.slice(0, 120)})`);
    return [];
  }
}

// Phase 3 — precise retrieval (drift #3 counter): query each standards
// category separately so one dominant doc cannot crowd the others out of a
// blended top-k. The architect gets the relevant ADR AND convention AND
// contract AND entity-mapping; the score floor keeps irrelevant filler out.
const STANDARD_CATEGORIES = ['adr', 'convention', 'api-contract', 'entity-mapping', 'reference'];
const PER_CATEGORY = 3;
const SCORE_FLOOR = 0.5;

async function retrieveStandards(vector) {
  const seen = new Set();
  const picked = [];
  for (const t of STANDARD_CATEGORIES) {
    const hits = await qdrantSearch('koinon-standards', vector, PER_CATEGORY,
      { must: [{ key: 'doc_type', match: { value: t } }] });
    for (const h of hits) {
      if (h.score < SCORE_FLOOR || seen.has(h.id)) continue;
      seen.add(h.id);
      picked.push(h);
    }
  }
  picked.sort((a, b) => b.score - a.score);
  return picked;
}

function acceptanceCriteria(issue) {
  if (!issue) return 'NONE PROVIDED — no linked issue was given for this change.';
  try {
    const out = execFileSync('gh', ['issue', 'view', String(issue), '--json', 'title,body'],
      { encoding: 'utf8', timeout: 30000 });
    const j = JSON.parse(out);
    return `Issue #${issue}: ${j.title}\n${j.body}`;
  } catch (e) {
    return `UNAVAILABLE — linked issue #${issue} could not be fetched (${String(e.message).slice(0, 120)}).`;
  }
}

// ---- the brains ---------------------------------------------------------------
const SYSTEM_PROMPT = `You are the chief architect for Koinon RMS. You are reviewing ONE proposed change in isolation. Judge CONFORMANCE only, against the standards excerpts provided: does the diagnosis hold, and does the proposal violate any ADR, convention, API contract, entity mapping, or the traced existing pattern? Does it meet the acceptance criteria if provided? Cite the specific standard (path + section) for every finding. Approve the majority of conforming work; reject or condition ONLY with a specific cited collision. Respond with ONLY a JSON object, no prose, no markdown fence: {"ruling":"APPROVED"|"APPROVED_WITH_CONDITIONS"|"REJECTED","reasons":[{"standard":"<path § section>","finding":"<specific>"}],"conditions":["<only if ruling is APPROVED_WITH_CONDITIONS>"]}`;

function extractRuling(text) {
  // Local reasoning models wrap the JSON in <think> blocks and prose; scan
  // every balanced top-level {...} candidate instead of first-{ to last-}.
  let s = String(text).replace(/<think>[^]*?<\/think>/gi, '');
  const candidates = [];
  for (let i = 0; i < s.length; i++) {
    if (s[i] !== '{') continue;
    let depth = 0, inStr = false, esc = false;
    for (let k = i; k < s.length; k++) {
      const ch = s[k];
      if (esc) { esc = false; continue; }
      if (ch === '\\') { esc = true; continue; }
      if (ch === '"') inStr = !inStr;
      if (inStr) continue;
      if (ch === '{') depth++;
      if (ch === '}') { depth--; if (depth === 0) { candidates.push(s.slice(i, k + 1)); i = k; break; } }
    }
  }
  // Scan candidates LAST-first: codex's stdin mode echoes the prompt before
  // the model's answer, and the ruling is always the final JSON emitted.
  for (const c of candidates.reverse()) {
    try {
      const j = JSON.parse(c);
      if (['APPROVED', 'APPROVED_WITH_CONDITIONS', 'REJECTED'].includes(j.ruling)) {
        j.reasons ??= []; j.conditions ??= [];
        return j;
      }
    } catch { /* try next candidate */ }
  }
  console.error(`  (unparseable brain response, first 300 chars: ${s.slice(0, 300).replace(/\s+/g, ' ')})`);
  throw new Error('no JSON object with a valid ruling in brain response');
}

async function callChat(brain, headers, userContent) {
  const j = await post(`${brain.url}/chat/completions`, {
    model: brain.model,
    temperature: 0,
    messages: [
      { role: 'system', content: SYSTEM_PROMPT },
      { role: 'user', content: userContent },
    ],
  }, headers, TIMEOUT_MS);
  return j.choices[0].message.content;
}

async function callResponses(brain, headers, userContent) {
  const j = await post(`${brain.url}/responses`, {
    model: brain.model,
    instructions: SYSTEM_PROMPT,
    input: userContent,
  }, headers, TIMEOUT_MS);
  if (typeof j.output_text === 'string' && j.output_text) return j.output_text;
  const parts = [];
  for (const item of j.output ?? []) {
    for (const c of item.content ?? []) {
      if (c.type === 'output_text' && c.text) parts.push(c.text);
    }
  }
  if (!parts.length) throw new Error('Responses API returned no output_text');
  return parts.join('\n');
}

// JSON Schema for --output-schema: constrains codex's final message to a
// valid ruling (structured output — no prose scraping needed).
const RULING_SCHEMA = {
  type: 'object',
  properties: {
    ruling: { type: 'string', enum: ['APPROVED', 'APPROVED_WITH_CONDITIONS', 'REJECTED'] },
    reasons: {
      type: 'array',
      items: {
        type: 'object',
        properties: { standard: { type: 'string' }, finding: { type: 'string' } },
        required: ['standard', 'finding'], additionalProperties: false,
      },
    },
    conditions: { type: 'array', items: { type: 'string' } },
  },
  required: ['ruling', 'reasons', 'conditions'], additionalProperties: false,
};

function callCodex(brain, promptText) {
  // npm ships codex as a .cmd shim on Windows; Node (CVE-2024-27980) requires
  // shell:true to launch it. Safe here because argv is entirely static — the
  // prompt travels via STDIN and the model name is validated below.
  if (brain.codexModel && !/^[A-Za-z0-9._:-]+$/.test(brain.codexModel)) {
    throw new Error(`invalid ARCHITECT_CODEX_MODEL: ${brain.codexModel}`);
  }
  const useShell = process.platform === 'win32';
  const schemaPath = join(APPROVALS_DIR, '..', 'ruling-schema.json');
  writeFileSync(schemaPath, JSON.stringify(RULING_SCHEMA));
  // Quote the path only when a shell will re-parse the argv (win32 .cmd shim).
  const args = ['exec', '--sandbox', 'read-only', '--skip-git-repo-check', '--output-schema', useShell ? `"${schemaPath}"` : schemaPath];
  if (brain.codexModel) args.push('-m', brain.codexModel);
  const env = { ...process.env };
  if (brain.codexHome) env.CODEX_HOME = brain.codexHome;
  const out = execFileSync(brain.bin, args, {
    cwd: REPO_ROOT, encoding: 'utf8', input: promptText,
    shell: useShell,
    timeout: TIMEOUT_MS, maxBuffer: 32 * 1024 * 1024, env, windowsHide: true,
  });
  return out;
}

// The error chat/completions returns when a model is Responses-only.
const NEEDS_RESPONSES = /responses api|not a chat model|only supported in v1\/responses|unsupported.*chat/i;

async function callBrain(brain, userContent) {
  const t0c = Date.now();
  if (brain.provider === 'codex') {
    const text = callCodex(brain, `${SYSTEM_PROMPT}

${userContent}`);
    const elapsedMs = Date.now() - t0c;
    console.error(`  (${brain.label} brain responded in ${(elapsedMs / 1000).toFixed(1)}s via codex${brain.codexModel ? ':' + brain.codexModel : ''})`);
    return { ...extractRuling(text), elapsedMs, apiUsed: 'codex' };
  }
  if (!brain.model) throw new Error(`${brain.label} not configured (missing model)`);
  if (brain.label === 'primary' && !brain.key) throw new Error('primary not configured (missing ARCHITECT_API_KEY)');
  const headers = brain.key ? { authorization: `Bearer ${brain.key}` } : {};
  const t0 = Date.now();
  let text, apiUsed = brain.api;
  if (brain.api === 'responses') {
    text = await callResponses(brain, headers, userContent);
  } else if (brain.api === 'chat') {
    text = await callChat(brain, headers, userContent);
  } else { // auto
    try {
      text = await callChat(brain, headers, userContent);
      apiUsed = 'chat';
    } catch (e) {
      if (!NEEDS_RESPONSES.test(e.message)) throw e;
      console.error(`  (${brain.label}: model is Responses-only per API error — switching to /v1/responses)`);
      text = await callResponses(brain, headers, userContent);
      apiUsed = 'responses';
    }
  }
  const elapsedMs = Date.now() - t0;
  console.error(`  (${brain.label} brain responded in ${(elapsedMs / 1000).toFixed(1)}s via ${apiUsed})`);
  return { ...extractRuling(text), elapsedMs, apiUsed };
}

// ---- content binding ------------------------------------------------------------
function sha256(buf) { return createHash('sha256').update(buf).digest('hex'); }

function bindChange(files, deduced, proposed) {
  const fileHashes = files.slice().sort().map(f => {
    const p = resolve(REPO_ROOT, f);
    return { path: f.replaceAll('\\', '/'), hash: existsSync(p) ? sha256(readFileSync(p)) : 'NEW' };
  });
  const sha = sha256(JSON.stringify({ files: fileHashes, deduced, proposed }));
  return { sha, fileHashes };
}

// ---- main -----------------------------------------------------------------------
async function main() {
  const args = parseArgs(process.argv.slice(2));

  console.error('Retrieving standards for this change (koinon-standards)...');
  const vector = await embed(retrievalQuery(args.deduced, args.proposed));
  const standards = await retrieveStandards(vector);
  const lessons = await qdrantSearch('koinon-lessons', vector, 3);
  if (!standards.length) hardStop('koinon-standards returned zero chunks — the index is empty or the score floor excluded everything; run tools/rag/index-standards.py.');

  if (args.retrieveOnly) {
    console.log(JSON.stringify({
      standards: standards.map(h => ({ path: h.payload.path, doc_type: h.payload.doc_type, section: h.payload.section, score: h.score })),
      lessons: lessons.map(h => ({ text: String(h.payload.text ?? h.payload.content ?? '').slice(0, 120) })),
    }, null, 2));
    process.exit(0);
  }

  const criteria = acceptanceCriteria(args.issue);
  const { sha, fileHashes } = bindChange(args.files, args.deduced, args.proposed);

  const userContent = buildUserContent({
    fileHashes, deduced: args.deduced, proposed: args.proposed,
    mandates: args.mandates, criteria, standards, lessons,
  });

  let ruling, brainUsed = PRIMARY, fallbackActivated = false, primaryError = '';
  try {
    const primaryName = PRIMARY.provider === 'codex'
      ? `codex${PRIMARY.codexModel ? `:${PRIMARY.codexModel}` : ''}`
      : (PRIMARY.model || 'NOT CONFIGURED');
    console.error(`Consulting architect brain (${PRIMARY.label}: ${primaryName})...`);
    ruling = await callBrain(PRIMARY, userContent);
  } catch (e) {
    primaryError = e.message;
    console.error('');
    console.error('⚠⚠⚠ ARCHITECT FALLBACK ACTIVATED ⚠⚠⚠');
    console.error(`⚠ Primary brain unavailable: ${primaryError.slice(0, 200)}`);
    console.error(`⚠ Using local ${FALLBACK.model} at ${FALLBACK.url} — responses may take minutes.`);
    console.error('');
    try {
      ruling = await callBrain(FALLBACK, userContent);
      brainUsed = FALLBACK; fallbackActivated = true;
    } catch (e2) {
      hardStop(`Primary brain failed (${primaryError.slice(0, 150)}) AND fallback failed (${e2.message.slice(0, 150)}).`);
    }
  }

  mkdirSync(APPROVALS_DIR, { recursive: true });
  const record = {
    sha, ruling: ruling.ruling, reasons: ruling.reasons, conditions: ruling.conditions,
    files: fileHashes, deduced: args.deduced, proposed: args.proposed,
    issue: args.issue || null,
    brain: { url: brainUsed.url, model: brainUsed.model, api: ruling.apiUsed, fallbackActivated, primaryError: primaryError || null, elapsedMs: ruling.elapsedMs },
    at: new Date().toISOString(),
  };
  writeFileSync(join(APPROVALS_DIR, `${sha}.json`), JSON.stringify(record, null, 2));

  console.log(`\n════ ARCHITECT RULING: ${ruling.ruling} ════`);
  for (const r of ruling.reasons) console.log(`  • [${r.standard}] ${r.finding}`);
  for (const c of ruling.conditions) console.log(`  CONDITION: ${c}`);
  if (fallbackActivated) console.log(`  (ruled by FALLBACK brain ${FALLBACK.model} — primary was down)`);
  console.log(`  Binding: ${sha}`);
  console.log(`  Record:  ${join(APPROVALS_DIR, sha + '.json')}`);
  process.exit(ruling.ruling === 'REJECTED' ? 1 : 0);
}

if (process.argv[1] && resolve(process.argv[1]) === fileURLToPath(import.meta.url)) {
  main().catch(e => hardStop(`Unexpected failure: ${e.stack || e.message}`));
}
