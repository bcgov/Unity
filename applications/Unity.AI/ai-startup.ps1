param(
    [switch]$UseConfiguredAzureEndpoint,
    [switch]$UseConfiguredJwtSecret
)

$ErrorActionPreference = 'Stop'

if (-not (Test-Path (Join-Path $PSScriptRoot '.env'))) {
    throw "Create $PSScriptRoot\.env from .env.example and configure it before starting the AI application."
}

$tenantConfig = Join-Path $PSScriptRoot 'Unity.AI.Reporting.Backend\src\tenant_config.local.json'
if (-not (Test-Path -PathType Leaf -LiteralPath $tenantConfig)) {
    throw "Create $tenantConfig with the Default Grants Program Metabase api_key before starting the AI application."
}

if (-not $UseConfiguredJwtSecret) {
    $grantManagerSettings = Join-Path $PSScriptRoot '..\Unity.GrantManager\src\Unity.GrantManager.Web\appsettings.Development.json'
    if (-not (Test-Path -PathType Leaf -LiteralPath $grantManagerSettings)) {
        throw "GrantManager development settings not found at $grantManagerSettings. Use -UseConfiguredJwtSecret to keep the AI .env value."
    }
    $grantManagerJwtSecret = (Get-Content -Raw -LiteralPath $grantManagerSettings | ConvertFrom-Json).ReportingAI.JWTSecret
    if ([string]::IsNullOrWhiteSpace($grantManagerJwtSecret)) {
        throw "GrantManager ReportingAI:JWTSecret is not configured. Use -UseConfiguredJwtSecret to keep the AI .env value."
    }
}

if (-not $UseConfiguredAzureEndpoint) {
    $proxyUrl = 'http://127.0.0.1:18080/health'
    $listener = Get-NetTCPConnection -LocalPort 18080 -State Listen -ErrorAction SilentlyContinue |
        Where-Object { $_.LocalAddress -in @('127.0.0.1', '0.0.0.0') } |
        Select-Object -First 1
    if ($listener) {
        $owner = Get-CimInstance Win32_Process -Filter "ProcessId = $($listener.OwningProcess)"
        if ($owner.Name -ne 'oc.exe' -or
            $owner.CommandLine -notmatch '\bce395f-dev\b.*\bport-forward\b.*svc/unity-azure-llm-proxy\b.*\b18080:80\b') {
            throw 'Port 18080 is in use by another process, not the ce395f-dev AI proxy forward.'
        }
        curl.exe --fail --silent --output NUL --max-time 3 $proxyUrl
        if ($LASTEXITCODE -ne 0) {
            throw 'The existing OpenShift AI proxy forward on localhost:18080 is not healthy.'
        }
    } else {
        $oc = Get-Command oc -ErrorAction SilentlyContinue
        if (-not $oc) {
            throw 'Install the OpenShift CLI (oc) to forward the dev AI proxy.'
        }
        oc whoami --request-timeout=5s 1>$null 2>$null
        if ($LASTEXITCODE -ne 0) {
            throw 'Log in with oc login --web before starting the dev AI proxy.'
        }

        $forward = Start-Process -FilePath $oc.Source -ArgumentList @(
            '-n', 'ce395f-dev', 'port-forward', 'svc/unity-azure-llm-proxy',
            '18080:80', '--address', '127.0.0.1'
        ) -WindowStyle Hidden -PassThru
        $forwardReady = $false
        for ($attempt = 0; $attempt -lt 15; $attempt++) {
            if ($forward.HasExited) {
                throw 'The OpenShift AI proxy port-forward exited. Check your oc login and access to ce395f-dev.'
            }
            Start-Sleep -Seconds 1
            curl.exe --fail --silent --output NUL --max-time 3 $proxyUrl
            if ($LASTEXITCODE -eq 0) {
                $forwardReady = $true
                break
            }
        }
        if (-not $forwardReady) {
            if (-not $forward.HasExited) {
                Stop-Process -Id $forward.Id
            }
            throw 'The OpenShift AI proxy did not become available on localhost:18080.'
        }
        Write-Host "Forwarding the dev AI proxy on localhost:18080 (PID $($forward.Id))."
    }
}

$composeFile = Join-Path $PSScriptRoot 'docker-compose.yml'
$composeFiles = @('-f', $composeFile)
$localComposeFile = Join-Path $PSScriptRoot 'docker-compose.override.yml'
if (Test-Path -PathType Leaf -LiteralPath $localComposeFile) {
    $composeFiles += @('-f', $localComposeFile)
}
$previousEndpoint = [Environment]::GetEnvironmentVariable('AZURE_OPENAI_ENDPOINT', 'Process')
$previousJwtSecret = [Environment]::GetEnvironmentVariable('JWT_SECRET', 'Process')
try {
    if (-not $UseConfiguredAzureEndpoint) {
        $env:AZURE_OPENAI_ENDPOINT = 'http://host.docker.internal:18080/'
    }
    if (-not $UseConfiguredJwtSecret) {
        $env:JWT_SECRET = $grantManagerJwtSecret
    }
    docker compose --project-directory $PSScriptRoot @composeFiles up --build -d
    $composeExitCode = $LASTEXITCODE
} finally {
    [Environment]::SetEnvironmentVariable('AZURE_OPENAI_ENDPOINT', $previousEndpoint, 'Process')
    [Environment]::SetEnvironmentVariable('JWT_SECRET', $previousJwtSecret, 'Process')
}
if ($composeExitCode -ne 0) {
    throw 'Docker Compose could not start the AI application. Check the output above.'
}

$url = 'http://localhost'
$ready = $false
$deadline = [DateTime]::UtcNow.AddMinutes(2)
while ([DateTime]::UtcNow -lt $deadline) {
    curl.exe --fail --silent --output NUL --max-time 3 "$url/health"
    if ($LASTEXITCODE -eq 0) {
        $ready = $true
        break
    }
    Start-Sleep -Seconds 2
}

if (-not $ready) {
    docker compose --project-directory $PSScriptRoot @composeFiles logs --tail 40 reporting
    throw "The AI application did not respond at $url/health within two minutes."
}

curl.exe --fail --silent --output NUL --max-time 3 "$url/ready"
if ($LASTEXITCODE -eq 0) {
    Write-Host "AI application is ready at $url"
} else {
    Write-Warning "The web app is running at $url, but AI queries are not ready. Check $url/ready and the reporting logs."
}
$composeFileArgs = ($composeFiles | ForEach-Object { if ($_ -eq '-f') { $_ } else { "`"$_`"" } }) -join ' '
Write-Host "To view logs: docker compose --project-directory `"$PSScriptRoot`" $composeFileArgs logs -f reporting"
Start-Process $url
