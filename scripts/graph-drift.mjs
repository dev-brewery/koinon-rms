#!/usr/bin/env node
// Cross-platform graph-baseline drift check. Run AFTER `npm run graph:update`.
//
// The graph generators discover relationships from source scans where ordering
// is not part of the contract. CI should fail on semantic graph drift, not on a
// platform/runtime-specific reshuffle of equivalent arrays. Compare a canonical
// JSON form against HEAD with generated_at stripped everywhere.
import { execSync } from 'node:child_process';
import { readFileSync } from 'node:fs';

const graphPath = 'tools/graph/graph-baseline.json';

function normalize(value) {
  if (Array.isArray(value)) {
    return value
      .map(normalize)
      .sort((a, b) => JSON.stringify(a).localeCompare(JSON.stringify(b)));
  }

  if (value && typeof value === 'object') {
    return Object.fromEntries(
      Object.entries(value)
        .filter(([key]) => key !== 'generated_at')
        .sort(([a], [b]) => a.localeCompare(b))
        .map(([key, child]) => [key, normalize(child)]),
    );
  }

  return value;
}

function canonicalJson(text) {
  return JSON.stringify(normalize(JSON.parse(text)), null, 2);
}

const headText = execSync(`git show HEAD:${graphPath}`, { encoding: 'utf8' });
const worktreeText = readFileSync(graphPath, 'utf8');

if (canonicalJson(headText) !== canonicalJson(worktreeText)) {
  const diff = execSync(`git diff -- ${graphPath}`, { encoding: 'utf8' });
  const structural = diff
    .split('\n')
    .filter((l) => /^[+-]/.test(l) && !l.startsWith('+++') && !l.startsWith('---'))
    .filter((l) => !l.includes('"generated_at"'));

  console.error('✗ Graph baseline has semantic drift (' + structural.length + ' changed diff lines).');
  console.error('  Run "npm run graph:update" and commit tools/graph/graph-baseline.json with your change.');
  process.exit(1);
}

console.log('✓ Graph baseline is up to date (semantic compare; ordering/timestamp changes ignored)');
