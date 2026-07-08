// Shared pieces of the impact-analysis enforcement: pre-tool-guard.mjs (the
// gate) and impact-analyze.mjs (the analyzer) both import from here. Keep this
// dependency-free and boring — if it breaks, the guard fails CLOSED and all
// tool calls block until a human repairs it.
import { execSync } from 'node:child_process';
import { readFileSync } from 'node:fs';
import { join } from 'node:path';

// How long an analysis stays valid. Edits demand a recent look; a commit at
// the end of a long task may rely on analysis done when the task started.
export const EDIT_TTL_MS = 90 * 60 * 1000;
export const COMMIT_TTL_MS = 24 * 60 * 60 * 1000;

// What counts as "code" for the analyze-before-edit gate. Extensions plus the
// named config files whose changes ripple (package.json scripts, tsconfig,
// appsettings, compose). Markdown/docs are deliberately ungated.
export const CODE_FILE =
  /\.(cs|csproj|props|targets|ts|tsx|js|jsx|mjs|cjs|py|ps1|psm1|sh|sql|ipynb)$|(^|[\\/])(package\.json|tsconfig[^\\/]*\.json|appsettings[^\\/]*\.json|docker-compose[^\\/]*\.ya?ml)$/i;

export function repoRoot() {
  return execSync('git rev-parse --show-toplevel', { encoding: 'utf8' }).trim();
}

// The ledger lives under .claude/ on purpose: the guard blocks every agent
// tool from writing there, so entries can only come from impact-analyze.mjs.
export function ledgerPath(root) {
  return join(root, '.claude', 'impact-ledger.json');
}

export function normalizeKey(rel) {
  return rel.replaceAll('\\', '/').replace(/^\.\//, '').toLowerCase();
}

export function loadLedger(path) {
  try {
    return JSON.parse(readFileSync(path, 'utf8'));
  } catch {
    return {};
  }
}

// Every uncommitted code file: staged, unstaged, and untracked (-uall so
// files inside untracked directories are listed individually).
export function changedCodeFiles(root) {
  const out = execSync('git status --porcelain -uall', { cwd: root, encoding: 'utf8' });
  const files = [];
  for (const line of out.split('\n')) {
    if (!line.trim()) continue;
    let p = line.slice(3).trim();
    if (p.includes(' -> ')) p = p.split(' -> ').pop();
    p = p.replace(/^"|"$/g, '');
    if (!p || p.endsWith('/')) continue;
    if (CODE_FILE.test(p)) files.push(p.replaceAll('\\', '/'));
  }
  return files;
}
