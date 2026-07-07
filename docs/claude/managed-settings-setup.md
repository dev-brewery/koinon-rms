# Tamper-resistant enforcement via managed settings (per-device setup)

**Who does this:** every developer who clones this repo for agentic work, once per
machine, with local admin rights. It does **not** travel with the clone — managed
settings live outside the repo by design (that is what makes them tamper-resistant).

## Why this exists

The repo ships a PreToolUse guard (`scripts/hooks/pre-tool-guard.mjs`) wired in
`.claude/settings.json`, plus `permissions.deny` rules. Both are **project-scoped**, and
an agent or user can switch them off:

- `"disableAllHooks": true` in user/project/local settings disables the guard —
  *"There is no way to disable an individual hook"* (research/hooks-reference.md:L592).
  Only a **managed-level** `disableAllHooks` is itself immune
  (research/hooks-reference.md:L594).
- A crash in the guard fails **open**: any non-2 exit code proceeds with the tool call
  (research/hooks-reference.md:L672).

Managed settings are the only documented layer a local user cannot override — *"can't be
overridden by any other level, including command line arguments"*
(research/permissions.md:L449). This is the documented answer to G1 finding M5.

## What it costs (decide before deploying)

Managed settings are **machine-wide**, not per-project (research/settings.md:L26,
L104-108). Two consequences to design around:

1. **The hook path.** A managed hook `command` runs for *every* project on the machine.
   Referencing `node scripts/hooks/pre-tool-guard.mjs` (relative) errors harmlessly in
   other repos (non-blocking, research/hooks-reference.md:L654) but is noisy. Preferred:
   reference the guard by **absolute path** (managed settings are per-device and
   uncommitted, so the CLAUDE.md "no machine-specific paths" rule — which is about
   *committed* config — does not apply here), and have the guard early-exit `0` when the
   current repo is not koinon. *(Guard self-scoping is a small G-follow-up; until then,
   accept the cross-project noise or scope via `--settings`.)*
2. **Lockdown keys move enforcement off the repo.** Setting
   `allowManagedHooksOnly: true` blocks *all* user/project hooks
   (research/settings.md:L218) — so the guard must then be defined *in managed settings*,
   not just the repo. Likewise `allowManagedPermissionRulesOnly: true` ignores
   user/project `deny` rules (research/settings.md:L220), so the deny rules must move too.
   You choose the strength:
   - **Belt-and-suspenders (no lockdown):** deploy managed `deny` rules only; leave the
     repo hook in `.claude/settings.json`. Managed deny rules hold even if a user sets
     `disableAllHooks` (they are permission rules, not hooks). The hook itself remains
     disableable. Simple; partial.
   - **Full lockdown:** deploy the hook *and* deny rules in managed settings with
     `allowManagedHooksOnly: true` + `allowManagedPermissionRulesOnly: true` +
     (optionally) `disableBypassPermissionsMode: true`. A local user can no longer
     disable the guard or add permissive rules. Strongest; the repo `.claude/settings.json`
     becomes documentation/fallback.

## Deploy (Windows) — full-lockdown template

1. Create `C:\Program Files\ClaudeCode\managed-settings.json` (admin rights;
   **not** the legacy `C:\ProgramData\...` path, unsupported since v2.1.75 —
   research/settings.md:L108-111). Registry alternative: `HKLM\SOFTWARE\Policies\ClaudeCode`,
   value `Settings` (REG_SZ) containing the same JSON (research/settings.md:L102).

```jsonc
{
  "permissions": {
    "deny": [
      "Edit(//c/home/repos/koinon-rms/.claude/**)",
      "Edit(//c/home/repos/koinon-rms/.husky/**)",
      "Edit(//c/home/repos/koinon-rms/.github/workflows/**)",
      "Edit(//c/home/repos/koinon-rms/scripts/hooks/**)",
      "Edit(//c/home/repos/koinon-rms/tools/graph/backend-graph.json)",
      "Edit(//c/home/repos/koinon-rms/tools/graph/frontend-graph.json)",
      "Edit(//c/home/repos/koinon-rms/tools/graph/graph-baseline.json)"
    ]
  },
  "hooks": {
    "PreToolUse": [
      {
        "matcher": "Bash|PowerShell|Edit|Write|NotebookEdit|mcp__postgres__.*",
        "hooks": [
          { "type": "command", "command": "node //c/home/repos/koinon-rms/scripts/hooks/pre-tool-guard.mjs" }
        ]
      }
    ]
  },
  "allowManagedHooksOnly": true,
  "allowManagedPermissionRulesOnly": true
}
```

Notes:
- `//c/...` is the documented Windows absolute form — paths normalize to POSIX before
  matching, `C:\` → `/c/` (research/permissions.md:L271). Adjust the drive/clone path per
  machine. Because these are absolute (`//`) they match only this clone, not other repos.
- `allowManagedHooksOnly` disables the repo's own `.claude/settings.json` hook, so the
  managed hook above is now the *only* one — hence it is duplicated here.
- Multiple teams/machines can use the `managed-settings.d/` drop-in dir instead of one
  file (research/settings.md:L114-118).

2. Restart Claude Code. Verify with `claude doctor` (lists managed settings and any
   invalid entries — research/settings.md validation section) and `/hooks` (shows each
   hook's source; the guard should read **Managed**, not Project).

## Verify tamper-resistance

After deploy, from inside a session: setting `"disableAllHooks": true` in your *user*
settings must NOT disable the guard (only managed-level disable works —
research/hooks-reference.md:L594). If the guard still blocks a protected write with your
user `disableAllHooks` set, the lockdown is effective.

## Rollback

Delete `C:\Program Files\ClaudeCode\managed-settings.json` (or the HKLM `Settings` value)
and restart. The repo-scoped guard in `.claude/settings.json` resumes as the sole layer.
