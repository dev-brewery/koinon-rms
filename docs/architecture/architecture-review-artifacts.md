# Architecture review artifacts

Koinon RMS does not rely on autonomous-agent prose to enforce architecture. A coding agent may use the local architect/RAG harness to reason, but GitHub CI only trusts deterministic, structured evidence committed with the PR.

## Required artifact

Any PR that changes code or protected harness/standards files must include exactly one current review artifact under:

```text
docs/architecture/reviews/*.json
```

The artifact is bound to the PR diff excluding review artifacts themselves. CI recomputes that diff hash and rejects stale, partial, unsigned, or non-approving reviews.

## Signature model

Review artifacts are signed with Ed25519. Trusted public keys live in:

```text
docs/architecture/signers/trusted-agent-signers.json
```

Private keys never belong in the repository. On this development machine the bootstrap private key is stored outside the repo and read only by the local signing step.

The signed payload is the canonical JSON form of the artifact with `agentSignature` omitted. The signature block records:

```json
{
  "keyId": "koinon-bootstrap-agent-2026-07",
  "algorithm": "ed25519",
  "publicKeySha256": "...",
  "signedPayloadSha256": "...",
  "signature": "base64..."
}
```

## CI verification

The GitHub-hosted workflow does not call private RAG/model endpoints. It verifies only:

1. a review artifact exists for gated changes;
2. the artifact schema is valid;
3. the artifact covers every changed code/protected file;
4. per-file SHA-256 hashes match the current PR contents;
5. `diffSha256` matches `git diff --binary base...head`, excluding `docs/architecture/reviews/**`;
6. the ruling is `APPROVED` with cited reasons and no unresolved conditions;
7. cited standards exist in the repo;
8. the Ed25519 signature verifies against a trusted public key.

This lets the local architect perform nuanced review while CI remains deterministic and network-independent.

## Gated file scope

The verifier requires artifacts for:

- C#, TypeScript/JavaScript, Python, shell, SQL, project/config files;
- `.github/workflows/**`;
- `.claude/**`;
- `.husky/**`;
- `scripts/hooks/**`;
- `scripts/architecture/**`;
- `docs/adr/**`;
- `docs/reference/**`;
- `tools/graph/**`;
- `tools/mcp-koinon-dev/**`.

Docs-only changes outside protected standards/harness paths do not require an artifact.
