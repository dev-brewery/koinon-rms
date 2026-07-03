# 0003. snake_case database naming via explicit configuration

Date: 2026-07-03 (documents a decision in force since project start)
Status: Accepted

## Context

PostgreSQL folds unquoted identifiers to lowercase; PascalCase columns force
quoted identifiers into every hand-written query and are hostile to the
Postgres ecosystem. EF Core's default is to name columns after C#
properties (PascalCase).

## Decision

Tables and columns are snake_case (`group_member`, `first_name`), `id` int
identity PKs, `{entity}_id` FKs. Mapping is done **explicitly** per entity
in a `*Configuration.cs` (`HasColumnName`/`ToTable`), not via a global
naming convention.

Explicit-per-entity was chosen because a global convention rewrite mid-project
churns the entire model snapshot and risks subtle renames; revisit
post-demo (tracked as a `technical-debt` issue — see Consequences).

## Consequences

- Every new entity requires a configuration class; forgetting it used to
  silently produce PascalCase columns.
- Enforcement: `SnakeCaseNamingTests` in `tests/Koinon.ArchitectureTests`
  builds the EF model and fails on any table or column that isn't
  `^[a-z][a-z0-9_]*$` — a missing configuration is now a build failure, not
  a silent schema wart.
- The migration to an EF model-finalizing convention (removing ~60
  hand-written mappings) is deliberate post-demo work; the test makes the
  interim safe.
