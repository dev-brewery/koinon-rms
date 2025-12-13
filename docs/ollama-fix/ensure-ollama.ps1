# ensure-ollama.ps1 - Start Ollama with WSL access if not running
# Run manually or add to Windows startup if you want auto-start

$ErrorActionPreference = "SilentlyContinue"

# Check if already running
try {
    $response = Invoke-RestMethod -Uri "http://localhost:11434/api/tags" -TimeoutSec 2
    Write-Host "Ollama already running"
    exit 0
} catch {}

# Find ollama
$ollamaPath = Get-Command ollama -ErrorAction SilentlyContinue
if (-not $ollamaPath) {
    Write-Host "Ollama not found in PATH"
    exit 1
}

# Start with 0.0.0.0 binding for WSL access
# Using cmd.exe to properly pass environment variable to child process
Start-Process -FilePath "cmd.exe" -ArgumentList "/c", "set OLLAMA_HOST=0.0.0.0 && ollama serve" -WindowStyle Hidden

# Wait for ready
$attempts = 0
while ($attempts -lt 20) {
    Start-Sleep -Milliseconds 500
    try {
        Invoke-RestMethod -Uri "http://localhost:11434/api/tags" -TimeoutSec 1 | Out-Null
        Write-Host "Ollama started successfully"
        exit 0
    } catch {
        $attempts++
    }
}

Write-Host "Ollama failed to start"
exit 1
