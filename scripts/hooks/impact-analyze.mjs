#!/usr/bin/env node
// Mandatory impact analysis — the work an agent must do BEFORE modifying code.
// Delegates to the CANONICAL implementations only (conventions.md Process
// rule: never invent a second way to do something that already has a way):
//
//   structural impact  → `get_impact_analysis` on the koinon-dev MCP server,
//                        spoken over stdio to the committed build
//                        (tools/mcp-koinon-dev/dist/index.js) — the
//                        graph-baseline walk every agent uses
//   semantic impact    → getRagImpactAnalysis / searchLessons from the
//                        committed rag-client.js (Qdrant + embeddings on the
//                        inference server, ADR 0005) — the embedding index
//                        surfaces what the change is likely to touch
//   rules              → the matching sections of docs/reference/conventions.md,
//                        printed verbatim (single source of truth; no fork)
//
// Results land in .claude/impact-ledger.json; the PreToolUse guard
// (pre-tool-guard.mjs) refuses Edit/Write on code files and `git commit`
// until the ledger has a fresh entry — running this and reading its output
// is the ONLY way through.
//
//   node scripts/hooks/impact-analyze.mjs <file> [...]         # specific files
//   node scripts/hooks/impact-analyze.mjs --changed            # every uncommitted code file
//   node scripts/hooks/impact-analyze.mjs <file> --why "..."   # planned change, sharpens the semantic match
import { spawn } from 'node:child_process';
import { createHash } from 'node:crypto';
import { existsSync, readFileSync, writeFileSync } from 'node:fs';
import { basename, join, relative, resolve } from 'node:path';
import { pathToFileURL } from 'node:url';
import {
  COMMIT_TTL_MS,
  EDIT_TTL_MS,
  changedCodeFiles,
  ledgerPath,
  loadLedger,
  normalizeKey,
  repoRoot,
} from './impact-common.mjs';

const root = repoRoot();
process.chdir(root);

const args = process.argv.slice(2);
const whyIdx = args.indexOf('--why');
const why = whyIdx >= 0 ? args[whyIdx + 1] : undefined;
if (whyIdx >= 0) args.splice(whyIdx, 2);

let files;
if (args.includes('--changed')) {
  files = changedCodeFiles(root);
  if (files.length === 0) {
    console.log('No uncommitted code files to analyze.');
    process.exit(0);
  }
} else {
  files = args.filter((a) => !a.startsWith('--'));
  if (files.length === 0) {
    console.error('Usage: node scripts/hooks/impact-analyze.mjs <file...> | --changed  [--why "planned change"]');
    process.exit(1);
  }
}

// ---------------------------------------------------------------------------
// Canonical source 1: the koinon-dev MCP server, as a stdio child. One server
// per run; get_impact_analysis per file. The dist build is committed, so this
// works on a fresh clone with no network.
// ---------------------------------------------------------------------------
function startMcpServer() {
  const child = spawn(process.execPath, [join(root, 'tools', 'mcp-koinon-dev', 'dist', 'index.js')], {
    cwd: root,
    stdio: ['pipe', 'pipe', 'ignore'],
  });
  let buf = '';
  const pending = new Map();
  child.stdout.on('data', (d) => {
    buf += d;
    let nl;
    while ((nl = buf.indexOf('\n')) >= 0) {
      const line = buf.slice(0, nl).trim();
      buf = buf.slice(nl + 1);
      if (!line) continue;
      try {
        const msg = JSON.parse(line);
        if (msg.id != null && pending.has(msg.id)) {
          pending.get(msg.id)(msg);
          pending.delete(msg.id);
        }
      } catch { /* non-JSON stdout noise */ }
    }
  });
  let nextId = 1;
  const request = (method, params, timeoutMs = 15000) => {
    const id = nextId++;
    return new Promise((ok, fail) => {
      const timer = setTimeout(() => { pending.delete(id); fail(new Error(`${method} timed out`)); }, timeoutMs);
      pending.set(id, (msg) => {
        clearTimeout(timer);
        if (msg.error) fail(new Error(msg.error.message ?? 'MCP error'));
        else ok(msg.result);
      });
      child.stdin.write(JSON.stringify({ jsonrpc: '2.0', id, method, params }) + '\n');
    });
  };
  const notify = (method, params) => child.stdin.write(JSON.stringify({ jsonrpc: '2.0', method, params }) + '\n');
  return { child, request, notify };
}

let mcp = null;
try {
  mcp = startMcpServer();
  await mcp.request('initialize', {
    protocolVersion: '2024-11-05',
    capabilities: {},
    clientInfo: { name: 'impact-analyze', version: '2.0' },
  });
  mcp.notify('notifications/initialized', {});
} catch (e) {
  mcp?.child.kill();
  console.error(`✗ koinon-dev MCP server failed to start (${e?.message ?? e}).`);
  console.error('  Structural impact analysis is unavailable — fix the tooling; do not work around it.');
  process.exit(1);
}

async function graphImpact(filePath) {
  const res = await mcp.request('tools/call', { name: 'get_impact_analysis', arguments: { file_path: filePath } });
  return JSON.parse(res.content[0].text);
}

// ---------------------------------------------------------------------------
// Canonical source 2: the committed RAG client (semantic impact + lessons).
// QUALITY-OVER-AUTONOMY (memory: quality-over-autonomy): semantic retrieval is
// REQUIRED, not a nicety. rag-client's graceful retry absorbs a transient cold
// start; if the query STILL fails, that is a real infrastructure failure and
// development STOPS here — we never proceed graph-only or fall back to grep.
// The owner restores the inference server; the gate stays shut until then.
// (This inverts the old "degraded RAG is a warning, not a blocker" default —
//  that value is what let every session drift by proceeding half-blind.)
// ---------------------------------------------------------------------------
function hardStop(reason) {
  try { mcp.child.kill(); } catch { /* already gone */ }
  console.error(`\n✗ DEVELOPMENT HALTED — ${reason}.`);
  console.error('  RAG semantic retrieval is REQUIRED and did not complete after graceful retries.');
  console.error('  This is a real infrastructure failure, not something to work around. NO ledger entry');
  console.error('  was written, so the change stays blocked until it is fixed. The owner must restore the');
  console.error('  inference server (Qdrant 192.168.1.225:6333 / embeddings :4000 — see memory');
  console.error('  rag-infra-verified), then re-run this analysis. Do NOT bypass, do NOT fall back to grep.');
  process.exit(3);
}

let rag = null;
try {
  rag = await import(pathToFileURL(join(root, 'tools', 'mcp-koinon-dev', 'dist', 'rag-client.js')).href);
} catch (e) {
  hardStop(`the RAG client could not load (${e?.message ?? e})`);
}

// ---------------------------------------------------------------------------
// Canonical source 3: conventions.md sections, verbatim. No forked rule text.
// ---------------------------------------------------------------------------
const CANON_PATH = 'docs/reference/conventions.md';
const canonSections = (() => {
  const sections = new Map();
  let current = null;
  let lines = [];
  for (const line of readFileSync(join(root, CANON_PATH), 'utf8').split(/\r?\n/)) {
    const m = line.match(/^## (.+)$/);
    if (m) {
      if (current) sections.set(current, lines.join('\n').trimEnd());
      current = m[1];
      lines = [line];
    } else if (current) {
      lines.push(line);
    }
  }
  if (current) sections.set(current, lines.join('\n').trimEnd());
  return sections;
})();

function sectionsFor(rel, isNew) {
  const picks = [];
  if (/^src\/Koinon\.(Domain|Application|Infrastructure|Api)\//i.test(rel)) picks.push('Layering (dependency rule is absolute)');
  if (/^src\/Koinon\.Api\//i.test(rel)) picks.push('API surface');
  if (/^src\/Koinon\.(Domain|Infrastructure)\//i.test(rel)) picks.push('Data');
  if (/^src\/web\//i.test(rel)) picks.push('Frontend (src/web)');
  if (/docker-compose|\.csproj$|appsettings|Dockerfile/i.test(rel)) picks.push('Infrastructure / build');
  if (isNew && /^src\//i.test(rel)) picks.push('The feature slice (logic flow for a new feature)');
  if (picks.length === 0) picks.push('Process');
  return picks.filter((p) => canonSections.has(p));
}

const printedSections = new Set();
const ledger = loadLedger(ledgerPath(root));
const now = Date.now();
let analyzed = 0;
let failed = 0;

for (const arg of files) {
  const rel = relative(root, resolve(root, arg)).replaceAll('\\', '/');
  if (rel.startsWith('..')) {
    console.error(`✗ ${arg} is outside the repository — skipped.`);
    continue;
  }
  console.log(`\n=== Impact: ${rel} ===`);
  const exists = existsSync(rel);
  const content = exists ? readFileSync(rel, 'utf8') : '';
  if (!exists) console.log('  (new file — nothing references it yet; the feature-slice flow below applies)');

  // Structural impact from the graph baseline (canonical, always available).
  let structural;
  try {
    structural = await graphImpact(rel);
  } catch (e) {
    console.error(`  ✗ get_impact_analysis failed (${e?.message ?? e}) — no ledger entry written for this file.`);
    failed += 1;
    continue;
  }
  const affected = (structural.affected_files ?? []).filter((f) => normalizeKey(f.path ?? '') !== normalizeKey(rel));
  const summary = structural.impact_summary ?? {};
  if (affected.length === 0) {
    console.log('  Graph impact: no modeled dependents (file may sit outside the entity→DTO→service→controller→frontend graph).');
  } else {
    console.log(`  Graph impact (${affected.length} file(s), layers: ${(summary.layers_affected ?? []).join(', ') || 'n/a'}${summary.high_impact ? ', HIGH IMPACT' : ''}):`);
    for (const f of affected.slice(0, 30)) console.log(`    ${f.path}  [${f.layer ?? '?'}]`);
    if (affected.length > 30) console.log(`    ... and ${affected.length - 30} more`);
  }
  for (const wu of (structural.affected_work_units ?? []).slice(0, 15)) {
    console.log(`    ↳ ${wu.name}: ${wu.reason}`);
  }

  // Semantic impact + lessons — REQUIRED. A genuine failure halts development.
  const impact = await rag.getRagImpactAnalysis(rel, why, true);
  if (!impact?.success) {
    hardStop(`semantic search failed for ${rel} (${impact?.warning ?? 'unknown'})`);
  }
  const semanticCount = impact.semantic_matches.length;
  if (semanticCount > 0) {
    console.log(`  Semantic impact (${semanticCount}) — the embedding index surfaced these as related:`);
    for (const m of impact.semantic_matches) console.log(`    ${m.path}  [${m.layer}/${m.type}]  score ${m.score.toFixed(2)}`);
  } else {
    console.log('  Semantic impact: index returned no related code — unusual; verify the change is truly isolated.');
  }
  if (impact.related_tests.length > 0) {
    console.log(`  Related tests (${impact.related_tests.length}) — must stay green; extend them with your change:`);
    for (const t of impact.related_tests) console.log(`    ${t.path}`);
  }
  const lessons = await rag.searchLessons(`${basename(rel).replace(/\.[^.]+$/, '')} ${why ?? ''}`.trim(), 3);
  if (lessons.success && lessons.results.length > 0) {
    console.log('  Institutional lessons (koinon-lessons):');
    for (const l of lessons.results) console.log(`    [${l.topic || 'general'} ${l.date}] ${l.text.replace(/\s+/g, ' ').slice(0, 220)}`);
  }

  // The canon, verbatim — each section printed once per run.
  const picks = sectionsFor(rel, !exists);
  const fresh = picks.filter((p) => !printedSections.has(p));
  const repeated = picks.filter((p) => printedSections.has(p));
  for (const name of fresh) {
    printedSections.add(name);
    console.log(`  Canon (${CANON_PATH}):`);
    for (const line of canonSections.get(name).split('\n')) console.log(`    ${line}`);
  }
  if (repeated.length > 0) console.log(`  Canon: ${repeated.join('; ')} (printed above — it still applies here).`);

  ledger[normalizeKey(rel)] = {
    at: now,
    hash: exists ? createHash('sha1').update(content).digest('hex') : null,
    method: 'graph+rag',
    graph_files: affected.length,
    semantic_matches: semanticCount,
  };
  analyzed += 1;
}

mcp.child.kill();

for (const [k, v] of Object.entries(ledger)) {
  if (now - v.at > COMMIT_TTL_MS) delete ledger[k];
}
writeFileSync(ledgerPath(root), JSON.stringify(ledger, null, 2) + '\n');

console.log(`\n✓ Ledger updated (${analyzed} file(s)${failed ? `, ${failed} FAILED` : ''}): edits allowed for ${EDIT_TTL_MS / 60000} min, commits for 24 h.`);
console.log('  The gate is only satisfied on paper — actually READ what was surfaced above before changing anything.');
console.log('  Quality is the invariant (docs/claude/covenant.md): if RAG or the architect is down, HALT and summon the owner — never degrade or report done.');
process.exit(failed > 0 ? 1 : 0);
