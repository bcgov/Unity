# Prompt the user to optionally login to OpenShift
Write-Host "Do you want to log in to OpenShift now? (y/n)" -ForegroundColor Green
$loginResponse = Read-Host
if ($loginResponse -match '^(y|yes)$') {
    try {
        oc login --web --server=https://api.silver.devops.gov.bc.ca:6443
    }
    catch {
        Write-Host "Login failed. Please check your connection and credentials." -ForegroundColor Red
        exit 1
    }
}

# Prompt user for environment selection
$validEnvironments = @("dev", "dev2", "test", "uat", "prod")
do {
    Write-Host "Enter environment (dev, dev2, test, uat, prod)" -ForegroundColor Green
    $environment = Read-Host
} while (-not ($validEnvironments -contains $environment))

# Configuration parameters (dynamically updated based on environment)
$NameSpace = "d18498-$environment"  # OpenShift project namespace
$ClusterName = "$environment-crunchy-postgres"
$LocalPort = 5436
$RemotePort = 5432
$ListenInterface = "localhost"
$RetrySeconds = 3

# Check if oc is installed
if (-not (Get-Command oc -ErrorAction SilentlyContinue)) {
    Write-Host "The OpenShift CLI (oc) is not installed or not available in PATH." -ForegroundColor Red
    exit 1
}

# Set the OpenShift project namespace
Write-Host "Setting OpenShift project to $NameSpace..." -ForegroundColor Cyan
try {
    oc project $NameSpace
}
catch {
    Write-Host "Error setting project: $_" -ForegroundColor Red
    Write-Host "Please ensure you're logged in to OpenShift before running this script." -ForegroundColor Yellow
    exit 1
}

# Function to get the current primary pod using selectors
function Get-PrimaryPod {
    $selector = "postgres-operator.crunchydata.com/cluster=$ClusterName,postgres-operator.crunchydata.com/role=master"
    try {
        $primaryPod = oc get pod -o name --selector=$selector
        if ([string]::IsNullOrEmpty($primaryPod)) {
            Write-Host "No primary pod found with selector: $selector" -ForegroundColor Yellow
            return $null
        }
        return ($primaryPod -replace "^pod/", "").Trim()
    }
    catch {
        Write-Host "Error getting primary pod: $_" -ForegroundColor Red
        return $null
    }
}

# Function to check if still logged in
function Test-OCLogin {
    try {
        $null = oc project $NameSpace 2>&1
        return $true
    }
    catch {
        Write-Host "OpenShift login has expired or cannot access namespace '$NameSpace'. Please run the script again to re-authenticate." -ForegroundColor Red
        return $false
    }
}

# Main connection loop
while ($true) {
    Write-Host ""
    $datetime = Get-Date -Format "yyyy-MM-dd HH:mm:ss K"
    Write-Host $datetime -ForegroundColor Cyan

    # Get the current primary pod
    $primaryPod = Get-PrimaryPod

    if ($primaryPod) {
        Write-Host "Connecting to primary pod: $primaryPod" -ForegroundColor Green

        # Forward the port
        try {
            oc port-forward --address $ListenInterface $primaryPod "${LocalPort}:${RemotePort}"
        }
        catch {
            Write-Host "Error occurred during port forwarding: $_" -ForegroundColor Red
        }
    }
    else {
        Write-Host "Unable to find primary PostgreSQL pod. Retrying in $RetrySeconds seconds..." -ForegroundColor Yellow
    }

    # Pause before retry
    Start-Sleep -Seconds $RetrySeconds

    # Check login status
    if (-not (Test-OCLogin)) {
        break
    }
}

Write-Host "Press any key to exit..." -ForegroundColor Yellow
$null = $Host.UI.RawUI.ReadKey("NoEcho,IncludeKeyDown")