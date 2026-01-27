# Unity GitHub Runners Deployment Script for PowerShell
# This script deploys GitHub self-hosted runners to OpenShift d18498-tools namespace

param(
    [string]$Namespace = "d18498-tools",
    [switch]$SkipConfirmation = $false
)

$ErrorActionPreference = "Stop"

$SCRIPT_DIR = Split-Path -Parent $MyInvocation.MyCommand.Definition

Write-Host "🚀 Deploying Unity GitHub Runners to OpenShift namespace: $Namespace" -ForegroundColor Green

# Check if namespace exists
try {
    $null = oc get namespace $Namespace 2>$null
} catch {
    Write-Host "❌ Namespace $Namespace does not exist. Please create it first or check your OpenShift connection." -ForegroundColor Red
    exit 1
}

# Switch to target namespace
Write-Host "📂 Switching to namespace: $Namespace" -ForegroundColor Cyan
oc project $Namespace

# Function to apply and verify resource
function Apply-Resource {
    param(
        [string]$File,
        [string]$ResourceType = "",
        [string]$ResourceName = ""
    )
    
    Write-Host "📄 Applying $File..." -ForegroundColor Yellow
    
    try {
        oc apply -f (Join-Path $SCRIPT_DIR $File)
        Write-Host "✅ $File applied successfully" -ForegroundColor Green
        
        # Verify resource was created (optional verification)
        if ($ResourceType -and $ResourceName) {
            try {
                $null = oc get $ResourceType $ResourceName 2>$null
                Write-Host "✅ $ResourceType/$ResourceName verified" -ForegroundColor Green
            } catch {
                Write-Host "⚠️  $ResourceType/$ResourceName not found after creation" -ForegroundColor Yellow
            }
        }
    } catch {
        Write-Host "❌ Failed to apply $File" -ForegroundColor Red
        Write-Host $_.Exception.Message -ForegroundColor Red
        exit 1
    }
    Write-Host ""
}

# Check for required secrets configuration
Write-Host "🔍 Checking secret configurations..." -ForegroundColor Cyan

$warnings = @()

# Check if secrets contain placeholder values
$githubSecretPath = Join-Path $SCRIPT_DIR "github-runner-secret.yaml"
$sonarSecretPath = Join-Path $SCRIPT_DIR "sonarqube-token-secret.yaml"

if ((Get-Content $githubSecretPath -Raw) -match "your_github_pat_here") {
    $warnings += "⚠️  WARNING: github-runner-secret.yaml contains placeholder token!"
    $warnings += "   Please update 'your_github_pat_here' with your actual GitHub Personal Access Token"
}

if ((Get-Content $sonarSecretPath -Raw) -match "your_sonarqube_token_here") {
    $warnings += "⚠️  WARNING: sonarqube-token-secret.yaml contains placeholder token!"
    $warnings += "   Please update 'your_sonarqube_token_here' with your actual SonarQube token"
}

if ($warnings.Count -gt 0) {
    foreach ($warning in $warnings) {
        Write-Host $warning -ForegroundColor Yellow
    }
    Write-Host ""
}

if (-not $SkipConfirmation) {
    $continue = Read-Host "Continue with deployment? (y/N)"
    if ($continue -notmatch "^[Yy]$") {
        Write-Host "Deployment cancelled." -ForegroundColor Yellow
        exit 0
    }
}

# Deploy resources in order
Write-Host "🛠️  Deploying resources..." -ForegroundColor Cyan

Apply-Resource "github-runner-rbac.yaml" "serviceaccount" "unity-github-runner"
Apply-Resource "github-runner-secret.yaml" "secret" "github-runner-token"
Apply-Resource "sonarqube-token-secret.yaml" "secret" "sonarqube-token"
Apply-Resource "github-runner-configmap.yaml" "configmap" "unity-github-runner-config"
Apply-Resource "github-runner-networkpolicy.yaml" "networkpolicy" "unity-github-runner-network-policy"
Apply-Resource "github-runner-deployment.yaml" "deployment" "unity-github-runner"

Write-Host "⏳ Waiting for deployment to be ready..." -ForegroundColor Cyan
try {
    $rolloutResult = oc rollout status deployment/unity-github-runner --timeout=300s
    Write-Host "✅ Deployment successful!" -ForegroundColor Green
} catch {
    Write-Host "❌ Deployment failed or timed out" -ForegroundColor Red
    Write-Host $_.Exception.Message -ForegroundColor Red
    exit 1
}

Write-Host ""
Write-Host "🔍 Verifying deployment..." -ForegroundColor Cyan

# Check pod status
try {
    $allPods = oc get pods -l app=unity-github-runner --no-headers 2>$null
    $pods = if ($allPods) { ($allPods | Measure-Object).Count } else { 0 }
    
    $runningPods = if ($allPods) { 
        ($allPods | Where-Object { $_ -match "Running" } | Measure-Object).Count 
    } else { 0 }

    Write-Host "📊 Pod Status:" -ForegroundColor Cyan
    Write-Host "   Total pods: $pods" -ForegroundColor White
    Write-Host "   Running pods: $runningPods" -ForegroundColor White

    if ($runningPods -gt 0) {
        Write-Host "✅ Runners are starting up!" -ForegroundColor Green
        
        Write-Host ""
        Write-Host "📋 Next Steps:" -ForegroundColor Cyan
        Write-Host "1. Monitor runner logs:" -ForegroundColor White
        Write-Host "   oc logs -l app=unity-github-runner -f" -ForegroundColor Gray
        Write-Host ""
        Write-Host "2. Check runner registration:" -ForegroundColor White
        Write-Host "   oc logs -l app=unity-github-runner | Select-String 'successfully added as a runner'" -ForegroundColor Gray
        Write-Host ""
        Write-Host "3. Verify runners in GitHub:" -ForegroundColor White
        Write-Host "   Go to: Repository → Settings → Actions → Runners" -ForegroundColor Gray
        Write-Host ""
        Write-Host "4. Test with a GitHub workflow using label: [self-hosted, unity-runners]" -ForegroundColor White
        
    } else {
        Write-Host "⚠️  No running pods found. Check logs for issues:" -ForegroundColor Yellow
        Write-Host "   oc logs -l app=unity-github-runner" -ForegroundColor Gray
        Write-Host "   oc describe pods -l app=unity-github-runner" -ForegroundColor Gray
    }
} catch {
    Write-Host "⚠️  Could not verify pod status: $($_.Exception.Message)" -ForegroundColor Yellow
}

Write-Host ""
Write-Host "🎉 GitHub Runners deployment completed!" -ForegroundColor Green
Write-Host "   Namespace: $Namespace" -ForegroundColor White
Write-Host "   Labels: unity-runners, openshift, self-hosted, dotnet" -ForegroundColor White

# Optional: Show quick status
Write-Host ""
Write-Host "📈 Quick Status Check:" -ForegroundColor Cyan
try {
    oc get pods,deployment,secrets,configmap -l app=unity-github-runner
} catch {
    Write-Host "Could not retrieve status information" -ForegroundColor Yellow
}