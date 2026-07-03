#!/usr/bin/env node
// Cross-platform graph-baseline drift check (mirrors the CI logic):
// fails if tools/graph/graph-baseline.json differs from HEAD beyond
// generated_at timestamps. Run AFTER `npm run graph:update`.
import { execSync } from 'node:child_process';

const diff = execSync('git diff -- tools/graph/graph-baseline.json', { encoding: 'utf8' });
const structural = diff
  .split('\n')
  .filter((l) => /^[+-]/.test(l) && !l.startsWith('+++') && !l.startsWith('---'))
  .filter((l) => !l.includes('"generated_at"'));

if (structural.length > 0) {
  console.error('✗ Graph baseline has drifted (' + structural.length + ' changed lines).');
  console.error('  Run "npm run graph:update" and commit tools/graph/graph-baseline.json with your change.');
  process.exit(1);
}
console.log('✓ Graph baseline is up to date (timestamp-only changes ignored)');
