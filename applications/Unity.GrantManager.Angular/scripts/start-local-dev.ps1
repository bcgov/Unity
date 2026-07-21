<#
.SYNOPSIS
    Starts the full local dev stack (backend, Angular, gateway) in separate windows,
    killing anything already bound to their ports first.

.DESCRIPTION
    1. Backend      -> https://localhost:44343
    2. Angular      -> https://localhost:4300 (serve-path /app/)
    3. Gateway      -> https://localhost:44342 (browse here)
#>

$ErrorActionPreference = 'Stop'

$angularDir = Split-Path -Parent $PSScriptRoot
$repoRoot   = Split-Path -Parent (Split-Path -Parent $angularDir)
$backendDir = Join-Path $repoRoot 'applications\Unity.GrantManager\src\Unity.GrantManager.Web'
$gatewayDir = Join-Path $angularDir 'local-dev-gateway'

$certPath = "$env:USERPROFILE\.dev-certs\localhost-cert.pem"
$keyPath  = "$env:USERPROFILE\.dev-certs\localhost-key.pem"

function Stop-PortListener {
    param([int]$Port)

    $conns = Get-NetTCPConnection -LocalPort $Port -ErrorAction SilentlyContinue
    if (-not $conns) {
        Write-Host "Port $Port is free."
        return
    }

    $pids = $conns | Select-Object -ExpandProperty OwningProcess -Unique
    foreach ($procId in $pids) {
        try {
            $procName = (Get-Process -Id $procId -ErrorAction SilentlyContinue).ProcessName
            Stop-Process -Id $procId -Force -ErrorAction Stop
            Write-Host "Killed PID $procId ($procName) listening on port $Port"
        } catch {
            Write-Warning "Could not kill PID $procId on port $Port : $_"
        }
    }
}

Write-Host "=== Freeing ports 44343, 4300, 44342 ===" -ForegroundColor Cyan
Stop-PortListener -Port 44343
Stop-PortListener -Port 4300
Stop-PortListener -Port 44342

Write-Host "`n=== Launching backend (44343) ===" -ForegroundColor Cyan
Start-Process powershell -ArgumentList @(
    '-NoExit', '-Command',
    "Set-Location '$backendDir'; dotnet run --urls https://localhost:44343"
)

Write-Host "=== Launching Angular (4300, serve-path /app/) ===" -ForegroundColor Cyan
Start-Process powershell -ArgumentList @(
    '-NoExit', '-Command',
    "Set-Location '$angularDir'; ng serve --configuration=development-app-path --port 4300 --serve-path=/app/ --ssl --ssl-cert `"$certPath`" --ssl-key `"$keyPath`""
)

Write-Host "=== Launching gateway (44342) ===" -ForegroundColor Cyan
Start-Process powershell -ArgumentList @(
    '-NoExit', '-Command',
    "Set-Location '$gatewayDir'; `$env:LOCAL_GATEWAY_CERT = '$certPath'; `$env:LOCAL_GATEWAY_KEY = '$keyPath'; npm start"
)

Write-Host "`nAll three processes launched in separate windows. Browse to https://localhost:44342" -ForegroundColor Green
