# Unity GitHub Self-Hosted Runners on OpenShift
## Option 1: Experimental Deployment Alongside Azure DevOps

This directory contains OpenShift configurations for deploying GitHub self-hosted runners as an **experiment alongside your current Azure DevOps pipeline** to validate GitHub Actions with SonarQube integration.

## Experiment Goals

- **Parallel Operation**: Run GitHub Actions alongside existing Azure DevOps
- **Validation**: Compare SonarQube analysis results between both pipelines  
- **Risk Mitigation**: No disruption to current production workflows
- **Proof of Concept**: Test GitHub Actions + OpenShift runners integration

## Files

| File | Description |
|------|-------------|
| `github-runner-deployment.yaml` | Main deployment configuration for GitHub runners |
| `github-runner-secret.yaml` | Secret for GitHub Personal Access Token |
| `sonarqube-token-secret.yaml` | Secret for SonarQube authentication token |
| `github-runner-rbac.yaml` | ServiceAccount, Role, and RoleBinding for runner permissions |
| `github-runner-configmap.yaml` | Configuration settings for runners and build environment |
| `github-runner-networkpolicy.yaml` | Network policies for secure communication |
| `option1-setup.ps1` | **Automated PowerShell deployment script** |
| `DEPLOYMENT-GUIDE.md` | Detailed troubleshooting and advanced configuration |

---

## Quick Start Deployment

### Step 1: Prepare Tokens

#### 1.1 Create GitHub Personal Access Token
```bash
# Navigate to: https://github.com/settings/personal-access-tokens/new
# Required permissions:
# - Repository access: Selected repositories → bcgov/Unity
# - Repository permissions: Actions (Write), Administration (Write), Metadata (Read)
```

#### 1.2 Get SonarQube Token
- **Option A**: Reuse existing token from Azure DevOps service connection
- **Option B**: Generate new token in SonarQube → Administration → Security → Users → Tokens

### Step 2: Configure Secret Files

```powershell
# Navigate to deployment directory
cd C:\opt\Repository\Unity\openshift\github-runners

# Edit secret files with actual tokens:
# 1. Open github-runner-secret.yaml
#    Replace 'your_github_pat_here' with your GitHub PAT
# 2. Open sonarqube-token-secret.yaml  
#    Replace 'your_sonarqube_token_here' with your SonarQube token
```

### Step 3: Configure GitHub Repository

```bash
# Set repository secrets and variables (use GitHub CLI or web interface)

# Using GitHub CLI:
gh secret set SONAR_TOKEN --body "your_sonarqube_token" --repo bcgov/Unity
gh variable set SONAR_HOST_URL --body "https://sonarqube.econ.gov.bc.ca/sonar" --repo bcgov/Unity

# Or via web interface:
# Go to: https://github.com/bcgov/Unity/settings/secrets/actions
# Secrets → Add: SONAR_TOKEN
# Variables → Add: SONAR_HOST_URL
```

### Step 4: Deploy to OpenShift

```powershell
# Verify OpenShift connection
oc project d18498-tools

# Run automated deployment script
.\option1-setup.ps1

# Script will:
# - Validate prerequisites
# - Deploy all 7 YAML files in correct order
# - Verify deployment success
# - Show monitoring commands
```

### Step 5: Test & Validate

```bash
# 1. Verify runners registered in GitHub
# Navigate to: https://github.com/bcgov/Unity/settings/actions/runners
# Should see: "openshift-runner-xxx" entries with "Idle" status

# 2. Create test branch and trigger workflow
git checkout -b experiment/github-actions-sonarqube
echo "# GitHub Actions SonarQube test" >> README.md
git add README.md
git commit -m "Test: Trigger experimental GitHub Actions workflow"
git push -u origin experiment/github-actions-sonarqube

# 3. Monitor execution
# GitHub Actions: https://github.com/bcgov/Unity/actions
# SonarQube: https://sonarqube.econ.gov.bc.ca/sonar/dashboard?id=UnityScanKey
```

---

## PowerShell Deployment Script Features

The `option1-setup.ps1` script provides:

```powershell
# Basic deployment
.\option1-setup.ps1

# Advanced usage
.\option1-setup.ps1 -Namespace "d18498-tools" -SkipConfirmation

# What the script does:
# 1. Validates OpenShift connection and namespace
# 2. Checks for placeholder tokens in secret files
# 3. Deploys all resources in correct dependency order
# 4. Monitors rollout status with timeout
# 5. Verifies pod health and runner registration
# 6. Provides next steps and monitoring commands
```

---

## SonarQube Integration - Azure DevOps vs GitHub Actions

### Azure DevOps Pipeline Equivalency

The GitHub Actions workflow provides **identical functionality** to your current Azure DevOps pipeline:

| Azure DevOps Task | GitHub Actions Equivalent | Purpose |
|-------------------|---------------------------|---------|
| `SonarQubePrepare@6` | `dotnet sonarscanner begin` | Initialize analysis with project config |
| Service connection: `...` | `secrets.SONAR_TOKEN` | Authentication to SonarQube |
| `projectKey: UnityScanKey` | `/k:"UnityScanKey"` | Same project in SonarQube |
| `sonar.qualitygate.wait=true` | `/d:sonar.qualitygate.wait=true` | Pipeline fails on quality gate failure |
| Built-in coverage collection | `--collect:"XPlat Code Coverage"` | Same code coverage metrics |
| `SonarQubeAnalyze@6` + `SonarQubePublish@6` | `dotnet sonarscanner end` | Complete analysis and publish |

### GitHub Workflow Configuration

The workflow file `.github/workflows/ci-sonarqube.yml` includes:
- **Full SonarQube analysis** with quality gate enforcement
- **Code coverage collection** matching Azure DevOps configuration  
- **Test result reporting** with artifact upload
- **Branch-aware analysis** for feature branches and PRs
- **Same exclusions** as current Azure pipeline

### Key Benefits
- **Same SonarQube Server**: `https://sonarqube.econ.gov.bc.ca/sonar`
- **Same Project**: `UnityScanKey` with identical quality gates
- **Same Coverage**: Identical test coverage and code analysis
- **Enhanced Features**: Automatic branch analysis, PR decoration

---

## Monitoring & Validation

### Quick Health Check Commands
```bash
# Check runner status
oc get pods -l app=unity-github-runner -n d18498-tools

# Monitor runner logs  
oc logs -l app=unity-github-runner -n d18498-tools -f

# Verify runner registration
oc logs -l app=unity-github-runner -n d18498-tools | grep "Successfully added as a runner"

# Check GitHub runners (web)
# https://github.com/bcgov/Unity/settings/actions/runners

# Monitor GitHub Actions (CLI)
gh run list --repo bcgov/Unity --workflow="CI with SonarQube Analysis"
```

### Success Criteria
Your experiment is successful when:
- Self-hosted runners appear in GitHub repository settings
- GitHub workflow executes without errors  
- SonarQube receives analysis from GitHub Actions
- Quality gate results match Azure DevOps pipeline
- Test coverage percentages are identical

---

## Troubleshooting

### Common Issues & Quick Fixes

#### Runner Not Registering
```bash
# Check GitHub token
oc get secret github-runner-token -o jsonpath='{.data.token}' | base64 -d

# Check pod events
oc describe pods -l app=unity-github-runner -n d18498-tools
```

#### SonarQube Connection Failed  
```bash
# Test connectivity from runner
oc exec -it $(oc get pods -l app=unity-github-runner -o jsonpath='{.items[0].metadata.name}') -- curl -I https://sonarqube.econ.gov.bc.ca/sonar

# Verify SonarQube token
oc get secret sonarqube-token -o jsonpath='{.data.token}' | base64 -d
```

#### Workflow Permission Issues
```bash
# Check service account permissions
oc auth can-i --list --as=system:serviceaccount:d18498-tools:unity-github-runner
```

---

## Advanced Configuration

For detailed troubleshooting, advanced configuration options, and comprehensive validation procedures, see:
- **`DEPLOYMENT-GUIDE.md`** - Complete step-by-step guide with detailed explanations
- **`.github/workflows/ci-sonarqube.yml`** - Complete GitHub Actions workflow
- **Azure DevOps comparison** - Side-by-side feature comparison

---

## Cleanup (If Needed)

```bash
# Remove all experimental resources
oc delete -f github-runner-deployment.yaml
oc delete -f github-runner-networkpolicy.yaml  
oc delete -f github-runner-configmap.yaml
oc delete -f sonarqube-token-secret.yaml
oc delete -f github-runner-secret.yaml
oc delete -f github-runner-rbac.yaml

# Remove GitHub configuration (optional)
gh secret remove SONAR_TOKEN --repo bcgov/Unity
gh variable remove SONAR_HOST_URL --repo bcgov/Unity
```

---

## Next Steps After Successful Experiment

1. **Extended Testing**: Run on multiple feature branches
2. **Performance Comparison**: Compare execution times with Azure DevOps  
3. **Team Training**: Share results with development team
4. **Production Planning**: Plan phased migration strategy
5. **Resource Scaling**: Adjust runner count for production workload

---

**Pro Tip**: Run this experiment in parallel with your Azure DevOps pipeline for risk-free validation!