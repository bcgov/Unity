$ErrorActionPreference = 'Stop'

$composeFile = Join-Path $PSScriptRoot 'docker-compose.yml'
$composeFiles = @('-f', $composeFile)
$localComposeFile = Join-Path $PSScriptRoot 'docker-compose.override.yml'
if (Test-Path -PathType Leaf -LiteralPath $localComposeFile) {
    $composeFiles += @('-f', $localComposeFile)
}
docker compose --project-directory $PSScriptRoot @composeFiles down
if ($LASTEXITCODE -ne 0) {
    throw 'Docker Compose could not stop the AI application. Check the output above.'
}

$listener = Get-NetTCPConnection -LocalPort 18080 -State Listen -ErrorAction SilentlyContinue |
    Where-Object { $_.LocalAddress -in @('127.0.0.1', '0.0.0.0') } |
    Select-Object -First 1
if ($listener) {
    $owner = Get-CimInstance Win32_Process -Filter "ProcessId = $($listener.OwningProcess)"
    if ($owner.Name -eq 'oc.exe' -and
        $owner.CommandLine -match '\bce395f-dev\b.*\bport-forward\b.*svc/unity-azure-llm-proxy\b.*\b18080:80\b') {
        Stop-Process -Id $listener.OwningProcess
        Write-Host 'Stopped the dev AI proxy port-forward.'
    } else {
        Write-Warning 'Port 18080 belongs to another process; leaving it running.'
    }
}

Write-Host 'AI application stopped. The database volume was preserved.'
