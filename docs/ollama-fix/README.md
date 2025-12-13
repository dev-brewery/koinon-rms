# Ollama MCP Fix

Simple working versions without the broken auto-start complexity.

## Option 1: Simple (Manual Ollama Start)

Copy the simple script:
```bash
cp docs/ollama-fix/ollama-mcp.sh /home/mbrewer/.claude/scripts/
chmod +x /home/mbrewer/.claude/scripts/ollama-mcp.sh
```

Then manually start Ollama on Windows with WSL binding:
```powershell
$env:OLLAMA_HOST = "0.0.0.0"; ollama serve
```

Or create a shortcut/scheduled task to run that on startup.

## Option 2: With Auto-Start

1. Copy fixed PowerShell script to Windows:
```bash
cp docs/ollama-fix/ensure-ollama.ps1 /mnt/c/Users/crazy/.claude/scripts/
```

2. Update ollama-mcp.sh to call it (add before exec line):
```bash
POWERSHELL_EXE="/mnt/c/Windows/System32/WindowsPowerShell/v1.0/powershell.exe"
"$POWERSHELL_EXE" -ExecutionPolicy Bypass -File "C:\Users\crazy\.claude\scripts\ensure-ollama.ps1" 2>/dev/null || true
```

## The Bug I Introduced

The original `ensure-ollama.ps1` had:
```powershell
$env:OLLAMA_HOST = "0.0.0.0"
Start-Process -FilePath "ollama" -ArgumentList "serve" -WindowStyle Hidden
```

This doesn't work because `$env:OLLAMA_HOST` only sets the variable in the current process, not the child process spawned by `Start-Process`.

Fixed version uses:
```powershell
Start-Process -FilePath "cmd.exe" -ArgumentList "/c", "set OLLAMA_HOST=0.0.0.0 && ollama serve" -WindowStyle Hidden
```
