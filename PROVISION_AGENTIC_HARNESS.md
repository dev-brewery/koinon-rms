# Provision the Agentic Harness

This document provisions a fresh machine to run Koinon RMS agentic development
through the committed harness. It covers checkout, runtime dependencies,
Claude Code hook enforcement, Codex architect review, RAG connectivity, and the
verification commands that must pass before bugfix work resumes.

The harness is intentionally strict:

- Code edits require current impact analysis.
- Code edits and commits require an approved architect ruling.
- RAG, standards retrieval, and the architect brain are required infrastructure.
- If those services are down, stop and fix provisioning. Do not bypass the gate.

## Shared Requirements

Use these versions or newer compatible versions:

- Git
- Node.js 20 or newer
- npm
- .NET SDK 8, matching `global.json` (`8.0.416`, roll-forward allowed)
- Docker Engine or Docker Desktop with Compose v2
- Python 3 for graph/RAG helper scripts
- Claude Code CLI
- Codex CLI available as `codex`
- GitHub CLI `gh`, authenticated if architect review should read issue criteria

The committed defaults assume the team inference server is reachable:

- Qdrant: `http://192.168.1.225:6333`
- Embeddings/model gateway: `http://192.168.1.225:4000`
- Collections: `koinon-code`, `koinon-lessons`, `koinon-standards`

Do not point committed config at localhost for RAG. Use `.env` only for
machine-local overrides.

## Linux Provisioning

The examples use `/opt/koinon/koinon-rms`. If you choose another directory,
replace every path in the commands and managed settings.

### 1. Install Host Packages

Ubuntu 22.04/24.04 example:

```bash
sudo apt-get update
sudo apt-get install -y git curl ca-certificates gnupg python3 python3-venv docker.io docker-compose-plugin gh
curl -fsSL https://deb.nodesource.com/setup_20.x | sudo -E bash -
sudo apt-get install -y nodejs dotnet-sdk-8.0
sudo usermod -aG docker "$USER"
```

If `dotnet-sdk-8.0` is not available from your distro packages, install the
Microsoft package feed for your Ubuntu release, then rerun
`sudo apt-get install -y dotnet-sdk-8.0`.

Log out and back in after `usermod` so Docker group membership applies. Then
confirm:

```bash
dotnet --version
node --version
npm --version
docker compose version
```

Install or update the Codex CLI:

```bash
npm install -g @openai/codex
codex --version
```

Install or update Claude Code:

```bash
npm install -g @anthropic-ai/claude-code
claude --version
```

### 2. Authenticate Tooling

Authenticate Codex through the Codex surface available to this host. ChatGPT
Plus/Pro access is not an OpenAI Platform API key.

```bash
codex login
codex login status
codex exec --ephemeral --sandbox read-only "Reply with OK only."
```

If the VM is headless, use the device-code option if available:

```bash
codex login --device-auth
codex login status
codex exec --ephemeral --sandbox read-only "Reply with OK only."
```

Authenticate GitHub CLI if issue criteria should be included in architect
review:

```bash
gh auth login
gh auth status
```

### 3. Check Out the Project

```bash
sudo mkdir -p /opt/koinon
sudo chown -R "$USER":"$USER" /opt/koinon
cd /opt/koinon
git clone https://github.com/dev-brewery/koinon-rms.git
cd /opt/koinon/koinon-rms
git switch handoff
git pull --ff-only origin handoff
git status --short --branch
```

Expected status shape:

```text
## handoff...origin/handoff
```

No modified or untracked files should be listed.

### 4. Install Project Dependencies

```bash
npm ci
npm --prefix src/web ci
npm --prefix tools/mcp-koinon-dev ci
npm run rag:install
cp .env.example .env
```

Keep the RAG defaults unless this host has an approved alternate inference
server:

```bash
grep -E '^(ARCHITECT_|RAG_|QDRANT_|EMBEDDINGS_)' .env
```

The architect review defaults should include:

```text
ARCHITECT_PROVIDER=codex
ARCHITECT_TIMEOUT_MS=600000
```

Do not add `ARCHITECT_API_KEY` for ChatGPT Plus/Pro access.

### 5. Install Managed Claude Code Enforcement

Create `/etc/claude-code/managed-settings.json` as root:

```bash
sudo mkdir -p /etc/claude-code
sudo tee /etc/claude-code/managed-settings.json >/dev/null <<'JSON'
{
  "permissions": {
    "deny": [
      "Agent",
      "Edit(/opt/koinon/koinon-rms/.claude/**)",
      "Edit(/opt/koinon/koinon-rms/.husky/**)",
      "Edit(/opt/koinon/koinon-rms/.github/workflows/**)",
      "Edit(/opt/koinon/koinon-rms/scripts/hooks/**)",
      "Edit(/opt/koinon/koinon-rms/docs/adr/**)",
      "Edit(/opt/koinon/koinon-rms/tools/graph/backend-graph.json)",
      "Edit(/opt/koinon/koinon-rms/tools/graph/frontend-graph.json)",
      "Edit(/opt/koinon/koinon-rms/tools/graph/graph-baseline.json)",
      "Read(/opt/koinon/koinon-rms/.env)",
      "Read(/opt/koinon/koinon-rms/.env.*)",
      "Read(/opt/koinon/koinon-rms/secrets/**)"
    ]
  },
  "hooks": {
    "PreToolUse": [
      {
        "matcher": "Bash|PowerShell|Edit|Write|NotebookEdit|mcp__postgres__.*",
        "hooks": [
          { "type": "command", "command": "node /opt/koinon/koinon-rms/scripts/hooks/pre-tool-guard.mjs" }
        ]
      }
    ],
    "SessionStart": [
      {
        "hooks": [
          { "type": "command", "command": "node /opt/koinon/koinon-rms/scripts/hooks/session-onboard.mjs" }
        ]
      }
    ]
  },
  "allowManagedHooksOnly": true,
  "allowManagedPermissionRulesOnly": true
}
JSON
```

If your clone is not `/opt/koinon/koinon-rms`, adjust every path before
writing the file.

Restart Claude Code after changing managed settings.
Then run a startup-only check from the repo root:

```bash
claude --init-only
```

### 6. Verify Linux Harness Readiness

Run from `/opt/koinon/koinon-rms`:

```bash
node --check scripts/hooks/pre-tool-guard.mjs
node --check scripts/hooks/architect-review.mjs
npm --prefix tools/mcp-koinon-dev test
node scripts/graph-drift.mjs
```

Verify the inference server:

```bash
curl -fsS http://192.168.1.225:6333/collections/koinon-code >/dev/null
curl -fsS http://192.168.1.225:6333/collections/koinon-lessons >/dev/null
curl -fsS http://192.168.1.225:6333/collections/koinon-standards >/dev/null
curl -fsS http://192.168.1.225:4000/v1/models >/dev/null
```

Verify standards retrieval without invoking the architect brain:

```bash
node scripts/hooks/architect-review.mjs \
  --retrieve-only \
  --files src/Koinon.Domain/Entities/Person.cs \
  --deduced "Provisioning check for architect standards retrieval." \
  --proposed "No code change; verify koinon-standards returns governing standards."
```

Expected result: JSON with non-empty `standards`. If it exits 3 or returns no
standards, the VM is not ready for agentic bugfix work.

Verify the guard blocks unapproved code edits by starting a Claude Code session
in the repo and attempting a harmless code-file edit. The expected result is a
block telling the agent to run impact analysis and architect review. Do not
weaken the guard to pass this check.

### 7. Optional Demo Stack Check

Only needed when the next bugfix requires a running app:

```bash
docker compose -f docker-compose.full.yml up -d --build
tools/qa/run-e2e-demo.sh
```

## Windows Provisioning

The examples use `C:\home\repos\koinon-rms`. If you choose another directory,
replace every path in the commands and managed settings.

Run PowerShell as Administrator for package installation and managed settings.

### 1. Install Host Packages

Install these with your normal Windows package manager:

- Git for Windows
- Node.js 20 or newer
- .NET 8 SDK
- Docker Desktop
- Python 3
- GitHub CLI

If you use `winget`, the base set is:

```powershell
winget install --id Git.Git -e
winget install --id OpenJS.NodeJS.LTS -e
winget install --id Microsoft.DotNet.SDK.8 -e
winget install --id Docker.DockerDesktop -e
winget install --id Python.Python.3.12 -e
winget install --id GitHub.cli -e
```

Install or update the Codex CLI:

```powershell
npm install -g @openai/codex
codex --version
```

Install or update Claude Code:

```powershell
npm install -g @anthropic-ai/claude-code
claude --version
```

Confirm the base toolchain:

```powershell
git --version
node --version
npm --version
dotnet --version
docker compose version
python --version
```

### 2. Authenticate Tooling

Authenticate Codex through the Codex surface available to this host. ChatGPT
Plus/Pro access is not an OpenAI Platform API key.

```powershell
codex login
codex login status
codex exec --ephemeral --sandbox read-only "Reply with OK only."
```

If browser login is not available, use device-code login if available:

```powershell
codex login --device-auth
codex login status
codex exec --ephemeral --sandbox read-only "Reply with OK only."
```

Authenticate GitHub CLI if issue criteria should be included in architect
review:

```powershell
gh auth login
gh auth status
```

### 3. Check Out the Project

```powershell
New-Item -ItemType Directory -Force C:\home\repos | Out-Null
Set-Location C:\home\repos
git clone https://github.com/dev-brewery/koinon-rms.git
Set-Location C:\home\repos\koinon-rms
git switch handoff
git pull --ff-only origin handoff
git status --short --branch
```

Expected status shape:

```text
## handoff...origin/handoff
```

No modified or untracked files should be listed.

### 4. Install Project Dependencies

```powershell
npm ci
npm --prefix src/web ci
npm --prefix tools/mcp-koinon-dev ci
npm run rag:install
Copy-Item .env.example .env
```

Keep the RAG defaults unless this host has an approved alternate inference
server:

```powershell
Select-String -Path .env -Pattern '^(ARCHITECT_|RAG_|QDRANT_|EMBEDDINGS_)'
```

The architect review defaults should include:

```text
ARCHITECT_PROVIDER=codex
ARCHITECT_TIMEOUT_MS=600000
```

Do not add `ARCHITECT_API_KEY` for ChatGPT Plus/Pro access.

### 5. Install Managed Claude Code Enforcement

Create `C:\Program Files\ClaudeCode\managed-settings.json` as Administrator:

```powershell
$dir = 'C:\Program Files\ClaudeCode'
New-Item -ItemType Directory -Force $dir | Out-Null
@'
{
  "permissions": {
    "deny": [
      "Agent",
      "Edit(//c/home/repos/koinon-rms/.claude/**)",
      "Edit(//c/home/repos/koinon-rms/.husky/**)",
      "Edit(//c/home/repos/koinon-rms/.github/workflows/**)",
      "Edit(//c/home/repos/koinon-rms/scripts/hooks/**)",
      "Edit(//c/home/repos/koinon-rms/docs/adr/**)",
      "Edit(//c/home/repos/koinon-rms/tools/graph/backend-graph.json)",
      "Edit(//c/home/repos/koinon-rms/tools/graph/frontend-graph.json)",
      "Edit(//c/home/repos/koinon-rms/tools/graph/graph-baseline.json)",
      "Read(//c/home/repos/koinon-rms/.env)",
      "Read(//c/home/repos/koinon-rms/.env.*)",
      "Read(//c/home/repos/koinon-rms/secrets/**)"
    ]
  },
  "hooks": {
    "PreToolUse": [
      {
        "matcher": "Bash|PowerShell|Edit|Write|NotebookEdit|mcp__postgres__.*",
        "hooks": [
          { "type": "command", "command": "node //c/home/repos/koinon-rms/scripts/hooks/pre-tool-guard.mjs" }
        ]
      }
    ],
    "SessionStart": [
      {
        "hooks": [
          { "type": "command", "command": "node //c/home/repos/koinon-rms/scripts/hooks/session-onboard.mjs" }
        ]
      }
    ]
  },
  "allowManagedHooksOnly": true,
  "allowManagedPermissionRulesOnly": true
}
'@ | Set-Content -LiteralPath (Join-Path $dir 'managed-settings.json') -Encoding utf8
```

If your clone is not `C:\home\repos\koinon-rms`, adjust every `//c/...` path
before writing the file. Claude Code normalizes Windows absolute paths as
POSIX-style paths, so `C:\home\repos\koinon-rms` becomes
`//c/home/repos/koinon-rms` in permission rules.

Restart Claude Code after changing managed settings.
Then run a startup-only check from the repo root:

```powershell
claude --init-only
```

### 6. Verify Windows Harness Readiness

Run from `C:\home\repos\koinon-rms`:

```powershell
node --check scripts/hooks/pre-tool-guard.mjs
node --check scripts/hooks/architect-review.mjs
npm --prefix tools/mcp-koinon-dev test
node scripts/graph-drift.mjs
```

Verify the inference server:

```powershell
Invoke-WebRequest -UseBasicParsing -TimeoutSec 15 http://192.168.1.225:6333/collections/koinon-code | Out-Null
Invoke-WebRequest -UseBasicParsing -TimeoutSec 15 http://192.168.1.225:6333/collections/koinon-lessons | Out-Null
Invoke-WebRequest -UseBasicParsing -TimeoutSec 15 http://192.168.1.225:6333/collections/koinon-standards | Out-Null
Invoke-WebRequest -UseBasicParsing -TimeoutSec 15 http://192.168.1.225:4000/v1/models | Out-Null
```

Verify standards retrieval without invoking the architect brain:

```powershell
node scripts/hooks/architect-review.mjs `
  --retrieve-only `
  --files src/Koinon.Domain/Entities/Person.cs `
  --deduced "Provisioning check for architect standards retrieval." `
  --proposed "No code change; verify koinon-standards returns governing standards."
```

Expected result: JSON with non-empty `standards`. If it exits 3 or returns no
standards, the machine is not ready for agentic bugfix work.

Verify the guard blocks unapproved code edits by starting a Claude Code session
in the repo and attempting a harmless code-file edit. The expected result is a
block telling the agent to run impact analysis and architect review. Do not
weaken the guard to pass this check.

### 7. Optional Demo Stack Check

Only needed when the next bugfix requires a running app:

```powershell
docker compose -f docker-compose.full.yml up -d --build
tools/qa/run-e2e-demo.ps1
```

## Ready-to-Work Checklist

Do not start alpha bugfix work until all items are true:

- `git status --short --branch` is clean on `handoff`.
- `claude --version` works.
- `codex --version` works.
- `codex login status` reports authenticated access.
- `gh auth status` works, or the team accepts missing GitHub issue criteria.
- `.env` exists and does not contain an `ARCHITECT_API_KEY` for Plus/Pro access.
- `node --check scripts/hooks/pre-tool-guard.mjs` passes.
- `node --check scripts/hooks/architect-review.mjs` passes.
- `npm --prefix tools/mcp-koinon-dev test` passes.
- `node scripts/graph-drift.mjs` passes.
- Qdrant and embeddings endpoints are reachable.
- `koinon-standards` retrieval returns non-empty standards.
- Claude Code has been restarted after managed settings were installed.
- The first Claude Code session receives the onboarding covenant.

When all checks pass, future agent sessions should pick up bugfix work through
the harness instead of bypassing it.
