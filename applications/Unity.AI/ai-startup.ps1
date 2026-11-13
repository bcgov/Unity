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
    # Same precedence as GrantManager.Web: the gitignored appsettings.secrets.json overrides appsettings.Development.json.
    $grantManagerWeb = Join-Path $PSScriptRoot '..\Unity.GrantManager\src\Unity.GrantManager.Web'
    $grantManagerSettings = @('appsettings.secrets.json', 'appsettings.Development.json') |
        ForEach-Object { Join-Path $grantManagerWeb $_ } |
        Where-Object { Test-Path -PathType Leaf -LiteralPath $_ }
    if (-not $grantManagerSettings) {
        throw "GrantManager settings not found in $grantManagerWeb. Use -UseConfiguredJwtSecret to keep the AI .env value."
    }
    $grantManagerJwtSecret = $grantManagerSettings |
        ForEach-Object { (Get-Content -Raw -LiteralPath $_ | ConvertFrom-Json).ReportingAI.JWTSecret } |
        Where-Object { -not [string]::IsNullOrWhiteSpace($_) } |
        Select-Object -First 1
    if ([string]::IsNullOrWhiteSpace($grantManagerJwtSecret)) {
        throw "GrantManager ReportingAI:JWTSecret is not configured in appsettings.secrets.json or appsettings.Development.json. Use -UseConfiguredJwtSecret to keep the AI .env value."
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

# Database name should match the running OpenShift environments (ce395f-dev: dev and dev2).
$openShiftDbName = 'UnityAI'
$dbName = $openShiftDbName
$dbNameLine = Get-Content -LiteralPath (Join-Path $PSScriptRoot '.env') | Where-Object { $_ -match '^\s*DB_NAME\s*=' } | Select-Object -Last 1
if ($dbNameLine) {
    $dbName = ($dbNameLine -replace '^\s*DB_NAME\s*=\s*', '').Trim().Trim('"', "'")
}
if ($dbName -cne $openShiftDbName) {
    Write-Warning "DB_NAME in .env is '$dbName'; OpenShift dev/dev2 use '$openShiftDbName'. Update .env to keep local and OpenShift aligned."
}

$usesSharedUnityDb = (Test-Path -PathType Leaf -LiteralPath $localComposeFile) -and
    ((Get-Content -Raw -LiteralPath $localComposeFile) -match 'unity_local_common-network')
if ($usesSharedUnityDb) {
    # Shared mode: GrantManager's "unitydb" container hosts the AI database, like ce395f-dev.
    docker network inspect unity_local_common-network 1>$null 2>$null
    if ($LASTEXITCODE -ne 0) {
        throw "Docker network 'unity_local_common-network' not found. Start Unity.GrantManager first: docker compose up -d (in $PSScriptRoot\..\Unity.GrantManager)."
    }
    $dbState = docker inspect --format '{{.State.Status}}' unitydb 2>$null
    if ($dbState -ne 'running') {
        throw "Shared Postgres container 'unitydb' is not running. Start Unity.GrantManager first: docker compose up -d (in $PSScriptRoot\..\Unity.GrantManager)."
    }
    $dbExists = docker exec unitydb psql -U postgres -Atc "select 1 from pg_database where datname = '$dbName'" 2>$null
    if ($dbExists -ne '1') {
        throw "Database '$dbName' does not exist in 'unitydb'. Create it (owned by the .env DB_USER) before starting the AI application."
    }
} else {
    # Standalone mode publishes its own postgres. Only stop when its effective host port
    # (5432 by default, or e.g. 5435 from a local override) is already taken by something
    # other than this project's own postgres container.
    $composeConfig = docker compose --project-directory $PSScriptRoot @composeFiles config --format json | ConvertFrom-Json
    if ($LASTEXITCODE -ne 0) {
        throw 'Docker Compose could not read the AI compose configuration. Check the output above.'
    }
    $ownPostgres = docker compose --project-directory $PSScriptRoot @composeFiles ps -q --status running postgres 2>$null
    if (-not $ownPostgres) {
        $publishedPorts = @($composeConfig.services.postgres.ports | Where-Object { $_.published } | ForEach-Object { [int]$_.published })
        foreach ($port in $publishedPorts) {
            # Docker Desktop's published ports don't always show as Windows listeners, so ask Docker too.
            $holder = (docker ps --filter "publish=$port" --format '{{.Names}}' 2>$null | Select-Object -First 1)
            if (-not $holder -and (Get-NetTCPConnection -LocalPort $port -State Listen -ErrorAction SilentlyContinue)) {
                $holder = 'another process'
            }
            if ($holder) {
                throw "Port $port, published by the standalone AI postgres, is already in use by $holder. Copy docker-compose.override.yml.example to docker-compose.override.yml to share unitydb, publish AI postgres on another port (see README), or stop $holder."
            }
        }
    }
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
