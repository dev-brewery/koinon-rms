#!/usr/bin/env node
// Integration smoke test for the koinon-dev MCP server: boots the committed
// dist and calls get_impact_analysis against the committed graph baseline.
//
// WHY THIS EXISTS: get_impact_analysis shipped broken for weeks — it read
// baseline.api_functions (an object keyed by name) as an array and threw
// "(baseline.api_functions || []).filter is not a function" on every call. The
// mcp-dist-drift CI job proved dist === build(src), and graph-validation proved
// the baseline matched the code, yet NEITHER ever invoked the tool, so the
// crash was invisible. This test closes that gap: it fails if the tool errors
// or returns malformed paths. Run via `npm test` in this package, or in CI.
import { spawn } from 'node:child_process';
import { dirname, join, resolve } from 'node:path';
import { fileURLToPath } from 'node:url';

const HERE = dirname(fileURLToPath(import.meta.url));
const REPO_ROOT = resolve(HERE, '..', '..');
const SERVER = join(HERE, 'dist', 'index.js');

const child = spawn('node', [SERVER], {
  cwd: REPO_ROOT,
  stdio: ['pipe', 'pipe', 'ignore'],
  env: {
    ...process.env,
    // Keep smoke deterministic: standards_search must gracefully degrade when
    // the shared RAG stack is unavailable; CI must not depend on private LAN.
    QDRANT_URL: 'http://127.0.0.1:9',
    EMBEDDINGS_URL: 'http://127.0.0.1:9'
  }
});
let buf = '';
const pending = new Map();
child.stdout.on('data', (d) => {
  buf += d;
  let nl;
  while ((nl = buf.indexOf('\n')) >= 0) {
    const line = buf.slice(0, nl).trim(); buf = buf.slice(nl + 1);
    if (!line) continue;
    try { const m = JSON.parse(line); if (m.id != null && pending.has(m.id)) { pending.get(m.id)(m); pending.delete(m.id); } } catch {}
  }
});
let id = 1;
const req = (method, params) => new Promise((ok, fail) => {
  const myId = id++;
  const t = setTimeout(() => fail(new Error(method + ' timeout')), 15000);
  pending.set(myId, (m) => { clearTimeout(t); m.error ? fail(new Error(m.error.message)) : ok(m.result); });
  child.stdin.write(JSON.stringify({ jsonrpc: '2.0', id: myId, method, params }) + '\n');
});

let failures = 0;
const check = (name, cond, detail) => {
  console.log(`${cond ? 'PASS' : 'FAIL'}  ${name}${detail ? ' — ' + detail : ''}`);
  if (!cond) failures++;
};

try {
  await req('initialize', { protocolVersion: '2024-11-05', capabilities: {}, clientInfo: { name: 'smoke', version: '1' } });
  child.stdin.write(JSON.stringify({ jsonrpc: '2.0', method: 'notifications/initialized', params: {} }) + '\n');

  const listed = await req('tools/list', {});
  const toolNames = new Set((listed.tools || []).map((t) => t.name));
  check('standards_search tool is registered', toolNames.has('standards_search'));

  // One entity, one controller: exercises all three findFrontendConnections*
  // paths (Domain→DTO, DTO→api_function, Controller→api_function).
  for (const file of ['src/Koinon.Domain/Entities/Person.cs', 'src/Koinon.Api/Controllers/PeopleController.cs']) {
    const res = await req('tools/call', { name: 'get_impact_analysis', arguments: { file_path: file } });
    const text = res.content?.[0]?.text ?? '';
    check(`${file}: no error`, !res.isError, res.isError ? text : '');
    if (res.isError) continue;
    const data = JSON.parse(text);
    check(`${file}: affected_files non-empty`, data.affected_files.length > 0, `${data.affected_files.length} files`);
    const malformed = data.affected_files.filter((f) => /\$\{|undefined|\.ts\.ts/.test(f.path || ''));
    check(`${file}: no malformed paths`, malformed.length === 0, malformed.map((f) => f.path).join(', '));
    const dupes = data.affected_files.length - new Set(data.affected_files.map((f) => f.path)).size;
    check(`${file}: no duplicate paths`, dupes === 0, `${dupes} dupes`);
  }

  const standards = await req('tools/call', {
    name: 'standards_search',
    arguments: { query: 'check-in kiosk refinement decisions', scope: 'product_decisions', limit: 3 }
  });
  check('standards_search degrades without private RAG', !standards.isError);
  const standardsData = JSON.parse(standards.content?.[0]?.text ?? '{}');
  check('standards_search targets koinon-standards', standardsData.collection === 'koinon-standards');
  check('standards_search keeps product decision scope', standardsData.scope === 'product_decisions');
} catch (e) {
  check('server responded', false, e.message);
} finally {
  child.kill();
}

console.log(failures === 0 ? '\n✓ MCP smoke test passed' : `\n✗ ${failures} failure(s)`);
process.exit(failures === 0 ? 0 : 1);
