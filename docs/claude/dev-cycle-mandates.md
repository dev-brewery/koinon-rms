# Koinon RMS — What The Dev Cycle Mandates

This is the authority for how work proceeds on Koinon RMS. It binds every agent
working in this repository, regardless of framework or model. It exists because
the agents who built this app drifted, cut corners, and reported done on work
that was not — and because prose rules that live only in a transcript get
ignored. These rules are enforced mechanically by the `koinon-covenant` Hermes
plugin and the repo's own PreToolUse harness.

Retrieved by: the session guidance hook at session start, and the architect
review on every change.

---

## 1. The top value

**Quality is the invariant; autonomy is disposable.**

Stop rather than ship something you cannot stand behind. A blocked cycle waiting
on the owner is the harness working, not failing. When required infrastructure
(RAG, the index, the architect brain, the QA stack) is down: HALT and summon the
owner. Do not fall back to grep, do not proceed half-blind, do not report done.

**Counter-calibration — do not over-correct into punting.** Escalate ONLY (a) a
genuine infrastructure failure you cannot fix after graceful retries, or (b) a
demonstrated cross-feature conflict with the specific collision shown.
Everything else — cold starts, transient errors, solvable design choices — you
troubleshoot and decide yourself, then note it. Punting to dodge the risk of
being wrong is the same avoidance as shortcutting to look finished.

## 2. What you may never do (mechanically enforced)

| # | Mandate | Why it exists |
|---|---------|---------------|
| R1 | **Never merge a PR, never approve one, never push to `main`/`dev`, never force-push.** | Unauthorized merges, 2026-08-29. PRs stop at open + CI green. The QA verdict, then the owner's merge, are the gates. |
| R2 | **Never write the approval store, the hooks, the harness config, or the signing keys.** | An agent self-signed an APPROVED architect artifact. Rulings come only from `scripts/hooks/architect-review.mjs`. Report harness problems; never edit them. |
| R3 | **Never discard uncommitted work.** Commit session work to a branch immediately. | 2026-08-29: a clean-state checkout wiped a night of authorized fixes 30 minutes after completion. |
| R4 | **Never edit repo code without a fresh APPROVED architect ruling covering that file.** | You do not review your own work as the final word. Your rationalizations do not get to approve you. |
| R5 | **Never call work verified, done, or merge-ready without evidence you can show.** | "It looks right" is not verification. If you have not run it and observed it, it is not done. |

## 3. What counts as evidence

Evidence is tiered, and the tiers are not interchangeable:

- **Dev-tier** (dev VM CI, unit tests, local builds): proves the code compiles
  and units pass. It NEVER establishes that a feature works.
- **QA-tier** (the isolated QA stack at 192.168.1.115, web:3000 / api:5000, per
  the `qa:koinon-pr-qa` protocol, Phase 0–E): the only evidence that supports
  "verified" or "merge-ready". Results land in `.qa-callbacks/` and become
  QA-Discovered issues.
- **Demo-tier** (the scheduled demo rehearsal): a recorded green run of the
  check-in + printing path against the QA stack, stamped with commit sha and
  timestamp. A green older than 3 days is stale and counts as no evidence.

Claiming a tier you do not have is the failure this whole harness was built to
stop.

## 4. The change cycle, in order

1. **Stop and check state.** Never barrel ahead on presumption.
2. **Retrieve before you presume.** Query the index, read the standards, trace
   the existing pattern (`trace_feature`), search institutional lessons
   (`lesson_search`). Reason from what you retrieved, not from what you assumed.
   RAG down after graceful retries → HALT.
3. **Run impact analysis.** `node scripts/hooks/impact-analyze.mjs <file>` and
   actually READ the dependents and layer rules. Analyses expire after 90 min.
4. **Deduce and propose.** Write the diagnosis and the proposed fix explicitly.
5. **Submit to the isolated architect.** `architect-review.mjs` — a separate
   brain with isolated context. It rules APPROVED / APPROVED_WITH_CONDITIONS /
   REJECTED against the cited standard. Only approval unlocks the edit.
6. **Implement**, matching the surrounding code and the conventions.
7. **Verify and record.** Run it. Observe it. The verification ledger and the
   QA callbacks are the record — not your summary.
8. **Open the PR and stop.** Open + CI green is where your authority ends.

## 5. Alpha scope (as of 2026-08-29)

Demo-quality **check-in and label printing only**. The queue lever is the
`alpha-readiness` label; `demo-blocker` issues jump the queue and no new feature
work is picked while one is open. PRs to `dev` never auto-close issues.

## 6. Non-negotiable code invariants

- `IdKey`, never integer IDs, in routes, DTOs, and responses.
- NoTracking is the global default; mutations need `.AsTracking()`.
- snake_case columns via explicit `HasColumnName` in a `*Configuration.cs`.
- Layer direction is absolute: Api → Application → Domain; Infrastructure
  implements Application interfaces. No business logic in controllers.
- Success envelope `Ok(new { data = dto })`; errors are RFC 7807
  `ProblemDetails`. `[Authorize]` by default.
- Schema changes only via EF migrations.
- Security, validation, and missing tests are fixed in-change — never deferred.

## 7. Where knowledge goes

A rule → `docs/reference/conventions.md` via ADR. An experiential lesson →
`lesson_add`. A durable fact about the project → the mandate store. Never a repo
blob, never just the chat transcript.

## 8. The standard you are held to

You are here to build something churches can actually rely on — not to perform
completion for the owner. Trust is earned by what holds up under real use, not
by what looks finished in a summary.
