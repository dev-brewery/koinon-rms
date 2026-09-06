// Tests for build-review-artifact.mjs (issue #750, ruling 50776d18).
// Each test gets a FRESH sandbox repo (no shared state — node --test interleaves).
// Battery: happy path + 6 fail-closed refusals + anti-drift (condition 3).
// Usage: node --test scripts/architecture/build-review-artifact.test.mjs

import { test } from 'node:test';
import assert from 'node:assert/strict';
import { execFileSync } from 'node:child_process';
import { mkdtempSync, writeFileSync, mkdirSync, readFileSync, rmSync, existsSync, cpSync } from 'node:fs';
import { createHash } from 'node:crypto';
import { tmpdir } from 'node:os';
import { join, resolve } from 'node:path';
import { fileURLToPath } from 'node:url';

const HERE = resolve(fileURLToPath(import.meta.url), '..');
const sha256 = (s) => createHash('sha256').update(s).digest('hex');

function git(cwd, args) {
  return execFileSync('git', args, { cwd, encoding: 'utf8' });
}

function freshSandbox(baseFiles = {}) {
  const sandbox = mkdtempSync(join(tmpdir(), 'bra750-'));
  const repo = join(sandbox, 'repo');
  for (const d of ['docs/reference', 'docs/architecture/reviews', 'docs/architecture/signers',
    'scripts/architecture', 'src', '.claude/approvals']) {
    mkdirSync(join(repo, d), { recursive: true });
  }
  writeFileSync(join(repo, 'docs', 'reference', 'conventions.md'), '# Conventions\n## Process\nDo things.\n');
  writeFileSync(join(repo, 'docs', 'architecture', 'signers', 'trusted-agent-signers.json'),
    JSON.stringify({ schemaVersion: 1, signers: [] }) + '\n');
  cpSync(join(HERE, 'verify-review-artifact.mjs'), join(repo, 'scripts', 'architecture', 'verify-review-artifact.mjs'));
  cpSync(join(HERE, 'build-review-artifact.mjs'), join(repo, 'scripts', 'architecture', 'build-review-artifact.mjs'));
  for (const [rel, content] of Object.entries(baseFiles)) writeFileSync(join(repo, rel), content);
  git(repo, ['init', '-q']);
  git(repo, ['checkout', '-q', '-b', 'main']);
  git(repo, ['add', '.']);
  git(repo, ['-c', 'user.name=t', '-c', 'user.email=t@t', 'commit', '-q', '-m', 'base']);
  git(repo, ['remote', 'add', 'origin', 'https://github.com/dev-brewery/koinon-rms.git']);
  git(repo, ['update-ref', 'refs/remotes/origin/dev', git(repo, ['rev-parse', 'HEAD']).trim()]);
  return { sandbox, repo };
}

function commitFile(repo, rel, content, msg = 'change') {
  writeFileSync(join(repo, rel), content);
  git(repo, ['add', rel]);
  git(repo, ['-c', 'user.name=t', '-c', 'user.email=t@t', 'commit', '-q', '-m', msg]);
}

function writeRecord(repo, rec) {
  writeFileSync(join(repo, '.claude', 'approvals', `${rec.sha}.json`), JSON.stringify(rec, null, 2));
}

function runBuilder(repo, recordSha, out) {
  const args = ['--record', recordSha];
  if (out) args.push('--out', out);
  try {
    const out2 = execFileSync('node',
      [join(repo, 'scripts', 'architecture', 'build-review-artifact.mjs'), ...args],
      { cwd: repo, encoding: 'utf8' });
    return { code: 0, out: out2, err: '' };
  } catch (e) {
    return { code: e.status ?? 1, out: e.stdout?.toString() ?? '', err: e.stderr?.toString() ?? '' };
  }
}

function approvedRecord(shaChar, files, reasons) {
  return {
    sha: shaChar.repeat(64), ruling: 'APPROVED',
    reasons: reasons ?? [{ standard: 'docs/reference/conventions.md § Process', finding: 'ok' }],
    conditions: [], files, deduced: 'd', proposed: 'p', issue: '750',
    at: new Date().toISOString(),
  };
}

test('happy path: APPROVED record builds a correct artifact', () => {
  const { sandbox, repo } = freshSandbox();
  try {
    commitFile(repo, 'src/foo.py', 'print("v1")\n');
    const fileSha = sha256(readFileSync(join(repo, 'src', 'foo.py')));
    writeRecord(repo, approvedRecord('a', [{ path: 'src/foo.py', hash: fileSha }]));
    const outPath = join(repo, 'docs', 'architecture', 'reviews', 'happy-path-out.json');
    const r = runBuilder(repo, 'a'.repeat(64), outPath);
    assert.equal(r.code, 0, `builder failed: ${r.err}`);
    const art = JSON.parse(readFileSync(outPath, 'utf8'));
    assert.equal(art.architectRuling.ruling, 'APPROVED');
    assert.equal(art.change.files[0].sha256, fileSha);
    assert.equal(art.impact.semantic.standardsRetrieved[0].path, 'docs/reference/conventions.md');
    assert.equal(art.reviewType, 'architect-code-review');
  } finally { rmSync(sandbox, { recursive: true, force: true }); }
});

test('refusal 1: REJECTED record refuses', () => {
  const { sandbox, repo } = freshSandbox();
  try {
    commitFile(repo, 'src/foo.py', 'print("v1")\n');
    writeRecord(repo, { ...approvedRecord('b', []), ruling: 'REJECTED' });
    const r = runBuilder(repo, 'b'.repeat(64));
    assert.equal(r.code, 1); assert.match(r.err, /REJECTED/);
  } finally { rmSync(sandbox, { recursive: true, force: true }); }
});

test('refusal 1b: APPROVED_WITH_CONDITIONS with unresolved conditions refuses', () => {
  const { sandbox, repo } = freshSandbox();
  try {
    commitFile(repo, 'src/foo.py', 'print("v1")\n');
    writeRecord(repo, { ...approvedRecord('c', []), ruling: 'APPROVED_WITH_CONDITIONS', conditions: ['must do x'] });
    const r = runBuilder(repo, 'c'.repeat(64));
    assert.equal(r.code, 1); assert.match(r.err, /unresolved conditions/);
  } finally { rmSync(sandbox, { recursive: true, force: true }); }
});

test('refusal 2: changed file not in record refuses', () => {
  const { sandbox, repo } = freshSandbox();
  try {
    commitFile(repo, 'src/foo.py', 'print("v1")\n');
    writeRecord(repo, approvedRecord('d', [])); // record lists nothing
    const r = runBuilder(repo, 'd'.repeat(64));
    assert.equal(r.code, 1); assert.match(r.err, /not listed in the approval record/);
  } finally { rmSync(sandbox, { recursive: true, force: true }); }
});

test('refusal 3: hash drift after ruling refuses', () => {
  const { sandbox, repo } = freshSandbox();
  try {
    commitFile(repo, 'src/foo.py', 'print("v1")\n');
    writeRecord(repo, approvedRecord('e', [{ path: 'src/foo.py', hash: '0'.repeat(64) }]));
    const r = runBuilder(repo, 'e'.repeat(64));
    assert.equal(r.code, 1); assert.match(r.err, /content drift/);
  } finally { rmSync(sandbox, { recursive: true, force: true }); }
});

test('refusal 4: deleted file refuses', () => {
  const { sandbox, repo } = freshSandbox({ 'src/victim.py': 'print("to be deleted")\n' });
  try {
    git(repo, ['rm', '-q', 'src/victim.py']);
    git(repo, ['-c', 'user.name=t', '-c', 'user.email=t@t', 'commit', '-q', '-m', 'remove']);
    writeRecord(repo, approvedRecord('f', []));
    const r = runBuilder(repo, 'f'.repeat(64));
    assert.equal(r.code, 1); assert.match(r.err, /deletions\/renames/);
  } finally { rmSync(sandbox, { recursive: true, force: true }); }
});

test('refusal 5: missing standard refuses', () => {
  const { sandbox, repo } = freshSandbox();
  try {
    commitFile(repo, 'src/foo.py', 'print("v1")\n');
    const fileSha = sha256(readFileSync(join(repo, 'src', 'foo.py')));
    writeRecord(repo, approvedRecord('1', [{ path: 'src/foo.py', hash: fileSha }],
      [{ standard: 'docs/nope/missing.md', finding: 'ok' }]));
    const r = runBuilder(repo, '1'.repeat(64));
    assert.equal(r.code, 1); assert.match(r.err, /missing standard/);
  } finally { rmSync(sandbox, { recursive: true, force: true }); }
});

test('refusal 6: no gated files refuses', () => {
  const { sandbox, repo } = freshSandbox();
  try {
    commitFile(repo, 'NOTES.txt', 'nothing gated here\n', 'notes');
    writeRecord(repo, approvedRecord('2', [{ path: 'NOTES.txt', hash: sha256('nothing gated here\n') }]));
    const r = runBuilder(repo, '2'.repeat(64));
    assert.equal(r.code, 1); assert.match(r.err, /not required|no gated files/);
  } finally { rmSync(sandbox, { recursive: true, force: true }); }
});

test('condition 2: dirty working tree on ruled path refuses', () => {
  const { sandbox, repo } = freshSandbox();
  try {
    commitFile(repo, 'src/foo.py', 'print("clean")\n');
    writeFileSync(join(repo, 'src', 'foo.py'), 'print("DIRTY")\n'); // uncommitted mutation
    const fileSha = sha256(readFileSync(join(repo, 'src', 'foo.py')));
    writeRecord(repo, approvedRecord('3', [{ path: 'src/foo.py', hash: fileSha }]));
    const r = runBuilder(repo, '3'.repeat(64));
    assert.equal(r.code, 1); assert.match(r.err, /working tree not clean/);
  } finally { rmSync(sandbox, { recursive: true, force: true }); }
});

test('anti-drift (condition 3): builder stripping rule pinned to verifier source', () => {
  const ref = 'docs/reference/conventions.md § Data § deeper #anchor';
  assert.equal(String(ref).split(/[§#]/)[0].trim(), 'docs/reference/conventions.md');
  const src = readFileSync(join(HERE, 'verify-review-artifact.mjs'), 'utf8');
  const m = src.match(/function standardPathFromRef\(ref\) \{\s*return String\(ref\)\.split\(\/\[§#\]\/\)\[0\]\.trim\(\);/);
  assert.ok(m, 'verifier standardPathFromRef changed — builder and verifier may have drifted; update both together');
});
