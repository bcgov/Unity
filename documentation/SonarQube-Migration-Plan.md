# SonarQube Migration Planning: Azure DevOps Pipeline into GitHub Actions

## Decision Summary

Given the recent Azure DevOps pipeline performance improvements that reduced build duration from 15-25 minutes to 1-3 minutes through refactoring optimizations, the recommendation is to maintain the current Azure DevOps on-premise SonarQube implementation. This approach allows us to continue leveraging the proven workflow while avoiding migration complexity and preserving team productivity with the significantly improved build performance.

Looking to a future off-board from Azure to GitHub & OpenShift the team would pursue Option 1 (Self-hosted GitHub runners in OpenShift) or Option 2 (Enhanced OpenShift BuildConfig) as future migration paths when business requirements or strategic priorities necessitate moving away from Azure DevOps infrastructure. These options provide the optimal balance of maintaining SonarQube connectivity while transitioning to GitHub-based workflows or OpenShift-native Tekton CI/CD pipelines.

## Migration Options Summary

| Option | Architecture | Connectivity | Complexity | Control | Effort |
|--------|-------------|-------------|------------|---------|---------|
| **Current** | Azure DevOps on-premise agents | Azure to Internal SonarQube | Low | High | None |
| **Option 1** | Self-hosted GitHub runners in OpenShift | OpenShift to External SonarQube | Medium | Medium | Medium |
| **Option 2** | Enhanced OpenShift BuildConfig | OpenShift to External SonarQube | Medium | Medium | Medium |
| **Option 3** | Self-hosted SonarQube in OpenShift | All runners to New OpenShift SonarQube | High | High | High |

### Quick Recommendation Guide

**Stay with Current**
- Keep existing Azure DevOps pipeline infrastructure
- Maintain current SonarQube integration at `https://sonarqube.econ.gov.bc.ca/sonar`
- Avoid migration complexity and maintain proven workflow
- Continue with established team knowledge and processes

**Choose Option 1**
- Minimal disruption to existing GitHub workflows
- Keep using external SonarQube at `https://sonarqube.econ.gov.bc.ca/sonar`
- Balance between GitHub ecosystem benefits and OpenShift connectivity to on-premise servers

**Choose Option 2**
- Leverage existing OpenShift BuildConfigs and infrastructure  
- Unified pipeline execution within OpenShift
- Minimal external dependencies

**Choose Option 3**
- Complete independence from external SonarQube infrastructure
- Full control over SonarQube configuration and upgrades
- Enhanced security with internal-only code analysis

### Existing SonarQube Infrastructure Status

## Current State

### Azure DevOps Pipeline Components
- **Build Pipeline**: Unity Grant Manager v$(UGM_VERSION).$(Build.BuildId)
- **Key Features**:
  - .NET 9.0 build and test
  - SonarQube integration with `https://sonarqube.econ.gov.bc.ca/sonar`
  - Cypress test artifact generation
  - Variable management through TFS libraries
  - Branch-based versioning (dev|test|main)

### Network Constraints
- GitHub runners blocked by firewall from SonarQube
- OpenShift pods can reach SonarQube (confirmed via curl test)

### Existing GitHub Workflows
- PR validation with unit tests in GitHub Actions
- Parallel test execution strategy
- Teams notifications
- Test result aggregation and reporting

## Migration Options

### Option 1: Self-Hosted GitHub Runners in OpenShift (Recommended)

**Architecture**:
- Deploy GitHub runner pods in `d18498-tools` namespace
- Runners register with GitHub Actions
- Direct SonarQube connectivity maintained
- Leverage existing GitHub workflow ecosystem

**Key Components**:
- GitHub runner deployment with persistent storage in d18498-tools namespace
- Secret management for GitHub PAT and SonarQube token secure connections
- Network policies for SonarQube access controls

**Advantages**:
- Minimal disruption to existing GitHub workflows
- Native GitHub ecosystem integration
- Direct SonarQube connectivity OpenShift to "https://sonarqube.econ.gov.bc.ca/sonar"
- Scalable runner infrastructure in OpenShift

**Implementation Requirements**:
- OpenShift deployment manifests in GitOps repository
- GitHub PAT with runner registration permissions
- SonarQube token configuration
- Runner lifecycle management

### Option 2: Enhanced OpenShift BuildConfig Pipeline

**Architecture**:
- Extend existing OpenShift BuildConfigs
- GitHub webhook triggers
- Integrated build, test, and SonarQube analysis
- OpenShift-native CI/CD pipeline

**Key Components**:
- Enhanced BuildConfig templates
- Custom builder images with .NET + SonarQube tools
- GitHub webhook integration
- Pipeline orchestration within OpenShift

**Advantages**:
- Leverages existing OpenShift infrastructure
- Unified pipeline execution environment
- No external runner dependencies

**Implementation Requirements**:
- Custom builder image creation
- BuildConfig template modifications
- Webhook configuration
- Pipeline result reporting

### Option 3: Self-Hosted SonarQube in OpenShift

**ALREADY DEPLOYED** - SonarQube container is running in `d18498-tools`

**Architecture**:
- SonarQube Community Edition 25.9.0.112764 deployed in `d18498-tools` namespace
- PostgreSQL 11.14.0 database with 960Mi persistent storage
- StatefulSet deployment with health checks and monitoring
- SSL/TLS termination via OpenShift routes
- Route host conflict needs resolution for external access

**Key Components**:
- SonarQube deployment with persistent volumes
- PostgreSQL database deployment
- OpenShift route for external access
- Backup and restore procedures
- User management and project configuration
- Plugin management (if required)

**Advantages**:
- Full control over SonarQube instance and configuration
- Accessible from both GitHub Actions and OpenShift runners
- No dependency on external SonarQube infrastructure
- Can be configured for specific Unity project needs
- Upgrade and maintenance control
- Enhanced security with internal-only access

**Implementation Requirements**:
- SonarQube and PostgreSQL already deployed via Helm
- Persistent volume claims configured (PostgreSQL: 960Mi)
- Route configuration needs host conflict resolution
- Database initialization completed
- Backup strategy implementation needed
- Security configuration (users, permissions, tokens) needed
- Plugin installation (if needed)
- Resource allocation and monitoring configured

**Current Storage Allocation**:
- PostgreSQL data: 960Mi persistent volume (may need expansion)
- SonarQube data: EmptyDir volumes (needs persistent storage for production)
- Consider migrating to persistent volumes for SonarQube data/logs/extensions

**Current Network Configuration**:
- Internal service: `sonarqube-sonarqube:9000`
- External route: Host conflict at `dev-unity-sonarqube.apps.silver.devops.gov.bc.ca`
- SSL/TLS termination configured
- Health checks and monitoring configured

**Current Self-Hosted Instance in `d18498-tools`**:
- **Running**: SonarQube Community Edition 25.9.0.112764
- **Database**: PostgreSQL 11.14.0 with 960Mi persistent storage
- **URL**: `https://dev-unity-sonarqube.apps.silver.devops.gov.bc.ca`
- **Route Status**: Host claimed conflict (needs resolution)
- **Connectivity**: Accessible from OpenShift runners and GitHub self-hosted runners

**Current Configuration**:
- Deployed via Helm chart (sonarqube-2025.5.0)
- StatefulSet with 1 replica
- Resource limits: 800m CPU, 6144M memory
- Persistent storage for PostgreSQL data
- SSL/TLS termination via OpenShift route
- Monitoring and health checks configured