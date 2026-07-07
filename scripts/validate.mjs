#!/usr/bin/env node
// Cross-platform local validation (replaces validate-staged.sh / validate-local.sh).
// Zero dependencies; Node is guaranteed (husky requires it). Works on Windows
// and Linux — never assume bash for anything a developer must run.
//
//   node scripts/validate.mjs --staged   # pre-commit: only what the staged files need
//   node scripts/validate.mjs            # pre-push: build + tests + typecheck + lint
//
// Graph baseline validation is NOT run here — it regenerates files and needs
// python; CI's graph-validation job is the gate for that.
import { execSync, spawnSync } from 'node:child_process';
import { existsSync } from 'node:fs';
import { resolve, dirname } from 'node:path';
import { fileURLToPath } from 'node:url';

const repoRoot = resolve(dirname(fileURLToPath(import.meta.url)), '..');
process.chdir(repoRoot);

const staged = process.argv.includes('--staged');
const start = Date.now();

// Let a newer installed SDK/runtime run net8.0 tools locally; CI pins its own.
const env = { ...process.env, DOTNET_ROLL_FORWARD: process.env.DOTNET_ROLL_FORWARD || 'LatestMajor' };

function run(label, command) {
  console.log(`\n=== ${label} ===`);
  const result = spawnSync(command, { shell: true, stdio: 'inherit', env });
  if (result.status !== 0) {
    console.error(`\n✗ ${label} FAILED (fix it or run the command yourself: ${command})`);
    process.exit(result.status ?? 1);
  }
  console.log(`✓ ${label} passed`);
}

function requireTool(tool, hint) {
  const probe = spawnSync(tool, ['--version'], { shell: true, stdio: 'ignore', env });
  if (probe.status !== 0) {
    console.error(`✗ '${tool}' is not on PATH. ${hint}`);
    console.error("  (Do not bypass with --no-verify — CI runs the same checks and will fail.)");
    process.exit(1);
  }
}

function stagedFiles() {
  return execSync('git diff --cached --name-only --diff-filter=ACM', { encoding: 'utf8' })
    .split('\n').filter(Boolean);
}

if (staged) {
  run('Graph baseline guard', 'node scripts/guard-baseline.mjs');

  const files = stagedFiles();
  const cs = files.filter((f) => f.endsWith('.cs'));
  const webTs = files.filter((f) => /^src\/web\/.*\.(ts|tsx)$/.test(f));

  if (cs.length === 0 && webTs.length === 0) {
    console.log('✓ No C#/frontend TypeScript changes staged — skipping validation');
    process.exit(0);
  }

  if (cs.length > 0) {
    requireTool('dotnet', 'Install the .NET 8 SDK: https://dotnet.microsoft.com/download/dotnet/8.0');
    run('Backend build (staged .cs files)', 'dotnet build --nologo -v q');
  }

  if (webTs.length > 0) {
    if (!existsSync('src/web/node_modules')) {
      console.error("✗ src/web/node_modules missing — run: npm --prefix src/web ci");
      process.exit(1);
    }
    run('Frontend typecheck', 'npm run --prefix src/web typecheck');
    const quoted = webTs.map((f) => `"${f}"`).join(' ');
    run('Frontend lint (staged files)', `npx --prefix src/web eslint --max-warnings 0 ${quoted}`);
  }
} else {
  requireTool('dotnet', 'Install the .NET 8 SDK: https://dotnet.microsoft.com/download/dotnet/8.0');
  run('Backend build', 'dotnet build --nologo -v q');
  run('Backend tests (includes architecture tests)', 'dotnet test --nologo -v q --no-build');
  if (!existsSync('src/web/node_modules')) {
    console.error("✗ src/web/node_modules missing — run: npm --prefix src/web ci");
    process.exit(1);
  }
  run('Frontend typecheck', 'npm run --prefix src/web typecheck');
  run('Frontend lint', 'npm run --prefix src/web lint');
  run('Frontend tests', 'npm run --prefix src/web test');
}

console.log(`\n✓ All checks passed (${Math.round((Date.now() - start) / 1000)}s) — safe to ${staged ? 'commit' : 'push'}.`);
