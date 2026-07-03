# 0004. Clean architecture layer dependencies

Date: 2026-07-03 (documents a decision in force since project start)
Status: Accepted

## Context

Church management systems live for decades and accrete integrations
(payments, SMS, printers, calendars). The classic failure mode is business
logic welded to infrastructure and controllers, making every provider swap
or UI rewrite a full-system rewrite.

## Decision

```
Koinon.Api → Koinon.Application → Koinon.Domain
                    ↑
        Koinon.Infrastructure (implements Application interfaces)
```

- Domain: entities + interfaces, zero dependencies.
- Application: services, DTOs (records), validators; depends only on Domain.
- Infrastructure: EF Core, Redis, external providers; referenced only at
  composition root.
- Api: controllers validate → delegate → shape the response. No business
  logic in controllers; no controller touching `DbContext`.

## Consequences

- A feature is a vertical slice through fixed layers (see
  `docs/reference/conventions.md`, "The feature slice") — more files per
  feature, but every feature has the same shape, which is what lets
  low-context contributors (and AI agents) copy a known-good exemplar.
- Enforcement: `LayerDependencyTests` in `tests/Koinon.ArchitectureTests`
  (NetArchTest) fails the build on any reference against the arrows.
  `koinon-dev` MCP `validate_dependencies` checks pre-flight.
