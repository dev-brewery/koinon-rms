#!/usr/bin/env node
// Blocks commits that change generated graph JSON without the source change
// that caused it. Generated files may only move together with src/** or a
// generator change in tools/graph/ — a regenerated baseline committed alone
// is CI-greening, not a fix.
//
//   node scripts/guard-baseline.mjs                  # staged files (pre-commit)
//   node scripts/guard-baseline.mjs --range A...B    # diff range (CI)
//   node scripts/guard-baseline.mjs --files a,b,c    # explicit list (tests)
import { execSync } from 'node:child_process';

const GENERATED = /^tools\/graph\/(backend-graph|frontend-graph|graph-baseline)\.json$/;
const isSource = (f) => /^src\//.test(f) || (/^tools\/graph\//.test(f) && !GENERATED.test(f));

const args = process.argv.slice(2);
let files;
const filesArg = args.indexOf('--files');
const rangeArg = args.indexOf('--range');
if (filesArg >= 0) {
  files = (args[filesArg + 1] ?? '').split(',').filter(Boolean);
} else {
  const cmd = rangeArg >= 0
    ? `git diff --name-only ${args[rangeArg + 1]}`
    : 'git diff --cached --name-only';
  files = execSync(cmd, { encoding: 'utf8' }).split('\n').filter(Boolean).map((f) => f.replace(/\\/g, '/'));
}

const generated = files.filter((f) => GENERATED.test(f));
if (generated.length > 0 && !files.some(isSource)) {
  console.error('✗ BLOCKED: generated graph JSON changed with no src/ or generator change:');
  for (const f of generated) console.error(`    ${f}`);
  console.error('  A regenerated baseline is never the fix. If graph-validation is red');
  console.error('  without a structural change, the generator is nondeterministic:');
  console.error('  fix tools/graph/generate-*.js|py, prove byte-identical output on');
  console.error('  Windows AND Linux, and commit the generator change WITH the regen.');
  process.exit(1);
}
console.log('✓ Graph baseline guard passed');
