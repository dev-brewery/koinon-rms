# 0001. Record architecture decisions

Date: 2026-07-03
Status: Accepted

## Context

The founding architect left the project at the 2026 demo milestone. The
conventions existed (in `docs/reference/conventions.md` and in the code),
but the *reasons* lived in one person's head and in scattered review
documents that went stale. Junior developers and AI agents inherit rules
they can't distinguish from arbitrary preferences, which makes the rules
easy to "improve" away.

## Decision

Decisions that set precedent are recorded here as ADRs — Context, Decision,
Consequences, date, sequential numbers. The `chief-architect` agent records
one for every `NEW DECISION REQUIRED` ruling. Changes to
`docs/reference/conventions.md` require an ADR.

## Consequences

- The "why" survives personnel changes; a rule can be revisited by reading
  its ADR and deliberately superseding it, instead of being rediscovered
  through a regression.
- Enforcement: the `chief-architect` agent instructions require it;
  `conventions.md` states that changes to it go through an ADR.
