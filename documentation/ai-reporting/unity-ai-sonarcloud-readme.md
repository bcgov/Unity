# Unity-AI SonarCloud Guide

This document provides a guide for the Unity-AI SonarCloud, including setup, permissions, IDE integration, and CI/CD workflows.

---

## Table of Contents

1. [Overview](#overview)
2. [Initial Setup & Login](#initial-setup--login)
3. [Project Configuration](#project-configuration)
4. [IDE Integration](#ide-integration)
5. [CI/CD Integration](#cicd-integration)
6. [Branch & Pull Request Analysis](#branch--pull-request-analysis)
7. [Support Contacts](#support-contacts)

---

## Overview

**Unity-AI SonarCloud Project Details:**

- **Organization:** `bcgov-sonarcloud`
- **Project Key:** `bcgov_unity-ai`
- **Project Name:** Unity-AI
- **URL:** https://sonarcloud.io/project/overview?id=bcgov_unity-ai

**Technology Stack:**

- **Backend:** Python 3.13 (Flask)
- **Frontend:** Angular 21 with TypeScript 5.9, Node.js 20
- **Database:** PostgreSQL with pgvector extension
- **Deployment:** Docker containers on OpenShift

---

## Initial Setup & Login

### First-Time SonarCloud Access

1. **Navigate to SonarCloud:** https://sonarcloud.io

2. **GitHub SSO Login:**
   - Click "Log in with GitHub"
   - Authorize SonarCloud access to your GitHub account
   - Accept organization invitation for `bcgov-sonarcloud`

3. **Join Unity-AI Project:**
   - Navigate to: https://sonarcloud.io/project/overview?id=bcgov_unity-ai
   - Request access if not automatically granted

### Required Permissions After First Login

Once logged in, request the following permissions from the SonarCloud Unity Project administrators:

#### Core Permissions:

- **Users and Groups** - View team members and manage group memberships
- **Administer Issues** - Triage, assign, and resolve code issues
- **Administer Security Hotspots** - Review and manage security vulnerabilities
- **Administer Architecture** - Configure architecture rules and design constraints
- **Execute Analysis** - Trigger manual scans and view analysis results

#### Permission Request Process:

1. Contact SonarCloud Unity Project administrators
2. Provide your GitHub username

---

## Project Configuration

### Current SonarCloud Settings

**Source Code Paths:**

```properties
sonar.sources=applications/Unity.AI.Reporting.Backend/src,applications/Unity.AI.Reporting.Frontend/src
sonar.tests=applications/Unity.AI.Reporting.Backend/test_packages.py
```

**Language Configuration:**

- Python 3.13
- TypeScript/Node.js 20
- Java 17 (for SonarCloud scanner)

**Quality Gate:**

- **Coverage Requirement:** Disabled (`sonar.coverage.exclusions=**/*`)
- **Quality Gate Wait:** Enabled for CI feedback
- **Duplicated Lines:** Excluded for HTML/CSS/SCSS files

**Key Exclusions:**

```properties
# Build artifacts and dependencies
**/node_modules/**, **/dist/**, **/build/**

# Python artifacts
**/__pycache__/**, **/*.pyc

# Environment configurations
applications/Unity.AI.Reporting.Frontend/src/environments/**
```

---

## IDE Integration

### VS Code Integration

#### 1. Install SonarLint Extension

```bash
# Via VS Code marketplace
code --install-extension SonarSource.sonarlint-vscode
```

#### 2. Configure SonarCloud Connection

**Settings (Ctrl+,):**

```json
{
  "sonarlint.connectedMode.project": {
    "connectionId": "bcgov-sonarcloud",
    "projectKey": "bcgov_unity-ai"
  },
  "sonarlint.connectedMode.connections.sonarcloud": [
    {
      "connectionId": "bcgov-sonarcloud",
      "organizationKey": "bcgov-sonarcloud",
      "token": "YOUR_SONARCLOUD_TOKEN"
    }
  ]
}
```

#### 3. Generate Personal Token

1. Go to: https://sonarcloud.io/account/security/
2. Generate new token with name: `Unity-AI-SonarLint`
3. Copy token and paste in VS Code settings

#### 4. Set as a Favorite Project

1. Navigate to Unity-AI project in SonarCloud
2. Click ⭐ "Add to favorites"
3. Access via SonarCloud dashboard favorites section

---

## CI/CD Integration

### Current GitHub Actions Setup

**Workflow File:** `.github/workflows/sonarsource-scan.yml`

#### Trigger Strategy: CI-Based Analysis

**Automatic Triggers:**

```yaml
on:
  push:
    branches: [dev, test, main]
  pull_request:
    types: [opened, synchronize, reopened]
  workflow_dispatch:
```

**Automated GitHub Analysis:**

- ✅ **CI-Based:** Build integration, PR decoration

#### Analysis Steps

1. **Environment Setup:**
   - Java 17 (SonarCloud scanner)
   - Python 3.13 (Backend dependencies)
   - Node.js 20 (Frontend build)

2. **Build Process:**
   - Install Python dependencies: `pip install -r requirements.txt`
   - Install Node.js dependencies: `npm ci --no-audit --prefer-offline`
   - Build Angular frontend: `npm run build`

3. **SonarCloud Scan:**
   - Uses `SonarSource/sonarqube-scan-action@v7`
   - Requires `SONAR_TOKEN` secret
   - Automatic PR decoration enabled
   - Version handling: Uses `UAI_BUILD_VERSION` variable if set, otherwise removes version property

#### Required GitHub Secrets

- **`SONAR_TOKEN`:** SonarCloud project token
- **`GITHUB_TOKEN`:** Automatic GitHub token for PR comments

#### Required GitHub Environment Variables

- **`UAI_BUILD_VERSION`:** Project version set at runtime (optional - if not set, SonarCloud will use automatic versioning)

---

## Branch & Pull Request Analysis

### Branch Strategy Support

**Analyzed Branches:**

- `main` - Production releases
- `test` - Pre-production testing
- `dev` - Development integration

**Pull Request Analysis:**

- **Decoration:** Automatic comments on PRs with quality gate status
- **New Code Detection:** Focuses on changed lines in PR
- **Quality Gate:** Must pass for merge approval

### Quality Metrics Tracked

**Code Quality:**

- Bugs
- Vulnerabilities  
- Security Hotspots
- Code Smells
- Technical Debt

**Code Coverage:**

- **Status:** Currently disabled (`sonar.coverage.exclusions=**/*`)
- **Reason:** Simplified maintenance, focuses on other quality metrics

**Duplication:**

- Excludes HTML/CSS/SCSS template files
- Tracks duplicated code blocks in Python/TypeScript

---

Check SonarCloud project → Quality Gates → View conditions

## Support Contacts

**SonarCloud Administration:**

- Repository Administrators
- DevOps Team Lead

**Technical Issues:**

- [SonarSource Documentation](https://docs.sonarcloud.io/)
- [GitHub Issues](https://github.com/bcgov/unity-ai/issues)