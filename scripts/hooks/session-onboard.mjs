#!/usr/bin/env node
// SessionStart onboarding gate. Injects the covenant + the mandate DIRECTLY into
// the session's context (not the deprioritized auto-memory channel), so a fresh
// agent is FORCED to confront the shared understanding before it acts — instead
// of relying on it randomly finding the signal in the noise (the exact drift the
// harness exists to counter). Wired via .claude/settings.json SessionStart hook.
//
// Emits the documented SessionStart JSON: hookSpecificOutput.additionalContext,
// which Claude Code wraps as a system reminder and inserts into the conversation.
import { readFileSync } from 'node:fs';
import { execSync } from 'node:child_process';
import { join } from 'node:path';

let root;
try {
  root = execSync('git rev-parse --show-toplevel', { encoding: 'utf8' }).trim();
} catch {
  process.exit(0); // not in the repo — nothing to inject, do not disrupt other projects
}

let covenant = '';
try {
  covenant = readFileSync(join(root, 'docs', 'claude', 'covenant.md'), 'utf8');
} catch {
  // Covenant missing = the onboarding gate cannot do its job. Surface loudly
  // rather than silently skip (quality-over-autonomy: do not degrade quietly).
  covenant = '⚠ docs/claude/covenant.md is MISSING — the onboarding covenant could not be loaded. Restore it before code work.';
}

const context = [
  '════════ ONBOARDING GATE — read before ANY code work ════════',
  '',
  'You are inheriting an enforcement harness built to counter this model family\'s',
  'measured tendency to drift, cut corners, and report done on work that was not.',
  'The harness is not friction; it is the reason the app can become something users',
  'rely on. Do NOT weaken, bypass, or route around it.',
  '',
  'TOP VALUE — quality is the invariant, autonomy is disposable. If RAG, the index,',
  'or the architect is down, HALT and summon the owner. Do not fall back to grep, do',
  'not proceed half-blind, do not report done. A blocked cycle waiting on the owner is',
  'the harness working.',
  '',
  'REQUIRED FIRST ACTION: read the covenant below and REPEAT IT BACK to the owner in',
  'your own words before you touch any code. The readback is the proof you processed',
  'it — you cannot skip it, and "I read it" without a readback does not count.',
  '',
  'Then: current build state and next steps are in the harness-build-state memory and',
  'docs/claude/harness-implementation-plan.md. The design/why is in the memory files',
  '(quality-over-autonomy, drift-counterweight-design, architect-review-cycle).',
  '',
  '──────── docs/claude/covenant.md ────────',
  covenant.trim(),
].join('\n');

process.stdout.write(JSON.stringify({
  hookSpecificOutput: {
    hookEventName: 'SessionStart',
    additionalContext: context,
  },
}) + '\n');
process.exit(0);
