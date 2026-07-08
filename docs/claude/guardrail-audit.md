# Guardrail Audit — Claude Code Enforcement Layer

Every verdict cites the raw docs corpus in `docs/claude/research/` (curled byte-for-byte
from code.claude.com/docs — never WebFetch). Cite form: `research/<file>.md:L<line>`.

## G1 — `scripts/hooks/pre-tool-guard.mjs` + `.claude/settings.json`

Audited by the `docs-authority` subagent against 14 corpus articles; the two highest-stakes
verdicts (M8 fail-open, M1 built-in protected paths) were re-verified against raw bytes by hand.

| # | Mechanism | Verdict | Disposition |
|---|-----------|---------|-------------|
| M1 | Path-regex deny of Edit/Write | reimplements-native-feature | ADD `permissions.deny` as fail-closed backstop; KEEP hook for bypassPermissions-mode hard-deny (research/hooks-guide.md:L905) |
| M2 | Command-string write-inference | misaligned + bypassable | FIX read false-positives; native `deny` for hard cases; hook keeps only argument-conditional checks (research/permissions.md:L211) |
| M3 | exit-2 deny | aligned | keep |
| M4 | Matcher | aligned + gap | EXTEND to MCP tools — `mcp__postgres__*` currently bypasses the DROP check (research/hooks-reference.md:L264-276) |
| M5 | Hook guards itself | bypassable | DOCUMENT managed-settings deployment (the only documented tamper layer, research/settings.md:L218); repo hook labeled as best-effort |
| M6 | Impact ledger/TTL block | aligned mechanism | KEEP THE BLOCK — user decision; do not soften to allow+inject. Bugfix only (G2) |
| M7 | Git-safety | mixed | ADD `git stash drop/clear`; conditional checks stay in hook |
| M8 | Fail-closed | **BROKEN → fail-open** | Native `deny` rules are the real fail-closed layer; scope hook's exit-2-on-broken-internals off the path checks |

### The M8 finding (verified verbatim)
`research/hooks-reference.md:L672`: *"Claude Code treats exit code 1 as a non-blocking
error and proceeds with the action ... If your hook is meant to enforce a policy, use
`exit 2`."* A syntax error, missing Node, or a throw before `block()` runs exits non-2 →
the tool call **proceeds**. The guard's "fail-closed" claim only held for in-process
errors it could catch. `permissions.deny` (evaluated by the platform, not the hook)
closes this: `research/permissions.md:L455` — a denied tool "no other level can allow".

### M1 nuance (verified verbatim)
`.claude`, `.husky`, `.git`, `.mcp.json` are built-in protected paths, but in `default`
mode they are **Prompted, not Denied** (`research/permission-modes.md:L390`), and a
session "allow Claude to edit its own settings" click opens them (L395). So the hook's
hard-deny still adds value over the built-in floor — it is not fully redundant.

## Decisions (2026-07-05)
- **Impact gate stays a hard block.** Not softened to allow+inject. (User.)
- **Managed settings: document, don't deploy.** Handoff docs will specify the per-device
  `managed-settings.json` + `allowManagedHooksOnly` setup so each cloning dev can install
  it. (User.)
- **All audit fixes are additive strength.** Native `deny` backstops the hook; nothing
  that exists is removed or weakened.

## G1 status: IMPLEMENTED + VERIFIED LIVE (2026-07-05)
Window #2 installed the rewritten guard + settings.json deny rules. Verified in the live
session: (a) hook re-armed — `git stash drop` blocked with the new M7 message; (b)
`permissions.deny` live and independent — `Read(.env)` denied by the platform, not the
hook, proving the fail-closed backstop for M8. 48/48 scratchpad suite green incl. the
broken-impact-common proof. Changes: reads no longer false-blocked (M2); MCP postgres
DROP now guarded (M4); `git stash drop/clear` added (M7); impact-common failure scoped to
the impact gate only (M8); native deny rules back the hook (M1/M8). Handoff for the
managed-settings tamper layer (M5): docs/claude/managed-settings-setup.md.

## G2 — impact-gate analyzer: IMPLEMENTED + VERIFIED (2026-07-05)
`get_impact_analysis` in `tools/mcp-koinon-dev/src/index.ts` had THREE schema-mismatch
defects against the committed baseline (verified across all 195 `api_functions` values):
1. **Crash** — `(baseline.api_functions || []).filter` treated an object-keyed-by-name as
   an array → `.filter is not a function` on every call. Fix: `Object.values(... || {})`.
2. **Silent-empty** — read `fn.return_type` (never present; field is `responseType`) →
   DTO branches always returned zero frontend connections. Fix: `fn.responseType`.
3. **Wrong paths** — rebuilt paths from `fn.name` (`getX.ts`) instead of the real
   `fn.path` (`services/api/analytics.ts`). Fix: `src/web/src/${fn.path}`.
Plus a path dedupe (multiple exports share a module file). Rebuilt dist via
`npm run mcp:build` (keeps mcp-dist-drift green). Verified: Person.cs → 36 unique files
across Domain/Application/Frontend, real paths, zero malformed; the impact-analyze HOOK's
structural path now works (35 files) where it silently errored before.

**Root-cause of the regression's invisibility:** mcp-dist-drift proved dist===build(src)
and graph-validation proved baseline===code, but NO gate ever INVOKED the tool. Closed
with `tools/mcp-koinon-dev/smoke-test.mjs` (npm `test` script) that boots the server and
asserts get_impact_analysis returns non-empty, well-formed, dup-free results. CI wiring
proposed in `tools/graph/WORKFLOW-DRAFT-mcp-smoke.yml` (adds one step to the existing
mcp-dist-drift job — human applies).

**Papercut noted for a follow-up:** the commit gate's CODE_FILE regex includes generated
`dist/*.js`, so committing a rebuild demands impact analysis on generated files — the same
exemption generated graph JSON already gets should extend to dist/.

## G3 — settings/permissions hygiene: AUDITED (2026-07-05), remediation pending user decisions
docs-authority audit; key citations re-verified by hand. Most items touch the user's
personal posture or workflow-critical allowlist, so they are user decisions, not auto-fixes.

- **`skipDangerousModePermissionPrompt: true` (user settings)** — skips the confirmation
  before entering bypass mode (settings.md:L366, verified). Latent (no `defaultMode:
  bypassPermissions` set) but wrong direction on a non-isolated host. Fix: remove it, add
  `permissions.disableBypassPermissionsMode: "disable"` (permissions.md:L63/L439). The
  guard hook still fires even in bypass mode (hooks-guide.md:L905), but don't rely on that.
- **`Bash(curl:*)` allow** — docs put curl in DENY (security.md:L52, verified); it's an
  unguarded egress channel. TENSION: the project's own WebFetch-ban process uses `curl`
  to pull raw docs. Resolve deliberately (keep / narrow / deny+alternative).
- **`Bash(docker exec:*)` and `Bash(docker run:*)`** — arbitrary code execution: docker
  exec is NOT wrapper-stripped, so `:*` matches any inner command incl. `rm -rf`
  (permissions.md:L187, verified). Docs prescribe per-inner-command rules. TENSION: the
  demo-stack workflow uses docker exec.
- **`WebFetch(domain:docs.claude.com)` allow** — contradicts the project's WebFetch ban;
  likely redundant (preapproved doc domains, tools-reference.md:L321). Remove.
- **Cruft**: `Bash(docker-compose up:*)`/`down:*` are dead under the superset
  `Bash(docker-compose:*)` (permissions.md:L169).
- **Missing hygiene** (additive): `ask` on `Bash(git push *)` and non-local `dotnet ef
  database update` (permissions.md:L38/L361); `defaultMode` pinned; `ConfigChange` audit
  hook (security.md:L134); home-relative secret denies `Read(~/.ssh/**)`/`Read(~/.aws/**)`.
  Sandbox filesystem/credential denies are the only layer that stops subprocess secret
  reads (permissions.md:L244/L398) — but macOS/Linux/WSL2 only, N/A on native Windows.
- **Placement**: current scopes are correct — deny rules + hook committed in project
  settings (team-shared, trust-exempt); allowlist in local (machine-specific). No moves needed.
- **Plugin `global-enforcer@local-sysadmin`: CLEARED.** Manifest declares only
  name/description/author (author "The Warden"); no hooks, no MCP, no subagents — sole
  content is one slash command (implement.md, TDD mode). Zero autonomous surface.

## Pending groups
G4 CLAUDE.md/memory + missing teeth · G5 skills + chief-architect tool grant · G6 CI cross-check.
