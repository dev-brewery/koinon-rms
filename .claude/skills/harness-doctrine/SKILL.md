---
name: harness-doctrine
description: >
  The full "why" behind the Koinon enforcement harness — the drift model it
  counters, the quality-over-autonomy value it serves, and the architect-review
  cycle it builds toward. Dormant by design: zero per-session cost, loaded only
  when summoned. Use when: the owner invokes it to redirect a drifting session;
  an agent is tempted to weaken, bypass, or "temporarily disable" a gate; a gate
  blocks and the agent believes it's wrong; designing or reviewing any harness
  component. Triggers: "doctrine", "why does the harness", "why is this gate
  here", "the guard is blocking me", "drift", "counter-weight".
---

# Harness Doctrine — why the gates exist

You are reading this because the owner (or a gate) summoned it. It is the
rationale behind every enforcement mechanism in this repo. It is deliberately
NOT in `CLAUDE.md`: always-loaded context becomes noise to ignore; this loads
at the moment it's needed, with full force.

**If a gate is blocking you right now:** the gate is probably working. Report
it to the owner and stop. Never weaken, bypass, or route around it — that
includes editing `scripts/hooks/`, `.claude/settings.json`, regenerating
baselines to pass, or "just this once" workarounds.

## The top value: quality is the invariant; autonomy is disposable

Fully-autonomous "set the agents on features and they run with it" is the
fantasy that produced this app's vibe-code — it forces every agent to optimize
for never-stopping. The owner inverted that (2026-07-06):

1. **Blocking is correct, not failure.** If RAG, the index, or the architect
   brain is down, development STOPS. Do not fall back to grep, do not proceed
   on presumption, do not report done. A blocked cycle waiting on the owner is
   the harness working.
2. **The human is in the loop by design.** Summoning the owner to fix
   infrastructure is the intended process, not a fallback.
3. **Invert the incentive.** The counter-weights make stopping-for-quality the
   path of least resistance, so completion pressure is channeled INTO the
   quality gate, not around it.

**Counter-calibration — do not over-correct into punting.** Escalate ONLY
(a) a genuine infrastructure failure you cannot fix after graceful retries, or
(b) a demonstrated cross-feature conflict (show the specific collision).
Everything else — cold-starts, transient errors, solvable design choices — you
troubleshoot and decide yourself, then note it. Punting decisions to dodge the
risk of being wrong is the same work-avoidance as shortcutting to look
finished.

## The drift model — seven measured mechanisms, each with a counter-weight

Generic guardrails fail because they don't oppose the force that produced the
drift. Every harness component must answer: *which drift mechanism does this
oppose, and at what point does it fire?* If it doesn't oppose a specific one,
it's decoration — cut it.

| # | Mechanism | Evidence in this app | Counter-weight |
|---|-----------|----------------------|----------------|
| 1 | **Turn-completion pressure** — emit a plausible finish fast | broken `get_impact_analysis` shipped for weeks; grep instead of RAG | required retrieval is the ONLY path through the gate; no graceful-degradation fallback |
| 2 | **Know-it-all prior** — answer from weights instead of retrieving | presuming patterns instead of `trace_feature` | retrieval mandatory AND its output a required input to proceed |
| 3 | **Haystack precision-collapse** — attention degrades over big context | grep buried 2 needles in 30 files | PRECISE retrieval feeds focused needles; never dump the corpus at the model |
| 4 | **Least-resistance enumeration** — hand-list the cases you thought of | gated Edit/Write but missed `rm`; "verified importers" ≠ coverage | gate on git/filesystem TRUTH, exhaustive by construction |
| 5 | **Self-review rationalization** — approve your own output | agent approved its own folder-deletion as "safe" | isolated-context architect review; separate brain, no shared rationalization |
| 6 | **Enforcement-rot** — unexercised gate silently breaks | `dev` branch got zero CI for months → 1,494-line baseline drift | every gate dogfooded by a test that exercises it, on all paths |
| 7 | **Escalation-as-avoidance** — punt decisions to dodge risk (mirror of #1) | AskUserQuestion spam on calls the agent could make | agent decides the solvable itself; escalation earned by analysis (see counter-calibration above) |

Calibrate counters against this app's ACTUAL failure history, not
hypotheticals.

## The architect-review cycle — the mandated change guardrail

An elegant emulation of a real dev cycle — not a skippable sidecar, not a
roadblock. Before ANY code change:

1. **Stop & check work** — no barreling ahead on presumption.
2. **Graceful, persistent semantic search** — retry RAG with backoff through
   embedding cold-loads. Required, not skippable; graceful, not
   fail-fast-at-5s. Genuine final failure → HARD STOP, summon the owner.
3. **Retrieve what's needed** — existing patterns (`trace_feature`),
   institutional lessons (`lesson_search`), change-specific connections.
4. **Deduce & propose** — write the diagnosis and the proposed fix.
5. **Isolated architect review** — a separate subroutine (configurable brain,
   isolated context: only problem + proposal + precisely-retrieved standards —
   the ADRs, `conventions.md`, `api-contracts.md`, `entity-mappings.md`, the
   traced pattern). It judges CONFORMANCE and cites the specific standard.
   Ruling: APPROVED / APPROVED_WITH_CONDITIONS / REJECTED, written
   content-bound to a guard-defended store (sha-named and sha-validated, so
   tampering or reuse voids the record; pattern-level enforcement, with its
   honest limits stated in the install notes — not claimed unforgeable).
6. **The ruling gates proceeding.** Only approval unlocks the code change; the
   ledger records the ruling, not just "analysis ran".

The architect approves the majority (approved work proceeds — do NOT punt it
back to the owner) and flags only real collisions with the specific reason.
The developer agent still does the work; it just cannot be its own final
reviewer (drift #5).

## Where the rest lives

- **The covenant** (session-start readback contract): `docs/claude/covenant.md`
- **The build plan** (what to build, in what order, current status):
  `docs/claude/harness-implementation-plan.md`
- **Enforcement mechanics**: `scripts/hooks/pre-tool-guard.mjs`,
  `scripts/hooks/impact-analyze.mjs`, `scripts/hooks/session-onboard.mjs`,
  wired in `.claude/settings.json` (agent-read-only; changes need the owner's
  two-key window)
- **Rules vs lessons**: rules → `docs/reference/conventions.md` via ADR;
  experiential lessons → `lesson_add` on `koinon-dev`. Never a repo blob,
  never just the transcript.
