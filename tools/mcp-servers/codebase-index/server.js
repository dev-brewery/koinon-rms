#!/usr/bin/env node
/**
 * koinon-index — zero-dependency MCP server that keeps a structural index of
 * the Koinon RMS codebase so agents can look up conventions and wiring
 * (route → page → hook → api service → endpoint → controller → app service →
 * entity) instead of grepping and guessing.
 *
 * Requires Node >= 18. No npm install needed: implements the MCP stdio
 * transport (newline-delimited JSON-RPC 2.0) by hand.
 *
 * Registered in .mcp.json. The project root is resolved relative to this
 * file (../../.. from tools/mcp-servers/codebase-index), so cwd doesn't matter.
 */

'use strict';

const fs = require('fs');
const path = require('path');
const readline = require('readline');

const ROOT = path.resolve(__dirname, '..', '..', '..');
const CONVENTIONS_PATH = path.join(ROOT, 'docs', 'reference', 'conventions.md');

// ---------------------------------------------------------------------------
// File scanning
// ---------------------------------------------------------------------------

const SCAN_SPECS = [
  { dir: 'src/Koinon.Api/Controllers', ext: '.cs', kind: 'controller' },
  { dir: 'src/Koinon.Application/Services', ext: '.cs', kind: 'appService' },
  { dir: 'src/Koinon.Application/Interfaces', ext: '.cs', kind: 'appInterface' },
  { dir: 'src/Koinon.Application/DTOs', ext: '.cs', kind: 'dto', recursive: true },
  { dir: 'src/Koinon.Application/Validators', ext: '.cs', kind: 'validator', recursive: true },
  { dir: 'src/Koinon.Domain/Entities', ext: '.cs', kind: 'entity' },
  { dir: 'src/web/src/services/api', ext: '.ts', kind: 'apiService' },
  { dir: 'src/web/src/hooks', ext: '.ts', kind: 'hook', extAlt: '.tsx' },
  { dir: 'src/web/src/pages', ext: '.tsx', kind: 'page', recursive: true },
  { dir: 'src/web/src/features', ext: '.tsx', kind: 'page', recursive: true },
];

function listFiles(dirAbs, exts, recursive) {
  const out = [];
  let entries;
  try {
    entries = fs.readdirSync(dirAbs, { withFileTypes: true });
  } catch {
    return out;
  }
  for (const e of entries) {
    const p = path.join(dirAbs, e.name);
    if (e.isDirectory()) {
      if (recursive && e.name !== '__tests__' && e.name !== 'node_modules') {
        out.push(...listFiles(p, exts, recursive));
      }
    } else if (exts.some((x) => e.name.endsWith(x)) && !e.name.includes('.test.')) {
      out.push(p);
    }
  }
  return out;
}

function rel(p) {
  return path.relative(ROOT, p).replace(/\\/g, '/');
}

// ---------------------------------------------------------------------------
// Parsers (regex-based, calibrated against this codebase's patterns)
// ---------------------------------------------------------------------------

function parseController(file, text) {
  const lines = text.split('\n');
  const endpoints = [];
  const className = (text.match(/public\s+(?:sealed\s+)?class\s+(\w+Controller)/) || [])[1];
  if (!className) return { endpoints, services: [] };
  const resource = className.replace(/Controller$/, '');
  const routeAttr = (text.match(/\[Route\("([^"]+)"\)\]/) || [])[1] || '';
  const baseRoute = routeAttr.replace('[controller]', resource);
  const classDeclIndex = text.search(/public\s+(?:sealed\s+)?class\s+\w+Controller/);
  const classAuthorize = /\[Authorize[^\]]*\]/.test(text.slice(0, classDeclIndex));

  // Injected services from primary constructor or ctor params
  const services = [];
  const ctorMatch = text.match(new RegExp(className + '\\s*\\(([^)]*)\\)', 's'));
  if (ctorMatch) {
    for (const m of ctorMatch[1].matchAll(/I(\w+(?:Service|Provider|Context|Validator[\w<>]*))\s+(\w+)/g)) {
      services.push('I' + m[1]);
    }
  }

  // Walk lines; collect attributes until an action signature appears
  let pending = [];
  for (let i = 0; i < lines.length; i++) {
    const line = lines[i].trim();
    const attr = line.match(/^\[(HttpGet|HttpPost|HttpPut|HttpDelete|HttpPatch)(?:\("([^"]*)"\))?\]/);
    if (attr) {
      pending.push({ verb: attr[1].replace('Http', '').toUpperCase(), sub: attr[2] || '', line: i + 1 });
      continue;
    }
    if (/^\[(AllowAnonymous)\]/.test(line) && pending.length) pending[pending.length - 1].anon = true;
    const action = line.match(/public\s+(?:async\s+)?Task(?:<[^>]+>)?\s+(\w+)\s*\(/);
    if (action && pending.length) {
      for (const p of pending) {
        const route = ('/' + baseRoute + (p.sub ? '/' + p.sub : '')).replace(/\/+/g, '/');
        endpoints.push({
          method: p.verb,
          route,
          controller: className,
          action: action[1],
          file: rel(file),
          line: p.line,
          auth: p.anon ? 'anonymous' : classAuthorize ? 'authorize' : 'inherit',
        });
      }
      pending = [];
    }
  }
  return { endpoints, services, className, file: rel(file) };
}

function parseCsTypes(file, text, kind) {
  const out = [];
  const re = /(?:public|internal)\s+(?:sealed\s+|abstract\s+|partial\s+|static\s+)*(class|record|interface|enum)\s+(\w+)/g;
  const lines = text.split('\n');
  for (const m of text.matchAll(re)) {
    const line = text.slice(0, m.index).split('\n').length;
    // capture ": BaseType" inheritance on same line
    const declLine = lines[line - 1] || '';
    const inherits = (declLine.match(/:\s*([\w<>,\s.]+?)\s*(?:\{|$|\()/) || [])[1];
    out.push({ kind, name: m[2], declType: m[1], file: rel(file), line, inherits: inherits ? inherits.trim() : undefined });
  }
  return out;
}

function parseApiService(file, text) {
  const out = [];
  const fnRe = /export\s+(?:async\s+)?function\s+(\w+)/g;
  const fns = [];
  for (const m of text.matchAll(fnRe)) {
    fns.push({ name: m[1], index: m.index, line: text.slice(0, m.index).split('\n').length });
  }
  for (let i = 0; i < fns.length; i++) {
    const bodyEnd = i + 1 < fns.length ? fns[i + 1].index : text.length;
    const body = text.slice(fns[i].index, bodyEnd);
    const calls = [];
    for (const c of body.matchAll(/\b(get|post|put|del|patch|apiClient)(?:<[^;]*?>)?\(\s*[`'"]([^`'"]+)[`'"]/g)) {
      calls.push({ verb: c[1] === 'del' ? 'delete' : c[1] === 'apiClient' ? 'request' : c[1], endpoint: c[2] });
    }
    out.push({ kind: 'apiFunction', name: fns[i].name, file: rel(file), line: fns[i].line, calls });
  }
  return out;
}

function parseHooks(file, text) {
  const out = [];
  // imported names from services/api
  const imported = new Set();
  for (const m of text.matchAll(/import\s*\{([^}]+)\}\s*from\s*['"][^'"]*services\/api[^'"]*['"]/g)) {
    m[1].split(',').forEach((n) => imported.add(n.trim().split(/\s+as\s+/)[0]));
  }
  // namespace imports: import * as xApi from ... / import { xApi } from services/api index
  for (const m of text.matchAll(/import\s+\*\s+as\s+(\w+)\s+from\s*['"][^'"]*services\/api[^'"]*['"]/g)) {
    imported.add(m[1]);
  }
  for (const m of text.matchAll(/export\s+function\s+(use\w+)/g)) {
    const line = text.slice(0, m.index).split('\n').length;
    // api functions referenced anywhere in file (approximation)
    const uses = [...imported].filter((n) => new RegExp('\\b' + n + '\\b').test(text.slice(m.index)));
    out.push({ kind: 'hook', name: m[1], file: rel(file), line, uses });
  }
  return out;
}

function parsePages(file, text) {
  const out = [];
  for (const m of text.matchAll(/export\s+(?:default\s+)?(?:function|const)\s+(\w+)/g)) {
    const line = text.slice(0, m.index).split('\n').length;
    const hooks = [...new Set([...text.matchAll(/\b(use[A-Z]\w+)\s*\(/g)].map((h) => h[1]))].filter(
      (h) => !['useState', 'useEffect', 'useMemo', 'useCallback', 'useRef', 'useContext', 'useNavigate', 'useLocation', 'useParams', 'useSearchParams'].includes(h)
    );
    out.push({ kind: 'page', name: m[1], file: rel(file), line, hooks });
  }
  return out;
}

function parseRoutes(text) {
  const out = [];
  // <Route path="x" element={<Comp .../>}> — tolerate ProtectedRoute wrappers
  for (const m of text.matchAll(/<Route\s+(?:index\s+)?path="([^"]+)"[\s\S]{0,400}?element=\{([\s\S]{0,300}?)\}\s*\/?\s*>/g)) {
    const comps = [...m[2].matchAll(/<(\w+)[\s/>]/g)]
      .map((c) => c[1])
      .filter((c) => !['ProtectedRoute', 'Navigate', 'Suspense'].includes(c));
    out.push({ kind: 'route', path: m[1], components: comps });
  }
  return out;
}

// ---------------------------------------------------------------------------
// Index build + staleness
// ---------------------------------------------------------------------------

let INDEX = null;
let INDEX_FILES = new Map(); // abs path -> mtimeMs

function buildIndex() {
  const idx = {
    builtAt: new Date().toISOString(),
    endpoints: [],
    controllers: [],
    csTypes: [],
    apiFunctions: [],
    hooks: [],
    pages: [],
    routes: [],
  };
  INDEX_FILES = new Map();

  for (const spec of SCAN_SPECS) {
    const dirAbs = path.join(ROOT, spec.dir);
    const exts = [spec.ext].concat(spec.extAlt ? [spec.extAlt] : []);
    for (const f of listFiles(dirAbs, exts, spec.recursive)) {
      let text;
      try {
        text = fs.readFileSync(f, 'utf8');
        INDEX_FILES.set(f, fs.statSync(f).mtimeMs);
      } catch {
        continue;
      }
      switch (spec.kind) {
        case 'controller': {
          const c = parseController(f, text);
          idx.endpoints.push(...c.endpoints);
          if (c.className) idx.controllers.push({ name: c.className, file: c.file, services: c.services });
          break;
        }
        case 'appService':
        case 'appInterface':
        case 'dto':
        case 'validator':
        case 'entity':
          idx.csTypes.push(...parseCsTypes(f, text, spec.kind));
          break;
        case 'apiService':
          idx.apiFunctions.push(...parseApiService(f, text));
          break;
        case 'hook':
          idx.hooks.push(...parseHooks(f, text));
          break;
        case 'page':
          idx.pages.push(...parsePages(f, text));
          break;
      }
    }
  }

  // Routes from App.tsx
  try {
    const appTsx = path.join(ROOT, 'src', 'web', 'src', 'App.tsx');
    const text = fs.readFileSync(appTsx, 'utf8');
    INDEX_FILES.set(appTsx, fs.statSync(appTsx).mtimeMs);
    idx.routes = parseRoutes(text);
  } catch {
    /* App.tsx missing — index still usable */
  }

  INDEX = idx;
  return idx;
}

function ensureFresh() {
  if (!INDEX) return buildIndex();
  // Cheap staleness check: any indexed file changed/removed?
  for (const [f, mtime] of INDEX_FILES) {
    let st;
    try {
      st = fs.statSync(f);
    } catch {
      return buildIndex(); // deleted
    }
    if (st.mtimeMs !== mtime) return buildIndex();
  }
  return INDEX;
}

// ---------------------------------------------------------------------------
// Query helpers
// ---------------------------------------------------------------------------

function matches(hay, needle) {
  return hay && hay.toLowerCase().includes(needle.toLowerCase());
}

function searchIndex(query, kind) {
  const idx = ensureFresh();
  const q = query.toLowerCase();
  const hits = [];
  const push = (k, item, label) => {
    if (kind && kind !== k) return;
    hits.push({ kind: k, ...item, _label: label });
  };
  for (const e of idx.endpoints) if (matches(e.route, q) || matches(e.action, q) || matches(e.controller, q)) push('endpoint', e, `${e.method} ${e.route}`);
  for (const t of idx.csTypes) if (matches(t.name, q)) push(t.kind, t, t.name);
  for (const f of idx.apiFunctions) if (matches(f.name, q) || f.calls.some((c) => matches(c.endpoint, q))) push('apiFunction', f, f.name);
  for (const h of idx.hooks) if (matches(h.name, q)) push('hook', h, h.name);
  for (const p of idx.pages) if (matches(p.name, q)) push('page', p, p.name);
  for (const r of idx.routes) if (matches(r.path, q) || r.components.some((c) => matches(c, q))) push('route', r, r.path);
  for (const c of idx.controllers) if (matches(c.name, q)) push('controller', c, c.name);
  return hits;
}

function traceFeature(term) {
  const idx = ensureFresh();
  const t = term.toLowerCase();
  const singular = t.endsWith('ies') ? t.slice(0, -3) + 'y' : t.endsWith('s') ? t.slice(0, -1) : t;
  const hit = (s) => s && (s.toLowerCase().includes(t) || s.toLowerCase().includes(singular));

  const routes = idx.routes.filter((r) => hit(r.path) || r.components.some(hit));
  const routeComps = new Set(routes.flatMap((r) => r.components));
  const pages = idx.pages.filter((p) => hit(p.name) || routeComps.has(p.name));
  const pageHooks = new Set(pages.flatMap((p) => p.hooks));
  const hooks = idx.hooks.filter((h) => hit(h.name) || pageHooks.has(h.name));
  const hookUses = new Set(hooks.flatMap((h) => h.uses));
  const apiFns = idx.apiFunctions.filter((f) => hit(f.name) || hookUses.has(f.name) || f.calls.some((c) => hit(c.endpoint)));
  const fnEndpoints = apiFns.flatMap((f) => f.calls.map((c) => c.endpoint.split('?')[0]));
  const endpoints = idx.endpoints.filter(
    (e) => hit(e.route) || hit(e.controller) || fnEndpoints.some((fe) => routeMatch(e.route, '/api/v1' + (fe.startsWith('/') ? fe : '/' + fe)))
  );
  const controllerNames = new Set(endpoints.map((e) => e.controller));
  const controllers = idx.controllers.filter((c) => controllerNames.has(c.name) || hit(c.name));
  const svcNames = new Set(controllers.flatMap((c) => c.services));
  const services = idx.csTypes.filter(
    (x) => (x.kind === 'appService' || x.kind === 'appInterface') && (hit(x.name) || svcNames.has(x.name) || svcNames.has('I' + x.name))
  );
  const entities = idx.csTypes.filter((x) => x.kind === 'entity' && hit(x.name));
  const dtos = idx.csTypes.filter((x) => x.kind === 'dto' && hit(x.name));

  return { term, routes, pages, hooks, apiFunctions: apiFns, endpoints, controllers, applicationServices: services, entities, dtos };
}

/** Loose route match: template segments {x} / :x match any concrete segment. */
function routeMatch(templateRoute, concrete) {
  const a = templateRoute.split('/').filter(Boolean);
  const b = concrete.split('/').filter(Boolean);
  if (a.length !== b.length) return false;
  return a.every((seg, i) => /^\{.*\}$/.test(seg) || /^:/.test(seg) || /^\$\{/.test(b[i]) || seg.toLowerCase() === b[i].toLowerCase());
}

// ---------------------------------------------------------------------------
// MCP tools
// ---------------------------------------------------------------------------

const TOOLS = [
  {
    name: 'get_conventions',
    description:
      'REQUIRED READING before writing code: returns the canonical, agreed conventions for Koinon RMS (layering, API envelope, IdKey, single frontend client, feature-slice flow). Use this instead of inferring conventions from grep samples.',
    inputSchema: { type: 'object', properties: {}, additionalProperties: false },
    handler: () => {
      try {
        return fs.readFileSync(CONVENTIONS_PATH, 'utf8');
      } catch {
        return 'ERROR: docs/reference/conventions.md not found.';
      }
    },
  },
  {
    name: 'search_index',
    description:
      'Search the structural index of the codebase (endpoints, controllers, application services/interfaces, DTOs, entities, validators, frontend api functions, hooks, pages, routes). Returns typed hits with file:line. Use before grep: this tells you what exists and where.',
    inputSchema: {
      type: 'object',
      properties: {
        query: { type: 'string', description: 'Substring to match (e.g. "person", "checkin", "/families")' },
        kind: {
          type: 'string',
          description: 'Optional filter',
          enum: ['endpoint', 'controller', 'appService', 'appInterface', 'dto', 'entity', 'validator', 'apiFunction', 'hook', 'page', 'route'],
        },
      },
      required: ['query'],
      additionalProperties: false,
    },
    handler: (a) => {
      const hits = searchIndex(a.query, a.kind);
      if (!hits.length) return `No index hits for "${a.query}"${a.kind ? ` (kind=${a.kind})` : ''}. Try a shorter substring or trace_feature.`;
      return hits
        .slice(0, 80)
        .map((h) => {
          const loc = h.file ? ` — ${h.file}${h.line ? ':' + h.line : ''}` : '';
          const extra =
            h.kind === 'endpoint'
              ? ` [auth: ${h.auth}] → ${h.controller}.${h.action}`
              : h.kind === 'apiFunction'
                ? ` → ${h.calls.map((c) => c.verb.toUpperCase() + ' ' + c.endpoint).join(', ')}`
                : h.kind === 'route'
                  ? ` → ${h.components.join(', ')}`
                  : h.inherits
                    ? ` : ${h.inherits}`
                    : '';
          return `[${h.kind}] ${h._label}${extra}${loc}`;
        })
        .join('\n') + (hits.length > 80 ? `\n… ${hits.length - 80} more (narrow the query)` : '');
    },
  },
  {
    name: 'list_endpoints',
    description: 'List all indexed API endpoints (method, route, controller.action, auth). Optionally filter by route prefix.',
    inputSchema: {
      type: 'object',
      properties: { prefix: { type: 'string', description: 'Route prefix filter, e.g. /api/v1/people' } },
      additionalProperties: false,
    },
    handler: (a) => {
      const idx = ensureFresh();
      const eps = idx.endpoints.filter((e) => !a.prefix || e.route.toLowerCase().startsWith(a.prefix.toLowerCase()));
      return eps.map((e) => `${e.method.padEnd(6)} ${e.route} → ${e.controller}.${e.action} [${e.auth}] (${e.file}:${e.line})`).join('\n') || 'No endpoints matched.';
    },
  },
  {
    name: 'trace_feature',
    description:
      'Trace a feature term across every layer: frontend route → page → hooks → api service functions → API endpoints → controller → application services → entities/DTOs. THE tool to understand how an existing feature is wired before adding or changing anything.',
    inputSchema: {
      type: 'object',
      properties: { term: { type: 'string', description: 'Feature term, e.g. "families", "checkin", "giving"' } },
      required: ['term'],
      additionalProperties: false,
    },
    handler: (a) => {
      const t = traceFeature(a.term);
      const fmt = (arr, f) => (arr.length ? arr.slice(0, 25).map(f).join('\n') : '  (none found)');
      return [
        `# Feature trace: "${a.term}"`,
        `\n## Frontend routes (App.tsx)`,
        fmt(t.routes, (r) => `  ${r.path} → ${r.components.join(', ')}`),
        `\n## Pages`,
        fmt(t.pages, (p) => `  ${p.name} (${p.file}:${p.line}) hooks: ${p.hooks.join(', ') || '-'}`),
        `\n## Hooks`,
        fmt(t.hooks, (h) => `  ${h.name} (${h.file}:${h.line}) uses: ${h.uses.join(', ') || '-'}`),
        `\n## API service functions`,
        fmt(t.apiFunctions, (f) => `  ${f.name} (${f.file}:${f.line}) → ${f.calls.map((c) => c.verb.toUpperCase() + ' ' + c.endpoint).join(', ') || '-'}`),
        `\n## API endpoints`,
        fmt(t.endpoints, (e) => `  ${e.method} ${e.route} → ${e.controller}.${e.action} [${e.auth}] (${e.file}:${e.line})`),
        `\n## Controllers (injected services)`,
        fmt(t.controllers, (c) => `  ${c.name} (${c.file}) ← ${c.services.join(', ') || '-'}`),
        `\n## Application services/interfaces`,
        fmt(t.applicationServices, (s) => `  [${s.kind}] ${s.name} (${s.file}:${s.line})`),
        `\n## Entities`,
        fmt(t.entities, (e) => `  ${e.name} (${e.file}:${e.line})`),
        `\n## DTOs`,
        fmt(t.dtos, (d) => `  ${d.name} (${d.file}:${d.line})`),
      ].join('\n');
    },
  },
  {
    name: 'get_stats',
    description: 'Index statistics: what is indexed and when it was built. Use to sanity-check index coverage.',
    inputSchema: { type: 'object', properties: {}, additionalProperties: false },
    handler: () => {
      const idx = ensureFresh();
      return JSON.stringify(
        {
          builtAt: idx.builtAt,
          filesIndexed: INDEX_FILES.size,
          endpoints: idx.endpoints.length,
          controllers: idx.controllers.length,
          csTypes: idx.csTypes.length,
          apiFunctions: idx.apiFunctions.length,
          hooks: idx.hooks.length,
          pages: idx.pages.length,
          routes: idx.routes.length,
        },
        null,
        2
      );
    },
  },
  {
    name: 'reindex',
    description: 'Force a full rebuild of the index (it also auto-rebuilds when indexed files change, so this is rarely needed).',
    inputSchema: { type: 'object', properties: {}, additionalProperties: false },
    handler: () => {
      buildIndex();
      return `Reindexed ${INDEX_FILES.size} files at ${INDEX.builtAt}.`;
    },
  },
];

// ---------------------------------------------------------------------------
// MCP stdio transport (newline-delimited JSON-RPC 2.0)
// ---------------------------------------------------------------------------

function send(msg) {
  process.stdout.write(JSON.stringify(msg) + '\n');
}

function handle(req) {
  const { id, method, params } = req;
  const isRequest = id !== undefined && id !== null;

  switch (method) {
    case 'initialize':
      return send({
        jsonrpc: '2.0',
        id,
        result: {
          protocolVersion: (params && params.protocolVersion) || '2025-06-18',
          capabilities: { tools: {} },
          serverInfo: { name: 'koinon-index', version: '1.0.0' },
        },
      });
    case 'notifications/initialized':
    case 'notifications/cancelled':
      return; // notifications: no response
    case 'ping':
      return send({ jsonrpc: '2.0', id, result: {} });
    case 'tools/list':
      return send({
        jsonrpc: '2.0',
        id,
        result: { tools: TOOLS.map(({ name, description, inputSchema }) => ({ name, description, inputSchema })) },
      });
    case 'tools/call': {
      const tool = TOOLS.find((t) => t.name === params.name);
      if (!tool) {
        return send({ jsonrpc: '2.0', id, error: { code: -32602, message: `Unknown tool: ${params.name}` } });
      }
      try {
        const text = tool.handler(params.arguments || {});
        return send({ jsonrpc: '2.0', id, result: { content: [{ type: 'text', text }] } });
      } catch (err) {
        return send({ jsonrpc: '2.0', id, result: { content: [{ type: 'text', text: `Tool error: ${err.message}` }], isError: true } });
      }
    }
    default:
      if (isRequest) send({ jsonrpc: '2.0', id, error: { code: -32601, message: `Method not found: ${method}` } });
  }
}

buildIndex();

const rl = readline.createInterface({ input: process.stdin, terminal: false });
rl.on('line', (line) => {
  line = line.trim();
  if (!line) return;
  let msg;
  try {
    msg = JSON.parse(line);
  } catch {
    return send({ jsonrpc: '2.0', id: null, error: { code: -32700, message: 'Parse error' } });
  }
  try {
    handle(msg);
  } catch (err) {
    if (msg.id !== undefined) send({ jsonrpc: '2.0', id: msg.id, error: { code: -32603, message: err.message } });
  }
});
rl.on('close', () => process.exit(0));
