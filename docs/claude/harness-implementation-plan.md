# Harness Implementation Plan

Executable, in order. Each step is verified before the next. This effects the design in
the memory files ([[quality-over-autonomy]], [[drift-counterweight-design]],
[[architect-review-cycle]]) and finishes what this session started. Discipline is
non-negotiable: draft+test in scratchpad, install in a window, verify, re-arm.

## Already done + verified (this session; in the working tree, UNCOMMITTED)
- Fail-closed guard + native `permissions.deny` (`scripts/hooks/pre-tool-guard.mjs`, `.claude/settings.json`).
- `get_impact_analysis` repaired + permanent smoke test + CI step (`tools/mcp-koinon-dev/`).
- Graceful RAG embedding retry (`rag-client.ts`) — recovers cold-start, fails honestly.
- Hard-stop inversion (`impact-analyze.mjs`) — RAG required; RAG-down → halt (exit 3, no ledger).
- **Phase 0 onboarding gate (0.1–0.3 below) — DONE + wired + verified.** `session-onboard.mjs`
  installed, SessionStart hook in `.claude/settings.json`, value woven into `impact-analyze.mjs`
  + `pre-tool-guard.mjs` block messages. Live-injection confirms only on the NEXT session.

---

## Phase 0 — The onboarding gate (DO FIRST; it protects everything else)
The mechanism that forces the shared understanding into every session instead of leaving
it in the noise. Counters drift #1/#3 applied to the handoff itself.

0.1 `scripts/hooks/session-onboard.mjs` (NEW): on SessionStart, emit
   `{"hookSpecificOutput":{"hookEventName":"SessionStart","additionalContext": <text>}}`
   where `<text>` = the covenant (`docs/claude/covenant.md`) + the current build-state
   (`harness-build-state` memory / this plan's status) + the mandate: "Read the covenant
   and repeat it back to the owner before any code work."
0.2 Wire it: add a `SessionStart` hook to `.claude/settings.json` → `node scripts/hooks/session-onboard.mjs`.
0.3 Weave the core value into gate outputs the agent CANNOT avoid: append the
   quality-over-autonomy line ("quality is the invariant; if RAG/architect is down, STOP —
   do not degrade") to `impact-analyze.mjs`'s final message and to `pre-tool-guard.mjs`
   block messages. The mechanism that forces the analysis now also forces the philosophy.
- **Window:** yes (`.claude/settings.json` + `scripts/hooks/`). Two-key: `disableAllHooks`
  in user settings AND remove `Edit(/scripts/hooks/**)` deny line; restore + probe both after.
- **Verify:** start a fresh session; confirm the covenant is injected as context and the
  readback is demanded before code work. *(0.1–0.3 done this session; live-injection pending
  next session.)*

0.4 **`harness-doctrine` skill (NEW — the ON-DEMAND home for the "why").** A committed skill
   at `.claude/skills/harness-doctrine/` (like `koinon-conventions`): dormant, zero per-session
   cost, NOT auto-loaded. The OWNER invokes it (or tells the agent to) to redirect a drifting
   session — it loads the full rationale as context AT THAT MOMENT: the drift model
   (`drift-counterweight-design`), the values (`quality-over-autonomy`), the design
   (`architect-review-cycle`), and pointers to the covenant + this plan. This is the portable,
   on-demand redirect context the owner asked for — the deliberate OPPOSITE of bloating
   `CLAUDE.md` (which loads every session = more noise to ignore). `CLAUDE.md` stays lean; the
   SessionStart hook injects only the tight covenant; the deep why lives here, summoned only
   when needed. Skills are committed (`!.claude/skills/` in .gitignore) so it TRAVELS to
   another PC — one of the two things (with committing) that make the "why" portable.
- **Window: yes** — verified 2026-07-07: the blanket `Edit(/.claude/**)` deny in
  `.claude/settings.json` covers `.claude/skills/` too (the earlier "no window" claim was
  wrong). Owner must open the window to install; draft+validate in scratchpad first.
  **Verify:** invoking it loads the doctrine; it stays out of context until invoked.

## Phase 1 — Verify the standards are indexed (read-only; NO window)
The architect review is worthless if the standards aren't retrievable (drift #3 haystack).
1.1 Query `koinon-code` (8599 chunks) for `docs/reference/conventions.md`, `docs/adr/*`,
   `docs/reference/api-contracts.md`, `entity-mappings.md`. Use `rag_search` /
   inspect payloads (`path` field) via the koinon-dev MCP tools.
1.2 If missing: index them — `tools/rag/index-codebase.py` (or add a standards pass) into
   `koinon-code` or a dedicated `koinon-standards` collection. Verify chunk counts rise
   and the docs are retrievable by relevance.

## Phase 2 — The isolated architect-review subroutine (the heart)
2.1 `scripts/hooks/architect-review.mjs` (NEW): input = files + `--deduced` + `--proposed`.
   - Precisely retrieve (RAG + `trace_feature`): the specific ADR(s), conventions, contract,
     and existing pattern relevant to THIS change — focused needles, never the whole corpus.
   - Call the configurable architect brain — **owner-decided 2026-07-07: PRIMARY = an
     OpenAI-API request (fast, hosted); FALLBACK = local Qwen at the :4000 gateway, and
     the fallback MUST emit a visible notice to the user that it activated** (local can
     take ~5 min on complex requests). Both paths are OpenAI-compatible
     `/v1/chat/completions`; dials: `ARCHITECT_URL`/`ARCHITECT_MODEL` (primary),
     `ARCHITECT_FALLBACK_URL`/`ARCHITECT_FALLBACK_MODEL` (default
     `http://192.168.1.225:4000`, Qwen). Isolated context = ONLY problem + proposal +
     retrieved standards. Both down → HARD STOP (quality-over-autonomy).
   - Ruling: `APPROVED | APPROVED_WITH_CONDITIONS | REJECTED` + cited reasons (which ADR/
     contract/convention). Write a **content-bound** ruling to `.claude/approvals/<sha>.json`
     (sha = hash of the changed file-set + proposal). RAG/brain down → HARD STOP (quality-over-autonomy).
2.2 Guard integration (`pre-tool-guard.mjs`): PreToolUse blocks Edit/Write/commit on a code
   file until a fresh APPROVED ruling exists whose hash matches the current change. The
   approval store lives under `.claude/` — deny-protected from the developer agent; only
   `architect-review.mjs` (a sanctioned named-script invocation) writes it → unforgeable.
2.3 OPEN DECISIONS — **RESOLVED by the owner 2026-07-07**: (a) architect brain = OpenAI-API
   primary with local-Qwen fallback + visible fallback notice (see 2.1); (b) acceptance-
   criteria source = the **linked GitHub issue** (the architect reads criteria from the
   issue the change references — changes need a linked issue with real criteria).
- **Window:** yes. **Verify:** a proposal that violates a known ADR gets REJECTED with the
  citation; a conforming one gets APPROVED; a forged approval (developer writing the store)
  is blocked by the deny rule.

## Phase 3 — Precise standards-retrieval (drift #3 counter)
Sibling-disambiguated, focused retrieval feeding Phase 2 (the reference-trace precision
proven in scratchpad + RAG relevance). Make "hand the architect the needle" real.

## Phase 4 — Commit the verified harness to the branch, THROUGH the gate
Everything is uncommitted. Once the gate is complete, run the harness on itself
(impact-analyze → architect review → commit), so the branch carries it and the first real
exercise of the cycle is publishing the cycle.

---

## Enforcement-of-the-plan note
Do the phases in order; do not skip Phase 0 (without it, the next agent inherits the gates
without the why and routes around them). Verify each phase before the next. If any
infrastructure is down, HALT and summon the owner — do not degrade to finish the plan.
