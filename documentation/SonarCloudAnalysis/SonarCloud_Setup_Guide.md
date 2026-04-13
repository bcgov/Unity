
# Unity SonarCloud Setup Guide — Complete Implementation

This document provides comprehensive step-by-step instructions for implementing SonarCloud analysis for the Unity Grant Manager project, incorporating lessons learned from the full implementation process.

---

## Prerequisites

- GitHub repository with write access
- Repository connected to `bcgov-sonarcloud` organization
- Access to repository Settings → Secrets and variables
- Ability to add or modify GitHub Actions workflows

---

## Step 1 — Project Configuration in SonarCloud

### Verify Project Setup
1. Sign in to https://sonarcloud.io using GitHub SSO
2. Navigate to `bcgov-sonarcloud` organization
3. Confirm the `bcgov_Unity` project exists
4. Verify project configuration:
   - **Project Key:** `bcgov_Unity`
   - **Organization:** `bcgov-sonarcloud`
   - **GitHub App Integration:** Installed

### Configure Long-Lived Branches
1. Go to **Project Administration → Branches and Pull Requests**
2. Set **branch pattern** to: `(main|test|dev|dev2)`
3. This ensures proper analysis for your promotion flow: `dev` → `test` → `main`

### Analysis Method Selection
Choose between:

**Option A: Automatic Analysis (Recommended)**
- Zero maintenance (no workflows to update)
- Automatic scanning on all pushes
- Perfect for long-lived branch strategy
- Requires disabling CI-based analysis

**Option B: CI-based Analysis**
- Full control over build process
- Includes test coverage collection
- Requires token management and workflow maintenance

---

## Step 2 — Authentication Setup

### Personal Token Generation (CI-based Only)
If using CI-based analysis:

1. Go to https://sonarcloud.io/account/security
2. Generate new token:
   - **Name:** `Unity GitHub Actions`
   - **Expiration:** 90 days (standard for personal tokens)
3. **Important:** Personal tokens expire every 90 days requiring manual renewal

### GitHub Secret Configuration
1. Go to **Repository Settings → Secrets and variables → Actions**
2. Create or update repository secret:
   - **Name:** `SONAR_TOKEN`
   - **Value:** Your SonarCloud token

---

## Step 3 — Project Properties Configuration

Create `applications/Unity.GrantManager/sonar-project.properties`:

```properties
# SonarCloud configuration for Unity Grant Manager
sonar.projectKey=bcgov_Unity
sonar.organization=bcgov-sonarcloud
sonar.host.url=https://sonarcloud.io

# Project metadata
sonar.projectName=Unity
sonar.projectDescription=Grant management application for the Province of British Columbia

# Source code settings (relative to projectBaseDir)
sonar.sources=src,modules
sonar.tests=test

# Quality gate settings
sonar.qualitygate.wait=true

# Exclusions (from existing Azure SonarQube configuration)
sonar.exclusions=src/Unity.GrantManager.EntityFrameworkCore/Migrations/**,modules/Unity.Payments/src/Unity.Payments.Web/Pages/BatchPayments/Index.js,**/bin/**,**/obj/**,**/wwwroot/lib/**,**/*.Designer.cs,**/node_modules/**

# Test exclusions
sonar.test.exclusions=**/bin/**,**/obj/**

# Code coverage exclusions
sonar.coverage.exclusions=modules/Volo.BasicTheme/**,**/Migrations/**,**/*DbContext.cs,**/*EntityTypeConfiguration.cs,**/Program.cs,**/Startup.cs,**/*.Designer.cs,**/DbMigrator/**

# Code duplication exclusions
sonar.cpd.exclusions=**/*.aspx,**/*.aspx.designer.cs,**/*.cshtml,**/*.html,**/*.js

# Coverage report paths
sonar.cs.vscoveragexml.reportsPaths=**/TestResults/**/*.coveragexml,**/coverage.coveragexml
sonar.cs.vstest.reportsPaths=**/TestResults/**/*.trx

# SCM settings
sonar.scm.provider=git
```

---

## Step 4 — GitHub Actions Workflow (CI-based Analysis)

Create `.github/workflows/sonarsource-scan.yml`:

```yaml
name: SonarCloud Analysis

on:
  push:
    branches:
      - dev
      - test
      - main
  pull_request:
    types: [opened, synchronize, reopened]
  workflow_dispatch:

permissions:
  contents: read
  pull-requests: write
  checks: write
  security-events: write

env:
  UGM_BUILD_VERSION: ${{vars.UGM_BUILD_VERSION}}

jobs:
  sonarcloud:
    name: SonarCloud
    runs-on: ubuntu-latest
    steps:
      - name: Set up JDK 17
        uses: actions/setup-java@v5
        with:
          java-version: 17
          distribution: 'zulu'

      - uses: actions/checkout@v6
        with:
          fetch-depth: 0  # Shallow clones should be disabled for a better relevancy of analysis

      - name: Setup .NET
        uses: actions/setup-dotnet@v5
        with:
          dotnet-version: '9.0.x'

      - name: Cache SonarCloud packages
        uses: actions/cache@v5
        with:
          path: ~/.sonar/cache
          key: ${{ runner.os }}-sonar
          restore-keys: ${{ runner.os }}-sonar

      - name: Cache SonarCloud scanner
        id: cache-sonar-scanner
        uses: actions/cache@v5
        with:
          path: ./.sonar/scanner
          key: ${{ runner.os }}-sonar-scanner
          restore-keys: ${{ runner.os }}-sonar-scanner

      - name: Install SonarCloud scanner
        if: steps.cache-sonar-scanner.outputs.cache-hit != 'true'
        run: |
          dotnet tool install --global dotnet-sonarscanner

      - name: Set version for SonarCloud
        run: |
          if [ -z "${{ env.UGM_BUILD_VERSION }}" ]; then
            echo "BUILD_VERSION=1.0.0-dev" >> $GITHUB_ENV
          else
            echo "BUILD_VERSION=${{ env.UGM_BUILD_VERSION }}" >> $GITHUB_ENV
          fi

      - name: Restore dependencies
        working-directory: ./applications/Unity.GrantManager
        run: dotnet restore Unity.GrantManager.sln

      - name: Build solution
        working-directory: ./applications/Unity.GrantManager
        run: dotnet build Unity.GrantManager.sln --no-restore

      - name: Run tests with coverage
        working-directory: ./applications/Unity.GrantManager
        run: dotnet test Unity.GrantManager.sln --no-build --verbosity normal --collect:"XPlat Code Coverage" --results-directory ./TestResults/

      - name: SonarCloud Scan
        uses: SonarSource/sonarqube-scan-action@v7
        with:
          projectBaseDir: applications/Unity.GrantManager
        env:
          GITHUB_TOKEN: ${{ secrets.GITHUB_TOKEN }}
          SONAR_TOKEN: ${{ secrets.SONAR_TOKEN }}
```

---

## Step 5 — Verification and Testing

### Manual Workflow Testing
1. Push changes to feature branch
2. Create PR to `dev` branch
3. Monitor workflow execution in Actions tab
4. Verify SonarCloud analysis completes successfully

### Expected Results
✅ **Analysis completes** without authentication errors  
✅ **Quality Gate status** appears in PR checks  
✅ **PR decoration** shows SonarCloud findings  
✅ **Test coverage** included in analysis  
✅ **Version tracking** using `UGM_BUILD_VERSION`  

### Common Issues and Solutions

**Authentication Failure:**
- Verify `SONAR_TOKEN` secret contains valid token
- Check token hasn't expired (90-day limit for personal tokens)
- Ensure GitHub App is properly installed

**Configuration File Not Found:**
- Verify `sonar-project.properties` location: `applications/Unity.GrantManager/`
- Check `projectBaseDir` setting in workflow matches file location

**Long-Lived Branch Issues:**
- Verify branch pattern: `(main|test|dev|dev2)`
- Ensure branches are properly configured in SonarCloud

---

## Architecture Integration

### ABP Framework Compatibility
- **Entity Framework migrations** properly excluded
- **Generated code** (.Designer.cs) excluded from analysis
- **Test coverage** integrated with .NET 9.0 test execution
- **Multi-module structure** (src/, modules/, test/) properly mapped

### BC Gov Standards Compliance
- **GitHub App integration** (enterprise-approved)
- **Secure token management** via GitHub Secrets
- **Audit-compliant** with explicit permissions
- **Branch protection** strategy alignment

---

## Migration from Azure SonarQube

All existing exclusion patterns from Azure DevOps SonarQube configuration have been migrated:

- ✅ **Entity Framework migrations** excluded
- ✅ **Generated code** excluded  
- ✅ **Third-party libraries** excluded
- ✅ **Test coverage settings** preserved
- ✅ **Code duplication** rules maintained

---

## Troubleshooting

### Token Expiration
Personal tokens expire every 90 days. Symptoms:
- `HTTP 403 Forbidden` errors
- "Project not found" messages

**Solution:** Generate new token and update GitHub secret.

### Analysis Conflicts
Error: "CI analysis while Automatic Analysis is enabled"

**Solution:** Choose one analysis method:
- Disable Automatic Analysis for CI-based, OR
- Disable CI-based workflow for Automatic Analysis

### Permissions Issues
Workflow warnings about missing permissions.

**Solution:** Ensure workflow includes explicit permissions block (see workflow example above).

---

## Enterprise Considerations

### Token Management
- **Personal tokens** require 90-day renewal cycles
- **Service account tokens** (if available) provide longer expiration
- **GitHub App integration** eliminates most token management needs

### Scaling Considerations
- **Automatic Analysis** recommended for multiple repositories
- **CI-based analysis** suitable for specialized build requirements
- **Organization-level tokens** preferred for enterprise deployment

---

## Outcome

✅ **Full SonarCloud integration** with Unity Grant Manager  
✅ **Enterprise GitHub App** authentication  
✅ **Long-lived branch analysis** (dev/test/main)  
✅ **Test coverage collection** and reporting  
✅ **Security compliance** with explicit permissions  
✅ **Migration complete** from Azure SonarQube configuration  
✅ **Maintenance documentation** for ongoing operations
