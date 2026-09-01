"""Publish a PROJECTION of the committed mandate canon to the mandate store.

ADR 0006: committed markdown is the source of truth; an index is a rebuildable
projection of it, never an authority, and standards under `docs/reference/*` and
`docs/adr/*` belong to the `koinon-standards` collection — they are NOT mirrored
here. This script therefore publishes only the two process documents the plugin
itself owns, and only ever by reading them from the repo.

The plugin does not need this to work: `mandates.digest()` reads the committed
file directly. This is an optional accelerator for semantic recall.

Run:  python seed_mandates.py [--verify-only]
"""

from __future__ import annotations

import sys
from pathlib import Path

sys.path.insert(0, str(Path(__file__).resolve().parent))

import mandates as m  # noqa: E402
from paths import repo_root  # noqa: E402

# Process documents only. Repo standards live in koinon-standards (ADR 0006).
CORPUS = [
    ("dev-cycle-mandates.md", ("docs", "claude", "dev-cycle-mandates.md")),
    ("covenant.md", ("docs", "claude", "covenant.md")),
]

VERIFY = [
    ("may I merge a PR", "dev-cycle-mandates"),
    ("what evidence counts as verified", "dev-cycle-mandates"),
    ("what does the dev cycle mandate", "dev-cycle-mandates"),
]


def seed() -> int:
    if not m.store_configured():
        print("No mandate store configured (OPENVIKING_ENDPOINT unset).")
        print("This is a supported configuration: the plugin reads the committed")
        print("canon directly. Nothing to publish.")
        return 0
    root = m.mandate_root()
    print(f"Mandate store: {m.endpoint()}  healthy={m.healthy()}")
    print(f"Projection root: {root}")
    if not m.healthy():
        print("Store unhealthy — refusing to publish a partial projection.")
        return 3
    if not m.ensure_dir(root):
        print(f"Could not create {root}.")
        return 3

    ok = 0
    for name, parts in CORPUS:
        path = repo_root().joinpath(*parts)
        try:
            content = path.read_text(encoding="utf-8")
        except Exception as exc:
            print(f"  ! cannot read {path}: {exc}")
            continue
        header = (
            f"<!-- PROJECTION of {'/'.join(parts)} — the committed file is the\n"
            f"     source of truth (ADR 0006). Do not edit here. -->\n\n"
        )
        wrote, detail = m.write(f"{root}/{name}", header + content)
        print(f"  {'+' if wrote else '!'} {name:26} {detail}")
        ok += 1 if wrote else 0
    print(f"Published {ok}/{len(CORPUS)}.")
    return 0 if ok == len(CORPUS) else 1


def verify() -> int:
    print("\nDigest (the thing that actually matters):")
    digest = m.digest(force=True)
    print(f"  source: {m.source()}  ({len(digest)} chars)")
    if m.source() != "committed-canon":
        print("  FAIL  the committed mandate doc is unreadable")
        return 1
    print("  ok    read from committed canon — available with or without a store")

    if not m.store_configured():
        print("\nNo store configured; skipping retrieval checks (not a failure).")
        return 0

    print("\nOptional store retrieval:")
    soft_failures = 0
    for query, expect in VERIFY:
        hits = m.search(query, limit=5)
        uris = [h.get("uri", "") for h in hits]
        names = ", ".join(u.rsplit("/", 1)[-1] for u in uris) or "(nothing)"
        if expect and not any(expect in u for u in uris):
            print(f"  pending  {query!r} -> {names}")
            soft_failures += 1
        else:
            print(f"  ok       {query!r} -> {names}")
    if soft_failures:
        print(f"\n{soft_failures} query/queries not yet answered by the store.")
        print("Indexing is asynchronous; this does NOT affect the plugin, which")
        print("reads the committed canon. Re-run --verify-only later to confirm.")
    return 0


if __name__ == "__main__":
    code = 0 if "--verify-only" in sys.argv else seed()
    if code in (0, 1):
        code = verify() or code
    sys.exit(code)
