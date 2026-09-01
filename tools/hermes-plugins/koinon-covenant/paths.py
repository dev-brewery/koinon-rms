"""Shared path/command primitives for the koinon-covenant plugin.

No I/O beyond locating the repo root; kept import-light so every other module
can use it without cycles.
"""

from __future__ import annotations

import os
import re
import subprocess
from functools import lru_cache
from pathlib import Path

# Tools whose arguments can mutate the repo or the outside world.
GATED_TOOLS = frozenset({
    "terminal",
    "execute_code",
    "write_file",
    "patch",
    "skill_manage",
})

# Where a path-bearing tool keeps its target path.
PATH_ARGS = ("path", "file_path", "filename", "target")

# Shell separators: evaluate each segment on its own, so a destructive command
# chained after a benign prefix is still seen by every rule.
_SEP = re.compile(r"\s*(?:&&|\|\||;|\||\n)\s*")


def segments(command: str) -> list[str]:
    """Split a shell command into independently-evaluated segments."""
    if not command:
        return []
    return [s.strip() for s in _SEP.split(command) if s.strip()]


@lru_cache(maxsize=1)
def repo_root() -> Path:
    """Locate the Koinon repo. Env override wins; then git; then the clone name.

    No machine-specific literal is baked in beyond the conventional clone name
    (CLAUDE.md forbids absolute machine paths in committed config).
    """
    env = os.environ.get("KOINON_REPO_ROOT")
    if env:
        return Path(env).expanduser()
    for cwd in (os.environ.get("KOINON_CWD"), os.getcwd()):
        if not cwd:
            continue
        try:
            out = subprocess.run(
                ["git", "rev-parse", "--show-toplevel"],
                cwd=cwd, capture_output=True, text=True, timeout=10,
            )
            if out.returncode == 0 and out.stdout.strip():
                return Path(out.stdout.strip())
        except Exception:
            pass
    return Path.home() / "koinon-rms"


def in_repo(path: str) -> bool:
    """True when *path* resolves inside the Koinon repo."""
    if not path:
        return False
    try:
        p = Path(path).expanduser()
        if not p.is_absolute():
            p = repo_root() / p
        p.resolve().relative_to(repo_root().resolve())
        return True
    except Exception:
        return False


def rel_to_repo(path: str) -> str:
    """Repo-relative POSIX path, or '' when outside the repo."""
    try:
        p = Path(path).expanduser()
        if not p.is_absolute():
            p = repo_root() / p
        return p.resolve().relative_to(repo_root().resolve()).as_posix()
    except Exception:
        return ""


def tool_paths(tool_name: str, args: dict) -> list[str]:
    """Every path-like argument a tool call carries."""
    found = []
    for key in PATH_ARGS:
        val = args.get(key)
        if isinstance(val, str) and val.strip():
            found.append(val.strip())
    return found


def git_status_porcelain(cwd: Path | None = None) -> str | None:
    """`git status --porcelain -uall`, or None when git cannot be consulted."""
    try:
        out = subprocess.run(
            ["git", "status", "--porcelain", "-uall"],
            cwd=str(cwd or repo_root()), capture_output=True, text=True, timeout=15,
        )
        if out.returncode != 0:
            return None
        return out.stdout
    except Exception:
        return None
