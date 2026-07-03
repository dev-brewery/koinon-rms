#!/usr/bin/env node
// Cross-platform python launcher: Windows ships `python`/`py`, most Linux
// distros ship only `python3`. Usage: node scripts/py.mjs <args...>
import { spawnSync } from 'node:child_process';

const candidates = process.platform === 'win32'
  ? [['python', []], ['py', ['-3']]]
  : [['python3', []], ['python', []]];

// Force UTF-8 stdout: Windows Python defaults to cp1252 and crashes on the
// checkmark/emoji output several repo scripts print.
const env = { ...process.env, PYTHONUTF8: '1', PYTHONIOENCODING: 'utf-8' };

for (const [cmd, pre] of candidates) {
  const probe = spawnSync(cmd, [...pre, '--version'], { stdio: 'ignore', shell: true, env });
  if (probe.status === 0) {
    const run = spawnSync(cmd, [...pre, ...process.argv.slice(2)], { stdio: 'inherit', shell: true, env });
    process.exit(run.status ?? 0);
  }
}

console.error('✗ No python interpreter found (tried: ' +
  candidates.map(([c]) => c).join(', ') + '). Install Python 3.10+.');
process.exit(1);
