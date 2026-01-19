# Unity AI Development Token Generator
param(
    [string]$UserId = "dev-user-123",
    [string]$Tenant = "default", 
    [bool]$IsAdmin = $false,
    [int]$ExpiresInMinutes = 1440 # 24 hours
)

Write-Host "Unity AI Development Token Generator" -ForegroundColor Green
Write-Host "=====================================" -ForegroundColor Green

# For development, let's create a JWT token using the secret from .env file
# This reads your actual JWT secret and creates a token with specified expiration

# Read JWT_SECRET from .env file - try multiple locations
$envFile = $null
$searchPaths = @(
    ".\.env",                    # Current directory (if running from applications folder)
    "..\\.env",                 # Parent directory (if running from documentation folder)
    (Join-Path $PSScriptRoot ".env"),           # Same folder as script
    (Join-Path $PSScriptRoot "..\\.env")        # Parent of script folder
)

foreach ($path in $searchPaths) {
    $resolvedPath = Resolve-Path $path -ErrorAction SilentlyContinue
    if ($resolvedPath -and (Test-Path $resolvedPath)) {
        $envFile = $resolvedPath
        break
    }
}

if (-not $envFile) {
    Write-Error "Cannot find .env file in any of these locations:"
    $searchPaths | ForEach-Object { 
        $resolved = Resolve-Path $_ -ErrorAction SilentlyContinue
        if ($resolved) {
            Write-Error "  - $resolved"
        } else {
            Write-Error "  - $_ (path not found)"
        }
    }
    Write-Error "Please run this script from the folder where the .env file is located."
    exit 1
}

$jwtSecret = $null
Get-Content $envFile | ForEach-Object {
    if ($_ -match '^JWT_SECRET="(.+)"$') {
        $jwtSecret = $matches[1]
    }
}

if (-not $jwtSecret) {
    Write-Error "JWT_SECRET not found in .env file"
    exit 1
}

Write-Host "Using JWT_SECRET from .env file" -ForegroundColor Green

# Pre-calculated components (to avoid PowerShell Base64 encoding issues)
$headerB64 = "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9"

# Create payload JSON manually for reliability
$now = [DateTimeOffset]::UtcNow
$iat = [long]$now.ToUnixTimeSeconds()
$exp = [long]$now.AddMinutes($ExpiresInMinutes).ToUnixTimeSeconds()
$jti = "$UserId" + "_" + $iat
$isAdminStr = $IsAdmin.ToString().ToLower()

$payloadJson = "{`"user_id`":`"$UserId`",`"tenant`":`"$Tenant`",`"is_it_admin`":$isAdminStr,`"iat`":$iat,`"exp`":$exp,`"jti`":`"$jti`"}"

# Manually encode payload to Base64URL
$payloadBytes = [System.Text.Encoding]::UTF8.GetBytes($payloadJson)
$payloadB64 = [Convert]::ToBase64String($payloadBytes).Replace('+', '-').Replace('/', '_').TrimEnd('=')

# Create signature
$dataToSign = "$headerB64.$payloadB64"
$secretBytes = [System.Text.Encoding]::UTF8.GetBytes($jwtSecret)
$dataBytes = [System.Text.Encoding]::UTF8.GetBytes($dataToSign)

$hmac = New-Object System.Security.Cryptography.HMACSHA256
$hmac.Key = $secretBytes
$hashBytes = $hmac.ComputeHash($dataBytes)
$signature = [Convert]::ToBase64String($hashBytes).Replace('+', '-').Replace('/', '_').TrimEnd('=')

# Assemble JWT
$jwt = "$headerB64.$payloadB64.$signature"
$bearerToken = "Bearer $jwt"

# Display results
Write-Host ""
Write-Host "Token Details:" -ForegroundColor Yellow
Write-Host "  User ID: $UserId"
Write-Host "  Tenant: $Tenant"  
Write-Host "  Is Admin: $IsAdmin"
Write-Host "  Expires: $($now.AddMinutes($ExpiresInMinutes).ToString('yyyy-MM-dd HH:mm:ss')) UTC"
Write-Host ""

Write-Host "Development URLs (copy and paste):" -ForegroundColor Yellow
$localUrl = "http://localhost/?token=$jwt"
$openshiftUrl = "https://dev-unity-ai-reporting-d18498-dev.apps.silver.devops.gov.bc.ca/?token=$jwt"
$openshiftUrl2 = "https://dev2-unity-ai-reporting-d18498-dev.apps.silver.devops.gov.bc.ca/?token=$jwt"

Write-Host "  Local:     $localUrl"
Write-Host "  OpenShift: $openshiftUrl"
Write-Host "  OpenShift2:$openshiftUrl2"
Write-Host ""

if ($IsAdmin) {
    Write-Host "Admin Test URLs:" -ForegroundColor Cyan
    Write-Host "  Local Admin:     http://localhost/admin?token=$jwt"
    Write-Host "  OpenShift Admin: https://dev-unity-ai-reporting-d18498-dev.apps.silver.devops.gov.bc.ca/admin?token=$jwt"
    Write-Host ""
}

Write-Host "Usage Examples:" -ForegroundColor Green
Write-Host "Regular User:"
Write-Host "  .\unity-ai-reporting-verify-devTokenURL.ps1 -UserId 'user@gov.bc.ca'"
Write-Host ""
Write-Host "Admin User:"
Write-Host "  .\unity-ai-reporting-verify-devTokenURL.ps1 -UserId 'admin@gov.bc.ca' -IsAdmin `$true"
Write-Host ""
Write-Host "Quick Testing (1 minute expiration):"
Write-Host "  .\unity-ai-reporting-verify-devTokenURL.ps1 -UserId 'user@gov.bc.ca' -ExpiresInMinutes 1"
Write-Host ""
Write-Host "Instructions:" -ForegroundColor Green
Write-Host "Local Testing:"
Write-Host "1. Start app: docker-compose up --build"
Write-Host "2. Use Local URL above"
Write-Host ""
Write-Host "OpenShift Testing:"  
Write-Host "1. Use OpenShift URL above"
Write-Host "2. Same token works on both environments"