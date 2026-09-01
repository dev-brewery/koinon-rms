"""Dev-cycle mandate retrieval.

ADR-COMPLIANT BY CONSTRUCTION (architect ruling b077b7d9 REJECTED the first
design; this is the revision):

* **ADR 0006** — committed markdown is the source of truth and any index is a
  rebuildable projection of it, never an authority. So the mandates are read
  from `docs/claude/dev-cycle-mandates.md` in the repo. That file is the
  authority, full stop. No retrieval service is consulted to establish what the
  rules ARE.
* **ADR 0005** — no localhost, and nothing depends on a per-machine store.
  There is no 127.0.0.1 default here and no implicit endpoint: a mandate store
  is used only when one is explicitly configured to a real host, and only ever
  as an optional accelerator layered on top of the committed file.
* **ADR 0006** — `koinon-standards` remains the single retrieval path for
  `docs/reference/*` and `docs/adr/*`. This module never mirrors those
  documents anywhere; duplicating them into a second store is exactly what the
  first design got rejected for.

Net effect: the mandates are always available and always canonical, with or
without any server running.
"""

from __future__ import annotations

import logging
import os
import re
import threading

from paths import repo_root

_log = logging.getLogger(__name__)

# The committed source of truth. Everything else is commentary.
MANDATE_DOC = ("docs", "claude", "dev-cycle-mandates.md")
COVENANT_DOC = ("docs", "claude", "covenant.md")

# Optional, explicitly-configured mandate store. No default endpoint: unset
# means "no store", which is a fully supported configuration (ADR 0005).
MANDATE_ROOT_TEMPLATE = "viking://user/{user}/resources/koinon/mandates"
_TIMEOUT_SECONDS = 20

_cache_lock = threading.Lock()
_digest_cache: dict[str, str] = {}


def endpoint() -> str:
    """Configured mandate-store endpoint, or '' when none is configured."""
    return (os.environ.get("OPENVIKING_ENDPOINT") or "").rstrip("/")


def store_configured() -> bool:
    return bool(endpoint())


def _api_key() -> str:
    return os.environ.get("OPENVIKING_API_KEY", "")


def _user() -> str:
    return os.environ.get("OPENVIKING_USER", "default")


def mandate_root() -> str:
    return MANDATE_ROOT_TEMPLATE.format(user=_user())


def _headers() -> dict:
    headers = {"Content-Type": "application/json"}
    key = _api_key()
    if key:
        headers["Authorization"] = f"Bearer {key}"
    else:
        headers["X-OpenViking-Account"] = os.environ.get("OPENVIKING_ACCOUNT", "default")
        headers["X-OpenViking-User"] = _user()
    agent = os.environ.get("OPENVIKING_AGENT")
    if agent:
        headers["X-OpenViking-Actor-Peer"] = agent
    return headers


def _post_raw(path: str, payload: dict) -> tuple[dict | None, str]:
    """POST returning (ok_body, error_text). Errors surface; they are not
    swallowed into an empty result that looks like 'nothing found'."""
    if not store_configured():
        return None, "no mandate store configured"
    try:
        import httpx

        resp = httpx.post(
            f"{endpoint()}{path}", json=payload, headers=_headers(), timeout=_TIMEOUT_SECONDS
        )
        body = resp.json()
        if body.get("status") == "ok":
            return body, ""
        err = body.get("error") or {}
        return None, f"{err.get('code', resp.status_code)}: {err.get('message', '')[:200]}"
    except Exception as exc:
        return None, f"{type(exc).__name__}: {exc}"


def _get(path: str, params: dict) -> dict | None:
    if not store_configured():
        return None
    try:
        import httpx

        resp = httpx.get(
            f"{endpoint()}{path}", params=params, headers=_headers(), timeout=_TIMEOUT_SECONDS
        )
        resp.raise_for_status()
        body = resp.json()
        return body if body.get("status") == "ok" else None
    except Exception:
        return None


def healthy() -> bool:
    if not store_configured():
        return False
    try:
        import httpx

        resp = httpx.get(f"{endpoint()}/health", timeout=5)
        return resp.status_code == 200 and resp.json().get("healthy") is True
    except Exception:
        return False


def search(query: str, limit: int = 5) -> list[dict]:
    """Optional enrichment search against the configured mandate store.

    NOTE: `max_tokens` is only valid with mode='context'; passing it with
    mode='list' is rejected as INVALID_ARGUMENT, and that rejection looks
    identical to "nothing found" unless it is logged. It is logged.
    """
    body, err = _post_raw(
        "/api/v1/search/search",
        {"query": query, "target_uri": mandate_root(), "limit": limit, "mode": "list"},
    )
    if err and err != "no mandate store configured":
        _log.warning("mandate search failed for %r: %s", query, err)
    if not body:
        return []
    result = body.get("result")
    if isinstance(result, list):
        return result
    if not isinstance(result, dict):
        return []
    hits: list[dict] = []
    for key in ("resources", "memories", "items", "results", "hits", "nodes"):
        value = result.get(key)
        if isinstance(value, list):
            hits.extend(h for h in value if isinstance(h, dict))
    hits.sort(key=lambda h: h.get("score") or 0, reverse=True)
    return hits[:limit]


def read(uri: str, raw: bool = False) -> str:
    body = _get("/api/v1/content/read", {"uri": uri, "raw": str(raw).lower()})
    if not body:
        return ""
    result = body.get("result")
    if isinstance(result, str):
        return result
    if isinstance(result, dict):
        for key in ("content", "text", "body", "abstract"):
            if isinstance(result.get(key), str):
                return result[key]
    return ""


def ensure_dir(uri: str) -> bool:
    body, err = _post_raw("/api/v1/fs/mkdir", {"uri": uri, "parents": True})
    return body is not None or "CONFLICT" in err or "ALREADY_EXISTS" in err


def write(uri: str, content: str, wait: bool = False) -> tuple[bool, str]:
    """Publish a PROJECTION of a committed document to the mandate store.

    Only ever called with content read from committed markdown. The store is a
    rebuildable projection; it is never written as an original source (ADR 0006).
    """
    payload = {"uri": uri, "content": content, "wait": wait}
    body, err = _post_raw("/api/v1/content/write", {**payload, "mode": "create"})
    if body is not None:
        return True, "created"
    if "ALREADY_EXISTS" in err:
        body, err2 = _post_raw("/api/v1/content/write", {**payload, "mode": "replace"})
        return (True, "replaced") if body is not None else (False, err2)
    return False, err


# ---------------------------------------------------------------------------
# The digest — read from the committed canon.
# ---------------------------------------------------------------------------
def _read_repo_doc(parts: tuple[str, ...]) -> str:
    path = repo_root().joinpath(*parts)
    try:
        return path.read_text(encoding="utf-8").strip()
    except Exception:
        return ""


_LAST_RESORT = """\
CARDINAL RULES (docs/claude/dev-cycle-mandates.md could not be read — these are
the rules the koinon-covenant plugin enforces regardless):
  - Never merge a PR, never approve one, never push to main/dev. PRs stop at
    open + CI green; the QA verdict, then the owner's merge, are the gates.
  - Never write the approval store, the hooks, or the signing keys.
  - Never discard uncommitted work; commit session work to a branch first.
  - Never edit repo code without a fresh APPROVED architect ruling.
  - Never call work verified or merge-ready without QA-instance evidence.
  - Quality is the invariant; if required infrastructure is down, HALT."""


def digest(force: bool = False) -> str:
    """The dev-cycle mandates, from the committed canon.

    Cached per process: the file changes on the order of weeks and a session
    must not re-read it every turn.
    """
    with _cache_lock:
        if not force and "digest" in _digest_cache:
            return _digest_cache["digest"]

    mandate = _read_repo_doc(MANDATE_DOC)
    if mandate:
        text = (
            "DEV-CYCLE MANDATES (source of truth: docs/claude/dev-cycle-mandates.md, "
            "committed canon — this is the authority for how work proceeds here)\n\n"
            + mandate
        )
        covenant = _read_repo_doc(COVENANT_DOC)
        if covenant:
            text += "\n\n--- docs/claude/covenant.md ---\n" + covenant
    else:
        _log.warning("committed mandate doc unreadable; using the built-in cardinal rules")
        text = _LAST_RESORT

    with _cache_lock:
        _digest_cache["digest"] = text
    return text


def architect_digest(force: bool = False) -> str:
    """Compact, canonical mandate context for an isolated change review.

    Session guidance needs the complete mandate and covenant documents.  The
    architect does not: it needs the cardinal value, the mechanically enforced
    rules, and the code invariants that bear on a proposal.  Extracting those
    statements from the committed mandate keeps this a rebuildable projection
    rather than a second policy source.
    """
    with _cache_lock:
        if not force and "architect" in _digest_cache:
            return _digest_cache["architect"]

    mandate = _read_repo_doc(MANDATE_DOC)
    if not mandate:
        text = _LAST_RESORT
    else:
        top_value = re.search(r"(?m)^\*\*(.+?)\*\*\s*$", mandate)
        rules = [
            match.group(1).strip()
            for match in re.finditer(
                r"(?m)^\|\s*R\d\s*\|\s*\*\*(.+?)\*\*.*\|", mandate
            )
        ]
        invariants_section = re.search(
            r"(?ms)^## 6\. Non-negotiable code invariants\s*\n(.*?)(?=^## |\Z)",
            mandate,
        )
        invariants = (
            re.findall(r"(?m)^-\s+(.+(?:\n  .+)*)", invariants_section.group(1))
            if invariants_section
            else []
        )
        lines = [
            "ARCHITECT MANDATE CONTEXT (compact projection of "
            "docs/claude/dev-cycle-mandates.md)",
            "",
            f"Top value: {top_value.group(1) if top_value else 'Quality is the invariant; autonomy is disposable.'}",
            "",
            "Mechanically enforced rules:",
            *(f"- {rule}" for rule in rules),
            "",
            "Non-negotiable code invariants:",
            *(f"- {item.strip()}" for item in invariants),
        ]
        text = "\n".join(lines).strip()

    with _cache_lock:
        _digest_cache["architect"] = text
    return text


def source() -> str:
    """Where the digest came from — always the committed canon when readable."""
    return "committed-canon" if _read_repo_doc(MANDATE_DOC) else "built-in-fallback"
