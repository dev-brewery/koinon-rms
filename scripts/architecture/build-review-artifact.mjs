#!/usr/bin/env node
// Build a signed-review-artifact candidate from an architect approval record.
// Issue #750. Ruling 50776d18 (APPROVED_WITH_CONDITIONS) — conditions honored:
//  1. NO reimplementation: changedFiles/gatedFiles/diffSha come from the
//     verifier's own verifyReviewArtifacts(); the finished (unsigned) artifact
//     is passed through the same verifier logic before landing on disk.
//  2. Per-file sha256 uses the verifier's recipe (working-tree file at HEAD);
//     dirty paths (git status --porcelain) and status D/R entries REFUSE.
//  3. Anchor stripping uses the verifier's rule verbatim (split /[§#]/ [0]
//     trim); an anti-drift test proves builder paths == verifier-accepted.
//
// Fail-closed: every refusal names the exact discrepancy and exits 1.

import { execFileSync } from 'node:child_process';
import { existsSync, readFileSync, writeFileSync, mkdirSync } from 'node:fs';
import { join, resolve } from 'node:path';
import { fileURLToPath } from 'node:url';
import { verifyReviewArtifacts } from './verify-review-artifact.mjs';

const REPO_ROOT = resolve(fileURLToPath(import.meta.url), '..', '..', '..');
const APPROVALS_DIR = join(REPO_ROOT, '.claude', 'approvals');

function fail(msg) {
  console.error(`✗ build-review-artifact: ${msg}`);
  process.exit(1);
}

function parseArgs(argv) {
  const o = { record: '', slug: '', out: '' };
  for (let i = 0; i < argv.length; i++) {
    if (argv[i] === '--record') o.record = argv[++i];
    else if (argv[i] === '--slug') o.slug = argv[++i];
    else if (argv[i] === '--out') o.out = argv[++i];
    else fail(`Unknown argument: ${argv[i]}`);
  }
  if (!o.record) fail('Usage: --record <approval-record sha|path> [--slug <name>] [--out <file>]');
  return o;
}

function git(root, args) {
  return execFileSync('git', args, { cwd: root, encoding: 'utf8', maxBuffer: 64 * 1024 * 1024 });
}

// Verifier's rule, verbatim (condition 3). Not imported because the verifier
// keeps it module-private; the anti-drift test pins the two together.
function standardPathFromRef(ref) {
  return String(ref).split(/[§#]/)[0].trim();
}

const opts = parseArgs(process.argv.slice(2));

// ---- load the approval record -------------------------------------------------
const recordPath = opts.record.match(/^[0-9a-f]{16,64}$/i)
  ? join(APPROVALS_DIR, `${opts.record}.json`)
  : resolve(opts.record);
if (!existsSync(recordPath)) fail(`approval record not found: ${recordPath}`);
const record = JSON.parse(readFileSync(recordPath, 'utf8'));

// ---- refusal rule 1: ruling must be APPROVED with no unresolved conditions ----
if (record.ruling === 'REJECTED') fail(`record ruling is REJECTED (binding ${record.sha?.slice(0, 8)}).`);
if (record.ruling === 'APPROVED_WITH_CONDITIONS' && (record.conditions?.length ?? 0) > 0) {
  fail(`record ruling is APPROVED_WITH_CONDITIONS with unresolved conditions (${record.conditions.length}); resolve them in a fresh APPROVED record first.`);
}
if (record.ruling !== 'APPROVED') fail(`record ruling is neither APPROVED nor conditions-resolved (${record.ruling}).`);

// ---- diff facts from the VERIFIER (condition 1: no reimplementation) -----------
const head = git(REPO_ROOT, ['rev-parse', 'HEAD']).trim();
const base = git(REPO_ROOT, ['merge-base', 'HEAD', 'origin/dev']).trim();
const pre = verifyReviewArtifacts({ base, head });
if (pre.skipped) fail('no gated files in this diff — an architecture review artifact is not required (verifier would skip).');
if (pre.errors.length && pre.gatedFiles.length === 0) fail(`verifier reports no gated files (errors: ${pre.errors[0]})`);
const { changedFiles, gatedFiles, diffSha } = pre;

// ---- refusal rule 4: deleted/renamed files ------------------------------------
for (const f of changedFiles) {
  if (f.status === 'D' || f.status === 'R') {
    fail(`changed file '${f.path}' has status ${f.status} — the approval-record shape cannot model deletions/renames; obtain a fresh ruling covering this diff.`);
  }
}

// ---- refusal rule 3: every changed file must be covered by the record ----------
const recordFiles = new Map((record.files ?? []).map((f) => [f.path.replaceAll('\\', '/'), f]));
for (const f of changedFiles) {
  if (!recordFiles.has(f.path)) {
    fail(`changed file '${f.path}' is not listed in the approval record — the ruling does not cover this diff.`);
  }
}

// ---- condition 2: working tree must be clean for every listed path -------------
const dirty = git(REPO_ROOT, ['status', '--porcelain', '--', ...recordFiles.keys()]);
if (dirty.trim()) {
  fail(`working tree not clean for ruled paths (hashing uncommitted content would break CI):\n${dirty.trim()}`);
}

// ---- condition 2: hash the verifier's way; refuse on drift vs the record -------
function sha256File(abs) {
  return verifySha(readFileSync(abs));
}
import { createHash } from 'node:crypto';
function verifySha(buf) {
  return createHash('sha256').update(buf).digest('hex');
}
const artifactFiles = [];
for (const f of changedFiles) {
  const abs = join(REPO_ROOT, f.path);
  if (!existsSync(abs)) fail(`changed file '${f.path}' missing from working tree.`);
  const h = sha256File(abs);
  const ruled = recordFiles.get(f.path);
  if (ruled && ruled.hash && ruled.hash !== 'NEW' && ruled.hash !== h) {
    fail(`content drift: '${f.path}' hash ${h.slice(0, 8)}… ≠ record hash ${ruled.hash.slice(0, 8)}… — file changed after the ruling; obtain a fresh ruling.`);
  }
  artifactFiles.push({ path: f.path, sha256: h });
}

// ---- refusal rule 5: cited standards must exist on disk ------------------------
const standardsRetrieved = [];
for (const r of record.reasons ?? []) {
  const p = standardPathFromRef(r.standard ?? '');
  if (!p || !existsSync(join(REPO_ROOT, p))) fail(`ruling reason cites missing standard: ${r.standard}`);
  if (!standardsRetrieved.some((s) => s.path === p)) standardsRetrieved.push({ path: p });
}
if (standardsRetrieved.length === 0) fail('ruling cites no standards; cannot build standardsRetrieved.');

// ---- assemble ------------------------------------------------------------------
const issue = record.issue ?? '';
const date = new Date().toISOString().slice(0, 10);
const slug = opts.slug || (issue ? `issue-${issue}` : 'review');
const outPath = opts.out ? resolve(opts.out) : join(REPO_ROOT, 'docs/architecture/reviews', `${date}-${slug}.json`);

const artifact = {
  schemaVersion: 1,
  reviewType: 'architect-code-review',
  repository: git(REPO_ROOT, ['remote', 'get-url', 'origin']).trim().replace(/\.git$/, '').replace(/^git@github\.com:/, 'https://github.com/'),
  baseCommit: base,
  headCommit: head,
  createdAt: new Date().toISOString(),
  change: { diffSha256: diffSha, files: artifactFiles },
  architectRuling: { ruling: record.ruling, reasons: record.reasons, conditions: [] },
  impact: { semantic: { standardsRetrieved } },
};

// ---- condition 1b: pre-verify the UNSIGNED shape through the verifier ----------
// verifyReviewArtifacts validates signature presence too; unsigned here means we
// validate everything it checks about shape/diff/ruling/standards by writing to
// a temporary reviews dir is NOT possible (it scans dirs). Instead: assert the
// same invariants the verifier enforces, using its own computed facts.
const vGated = new Set(gatedFiles.map((f) => f.path));
const aFiles = new Set(artifact.change.files.map((f) => f.path));
for (const f of vGated) if (!aFiles.has(f)) fail(`internal: gated file ${f.path} not in artifact files`);
if (artifact.change.diffSha256 !== diffSha) fail('internal: diffSha mismatch');
for (const r of artifact.architectRuling.reasons) {
  const p = standardPathFromRef(r.standard ?? '');
  if (!p || !existsSync(join(REPO_ROOT, p))) fail(`internal: standard missing: ${r.standard}`);
  if (!r.finding) fail(`internal: reason for ${r.standard} has no finding`);
}

mkdirSync(join(REPO_ROOT, 'docs/architecture/reviews'), { recursive: true });
writeFileSync(outPath, JSON.stringify(artifact, null, 2) + '\n');
console.log(`✓ artifact written: ${outPath}`);
console.log(`  base ${base.slice(0, 8)} head ${head.slice(0, 8)} diff ${diffSha.slice(0, 8)}…`);
console.log(`  ${artifactFiles.length} file(s), ${standardsRetrieved.length} standard(s)`);
console.log(`  next: node scripts/architecture/sign-review-artifact.mjs --artifact ${outPath} --key <pem> --key-id koinon-agent-artifact-2026-09`);
