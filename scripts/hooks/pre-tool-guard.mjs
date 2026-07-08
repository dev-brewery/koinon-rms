#!/usr/bin/env node
// PreToolUse guard — defense-in-depth layer over native permissions.deny rules.
//
// LAYERING (see docs/claude/guardrail-audit.md, G1):
//   - Hard path protection is enforced by `permissions.deny` in
//     .claude/settings.json (research/permissions.md:L241-289). Those rules are
//     evaluated by the platform, so they hold even if THIS hook never runs
//     (research/permissions.md:L455). That is the real fail-closed layer.
//   - This hook adds what deny rules cannot express (research/permissions.md:L211,
//     L243-245): protection against subprocess/shell writes that bypass the Edit
//     tool (`node -e`, redirects, `rm`), argument-conditional git safety, SQL DROP
//     guarding, and the impact-analysis gate. It also hard-denies in
//     bypassPermissions mode, where built-in protected paths are allowed
//     (research/hooks-guide.md:L905, research/permission-modes.md:L393).
//
// [PATCH 2026-07-07] Additions after the enumeration gaps proven that day
// (Agent spawn un-gated; docs/adr canon writable; python-heredoc interpreter
// hole; no approval gate): docs/adr is now canon-protected here AND in deny
// rules; stdin-interpreter forms are matched; Edit/Write/commit on a code file
// additionally require a fresh APPROVED architect ruling from
// scripts/hooks/architect-review.mjs (.claude/approvals/, content-bound).
// `Agent` itself is blocked by permissions.deny — platform-level, not here.
//
// Exit 2 = block, stderr → agent (research/hooks-reference.md:L652). NOTE: any
// non-2 exit PROCEEDS with the tool call (research/hooks-reference.md:L672), so a
// crash here fails OPEN — which is exactly why the deny rules above are the
// backstop and this hook scopes its own failures narrowly (M8).

import { readFileSync, readdirSync } from 'node:fs';
import { createHash } from 'node:crypto';
import { isAbsolute, relative, resolve, join } from 'node:path';

// Protected-path fragment shared by the shell-write checks. Matches the four
// protected dirs, the canon (docs/adr), and the three generated graph JSONs
// anywhere in a command. [PATCH 2026-07-07: + docs/adr]
const PROT = String.raw`(\.claude[\\/]|\.husky[\\/]|\.github[\\/]workflows[\\/]|scripts[\\/]hooks[\\/]|docs[\\/]adr[\\/]|tools[\\/]graph[\\/](?:backend-graph|frontend-graph|graph-baseline)\.json)`;

const PROTECTED_DIRS = /(^|[\\/])(\.claude|\.husky)[\\/]|(^|[\\/])\.github[\\/]workflows[\\/]|(^|[\\/])scripts[\\/]hooks[\\/]/i;
const GENERATED_GRAPH = /tools[\\/]graph[\\/](backend-graph|frontend-graph|graph-baseline)\.json/i;
// [PATCH 2026-07-07] The canon: ADRs are minted through the owner's window
// only. An agent-written ADR is unreviewed precedent (this happened: ADR 0006).
const CANON_DIRS = /(^|[\\/])docs[\\/]adr[\\/]/i;
// Exemptions: harness-owned agent workspaces that are NOT enforcement config —
// auto-memory (~/.claude/projects/<slug>/memory/) and plan-mode plans
// (~/.claude/plans/). Under HOME .claude, so the project-scoped deny rules never
// reach them either (research/permissions.md:L264); the hook mirrors that.
const MEMORY_DIR = /[\\/]\.claude[\\/]projects[\\/][^\\/]+[\\/]memory[\\/]/i;
const PLANS_DIR = /[\\/]\.claude[\\/]plans[\\/]/i;

// [PATCH 2026-07-07] Approval-gate policy: how long an architect ruling
// unlocks its file set, and which rulings count.
const APPROVAL_TTL_MS = 24 * 60 * 60 * 1000;
const APPROVING_RULINGS = new Set(['APPROVED', 'APPROVED_WITH_CONDITIONS']);

function block(reason, fix) {
  console.error(`BLOCKED: ${reason}`);
  if (fix) console.error(fix);
  // The value rides on the gate the agent cannot avoid (quality-over-autonomy).
  console.error('  Quality is the invariant. Do not bypass or route around this gate; if it blocks wrongly, report it to the owner. See docs/claude/covenant.md.');
  process.exit(2);
}

// Lazy: only the impact gate + commit gate need impact-common. A broken module
// therefore fails ONLY those checks closed, not every unrelated tool call (M8).
async function getCommon() {
  try {
    return await import(new URL('./impact-common.mjs', import.meta.url));
  } catch {
    block('impact-gate internals broken (impact-common.mjs) — this gate fails closed; path/git protections stay active. A human must repair scripts/hooks/.');
  }
}

// [PATCH 2026-07-07] Load the set of file paths covered by a fresh, integrity-
// valid APPROVED ruling. A record only counts when its filename and its `sha`
// both equal sha256(files+deduced+proposed) — a tampered or renamed record
// covers nothing. Unreadable/expired/rejected records cover nothing (closed).
function approvedFiles(root, normalizeKey) {
  const dir = join(root, '.claude', 'approvals');
  let names = [];
  try { names = readdirSync(dir); } catch { return new Set(); }
  const covered = new Set();
  const now = Date.now();
  for (const n of names) {
    if (!n.endsWith('.json')) continue;
    try {
      const rec = JSON.parse(readFileSync(join(dir, n), 'utf8'));
      if (!APPROVING_RULINGS.has(rec.ruling)) continue;
      if (!(now - Date.parse(rec.at) < APPROVAL_TTL_MS)) continue;
      const sha = createHash('sha256')
        .update(JSON.stringify({ files: rec.files, deduced: rec.deduced, proposed: rec.proposed }))
        .digest('hex');
      if (sha !== rec.sha || n !== `${sha}.json`) continue;
      for (const f of rec.files) covered.add(normalizeKey(f.path));
    } catch { /* malformed record covers nothing */ }
  }
  return covered;
}

const APPROVAL_FIX = 'Run `node scripts/hooks/architect-review.mjs --files <a,b> --deduced "<diagnosis>" --proposed "<fix>" [--issue N]`. Only an APPROVED ruling unlocks the change; REJECTED means revise the proposal, and infrastructure-down means HALT and summon the owner.';

// ---- M2: shell writes to protected paths, WITHOUT flagging reads --------------
// Each pattern targets a genuine write/delete of a protected path. Reads
// (`cat x`, `cp x /scratch`, `grep`, `2>/dev/null`) are deliberately NOT matched.
function cmdWritesProtected(cmd) {
  // Redirect target is protected: `> .claude/x`, `>> scripts/hooks/y`.
  // A digit or & before `>` is a stream/fd redirect (2>/dev/null, &>) — not a
  // file write we guard, so it is excluded and reads stay allowed.
  const REDIRECT = new RegExp(String.raw`(^|[^0-9&>])>>?\s*["']?[^\s>|&;"']*` + PROT, 'i');
  // tee / delete / write-cmdlets naming a protected path.
  const TEE = new RegExp(String.raw`\btee\b[^|;&]*` + PROT, 'i');
  const DELETE = new RegExp(String.raw`\b(rm|del|rmdir|git\s+rm|Remove-Item)\b[^|;&]*` + PROT, 'i');
  const CMDLET = new RegExp(String.raw`\b(Set-Content|Add-Content|Out-File|New-Item|Clear-Content)\b[^|;]*` + PROT, 'i');
  // cp/mv/Copy-Item/Move-Item only when the protected path is the DESTINATION
  // (last token, allowing trailing flags). `cp .claude/x /scratch/` (read) is
  // allowed; `cp /tmp/x .claude/y` (write) is blocked.
  const COPY_DEST = new RegExp(String.raw`\b(cp|mv|Copy-Item|Move-Item)\b[^|;&]*` + PROT + String.raw`[^\s"';|&]*["']?(\s+-\w+)*\s*$`, 'i');
  // Inline interpreters writing a protected path (`node -e "...writeFileSync('.claude/..')"`).
  // [PATCH 2026-07-07] Also stdin-script forms (`python - <<EOF`, `node -`):
  // the heredoc body is part of the command text, so the write-call check
  // applies to it exactly as it does to -e/-c payloads.
  const INTERP = /\b(node\s+(?:-e|--eval)|deno\s+eval|python3?\s+-c|ruby\s+-e|perl\s+-e)\b|\b(node|python3?|deno|ruby|perl)\s+-\s/i;
  const INTERP_WRITE = /\b(writeFileSync|appendFileSync|copyFileSync|renameSync|createWriteStream|mkdirSync|rmSync|unlinkSync|WriteAllText|WriteAllBytes|open\s*\([^)]*['"][wa])/i;
  const cmdProtected = new RegExp(PROT, 'i').test(cmd);
  return (
    REDIRECT.test(cmd) || TEE.test(cmd) || DELETE.test(cmd) || CMDLET.test(cmd) ||
    COPY_DEST.test(cmd) ||
    (INTERP.test(cmd) && cmdProtected && INTERP_WRITE.test(cmd))
  );
}

function dropOutsideLocalhost(text) {
  return /\bDROP\s+(DATABASE|TABLE|SCHEMA)\b/i.test(text) && !/localhost|127\.0\.0\.1/i.test(text);
}

async function main() {
  let input = '';
  try {
    input = readFileSync(0, 'utf8');
  } catch {
    block('guard could not read hook input (fail-closed)');
  }

  let tool = '', ti = {};
  try {
    const parsed = JSON.parse(input);
    tool = parsed.tool_name ?? '';
    ti = parsed.tool_input ?? {};
  } catch {
    block('guard received unparseable hook input (fail-closed)');
  }

  // ---- Edit-family: path protection + impact gate + approval gate ----------
  if (tool === 'Edit' || tool === 'Write' || tool === 'NotebookEdit') {
    const p = String(ti.file_path ?? ti.notebook_path ?? '');
    if (PROTECTED_DIRS.test(p) && !MEMORY_DIR.test(p) && !PLANS_DIR.test(p)) {
      block(
        `${p} is protected infrastructure — agents cannot modify their own constraints, hooks, or CI.`,
        'Workflow/CI changes: draft in tools/graph/WORKFLOW-DRAFT*.yml for a human. If a hook blocks you wrongly, report it — do not edit it.'
      );
    }
    // [PATCH 2026-07-07] The canon is owner-minted only.
    if (CANON_DIRS.test(p)) {
      block(
        `${p} is the architectural canon (docs/adr/) — precedent is minted only in an owner-opened window.`,
        'Draft the ADR in your scratchpad and hand it to the owner; never write docs/adr/ directly.'
      );
    }
    if (GENERATED_GRAPH.test(p)) {
      block(
        `${p} is generated — direct edits are forbidden.`,
        'Run `npm run graph:update` after a structural change. If graph-validation is red WITHOUT a structural change, fix the generator in tools/graph/ and prove byte-identical output on Windows AND Linux.'
      );
    }
    // Impact gate: no analysis, no edit. Needs impact-common (lazy).
    const common = await getCommon();
    if (common.CODE_FILE.test(p)) {
      const root = common.repoRoot();
      const rel = relative(root, resolve(root, p));
      if (!rel.startsWith('..') && !isAbsolute(rel)) {
        const entry = common.loadLedger(common.ledgerPath(root))[common.normalizeKey(rel)];
        const fwd = rel.replaceAll('\\', '/');
        if (!entry || Date.now() - entry.at > common.EDIT_TTL_MS) {
          block(
            `${fwd} — no current impact analysis. You may not modify code whose blast radius you have not established.`,
            `Run \`node scripts/hooks/impact-analyze.mjs "${fwd}"\`, READ the dependents and layer rules it prints, then retry this edit. Analyses expire after 90 minutes.`
          );
        }
        // [PATCH 2026-07-07] Approval gate: no architect ruling, no edit.
        if (!approvedFiles(root, common.normalizeKey).has(common.normalizeKey(rel))) {
          block(
            `${fwd} — no APPROVED architect ruling covers this file. Diagnosis and proposal must be reviewed before code changes (docs/claude/covenant.md #5).`,
            APPROVAL_FIX
          );
        }
      }
    }
    process.exit(0);
  }

  // ---- MCP tool calls: SQL DROP guard (M4 — matcher now includes mcp__*) ----
  if (tool.startsWith('mcp__')) {
    if (dropOutsideLocalhost(JSON.stringify(ti))) {
      block('SQL DROP via MCP tool outside localhost.', 'Schema changes only via EF migrations.');
    }
    process.exit(0);
  }

  // ---- Shell commands ------------------------------------------------------
  if (tool === 'Bash' || tool === 'PowerShell') {
    const cmd = String(ti.command ?? '');

    if (cmdWritesProtected(cmd)) {
      block(
        'command writes to or deletes protected infrastructure (.claude/, .husky/, .github/workflows/, scripts/hooks/, docs/adr/, or generated graph JSON).',
        'Generated graph JSON: `npm run graph:update` only. CI: draft in tools/graph/WORKFLOW-DRAFT*.yml. Constraints/hooks/ledger/canon: report problems, never modify.'
      );
    }

    if (/--no-verify\b|--no-gpg-sign\b|\bHUSKY=0\b|\bcore\.hooksPath\b|\bhusky\s+uninstall\b/i.test(cmd)) {
      block(
        'hook bypass detected (--no-verify / --no-gpg-sign / HUSKY=0 / core.hooksPath).',
        'If a hook fails wrongly, fix the hook via a human — never bypass it.'
      );
    }

    if (/git\s+push\b[^|;&]*(\s-\w*f\b|--force)/i.test(cmd) && /\b(main|master|dev|develop)\b/i.test(cmd)) {
      block('force push touching a protected branch (main/master/dev/develop).');
    }
    if (/git\s+branch\s+(-D|--delete\s+--force)\s+(main|master|dev|develop)\b/i.test(cmd) ||
        /git\s+push\b[^|;&]*--delete\s+(main|master|dev|develop)\b/i.test(cmd)) {
      block('deletion of a protected branch (main/master/dev/develop).');
    }
    if (/\bgit\s+reset\s+[^|;&]*--hard\b/i.test(cmd)) {
      block('git reset --hard discards uncommitted work.', 'If the user explicitly wants this, they run it with `! git reset --hard`.');
    }
    if (/\bgit\s+(checkout|restore)\s+(--\s+)?(\.|:\/)(\s|$)/i.test(cmd)) {
      block('bulk discard of uncommitted work (git checkout/restore of `.`).', 'Restore specific files by path, or ask the user.');
    }
    if (/\bgit\s+clean\b[^|;&]*\s-\w*[fd]/i.test(cmd)) {
      block('git clean -f/-d deletes untracked files.', 'If the user explicitly wants this, they run it with `! git clean ...`.');
    }
    // M7: git stash drop/clear discards stashed work (research/permission-modes.md:L233).
    if (/\bgit\s+stash\s+(drop|clear)\b/i.test(cmd)) {
      block('git stash drop/clear discards stashed work.', 'If the user explicitly wants this, they run it themselves.');
    }

    if (/\brm\s+-\w*r\w*f|\brm\s+-\w*f\w*r/i.test(cmd) && /(^|\s|["'])\.?[\\/]?(src|docs|tests|tools|\.git)([\\/"'\s]|$)/i.test(cmd)) {
      block('recursive delete of a critical project directory.', 'Clean build artifacts with `dotnet clean` or `npm run clean`.');
    }
    if (/Remove-Item\b[^|;]*-Recurse/i.test(cmd) && /(src|docs|tests|tools|\.git)([\\/"'\s]|$)/i.test(cmd)) {
      block('recursive delete of a critical project directory.');
    }

    if (dropOutsideLocalhost(cmd)) {
      block('SQL DROP outside localhost.', 'Schema changes only via EF migrations.');
    }

    // Commit gate: every changed code file needs impact analysis AND an
    // architect approval on record. [PATCH 2026-07-07: + approval]
    if (/\bgit(\s+-\S+(\s+\S+)?)*\s+commit\b/i.test(cmd)) {
      const common = await getCommon();
      const root = common.repoRoot();
      const ledger = common.loadLedger(common.ledgerPath(root));
      const nowTs = Date.now();
      const changed = common.changedCodeFiles(root);
      const missing = changed.filter((f) => {
        const entry = ledger[common.normalizeKey(f)];
        return !entry || nowTs - entry.at > common.COMMIT_TTL_MS;
      });
      if (missing.length > 0) {
        block(
          `git commit refused — ${missing.length} changed code file(s) have no impact analysis on record:\n    ${missing.join('\n    ')}`,
          'Run `node scripts/hooks/impact-analyze.mjs --changed`, READ the dependents and layer rules for each, then commit.'
        );
      }
      const covered = approvedFiles(root, common.normalizeKey);
      const unapproved = changed.filter((f) => !covered.has(common.normalizeKey(f)));
      if (unapproved.length > 0) {
        block(
          `git commit refused — ${unapproved.length} changed code file(s) have no APPROVED architect ruling:\n    ${unapproved.join('\n    ')}`,
          APPROVAL_FIX
        );
      }
    }

    process.exit(0);
  }

  process.exit(0);
}

await main();
