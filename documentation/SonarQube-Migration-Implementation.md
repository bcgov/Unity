# SonarQube Migration Implementation Guide

This document provides detailed implementation steps and configurations for migrating from Azure DevOps to GitHub Actions with SonarQube integration.

## Implementation Planning

### Phase 1: Infrastructure Setup

**Option 1 Tasks**:
- Create OpenShift namespace resources
- Deploy GitHub runner pods
- Configure persistent storage
- Setup network policies
- Create GitHub PAT secret
- Register runners with GitHub

**Option 2 Tasks**:
- Create custom builder images
- Update BuildConfig templates
- Configure GitHub webhooks
- Setup pipeline orchestration

**Option 3 Tasks**:
- PostgreSQL database already deployed with persistent storage
- SonarQube application already deployed with StatefulSet
- Resolve OpenShift route host conflict for external access
- Configure SonarQube for Unity project (projects, quality gates)
- Configure user authentication and authorization for team access
- Setup backup and restore procedures for production use
- Test connectivity from GitHub runners and OpenShift runners
- Migrate existing project history from external SonarQube (if needed)

### Phase 2: SonarQube Integration

**Common Tasks**:
- Create SonarQube token in secret
- Configure project keys and settings
- Setup quality gate integration
- Test connectivity and authentication

**Option-Specific Tasks**:
- **Option 1**: Integrate SonarQube scanner in GitHub workflows
- **Option 2**: Add SonarQube steps to BuildConfig
- **Option 3**: Configure new SonarQube instance with Unity project settings

### Phase 3: Workflow Migration

**Build Pipeline Migration**:
- Migrate .NET build configuration
- Transfer environment variables and secrets
- Update versioning strategy
- Configure artifact publishing

**Test Pipeline Migration**:
- Migrate unit test execution
- Setup test result reporting
- Configure coverage collection
- Maintain parallel execution strategy

**Deployment Pipeline Migration**:
- Update OpenShift deployment triggers
- Migrate environment-specific configurations
- Setup promotion workflows
- Configure release management

### Phase 4: Validation and Testing

**Testing Strategy**:
- Feature branch testing
- Full pipeline validation
- Performance benchmarking
- Rollback procedure validation

**Validation Checklist**:
- SonarQube analysis results match current pipeline
- All environment deployments function correctly
- Test reporting maintains current functionality
- Teams notifications continue working
- Artifact generation and storage works properly

### Phase 5: Migration Execution

**Preparation**:
- Backup current Azure DevOps configurations
- Document rollback procedures
- Prepare communication plan
- Setup monitoring and alerting

**Migration Steps**:
- Deploy new infrastructure
- Configure GitHub workflows/OpenShift jobs
- Update repository settings
- Test with non-production branches
- Migrate production workflows
- Decommission Azure DevOps pipelines

## Risk Mitigation

### Technical Risks
- **SonarQube connectivity issues**: Pre-validate network access from OpenShift
- **GitHub runner reliability**: Implement runner health monitoring
- **Performance degradation**: Establish baseline metrics before migration
- **Secret management**: Use OpenShift native secret management

### Operational Risks
- **Knowledge transfer**: Document all configurations and procedures
- **Rollback complexity**: Maintain Azure DevOps pipeline during transition
- **Dependency conflicts**: Test all integrations thoroughly
- **Deployment failures**: Implement staged rollout approach

## Post-Migration Considerations

### Monitoring and Maintenance
- GitHub runner health and capacity monitoring
- SonarQube integration status tracking
- Pipeline performance metrics
- Cost optimization opportunities

### Documentation Updates 
- Create troubleshooting guides
- Document new workflow procedures
- Update team training materials

### Continuous Improvement
- Optimize runner resource allocation
- Enhance pipeline parallelization
- Implement advanced caching strategies
- Explore additional automation opportunities

## Current Deployment Status for Option 3 (Self-Hosted SonarQube)

### Already Deployed Resources

**SonarQube StatefulSet**: `sonarqube-sonarqube`
- Image: `sonarqube:25.9.0.112764-community`  
- Resources: 800m CPU limit, 6144M memory limit
- Health checks: Liveness, readiness, and startup probes configured
- Volumes: EmptyDir for data, logs, extensions, temp

**PostgreSQL StatefulSet**: `sonarqube-postgresql-0` 
- Image: `bitnamilegacy/postgresql:11.14.0`
- Database: `sonarDB`, User: `sonarUser`
- Persistent storage: 960Mi (`data-sonarqube-postgresql-0`)
- Resources: 1 CPU limit, 1Gi memory limit

**Services**:
- `sonarqube-sonarqube`: ClusterIP 10.98.250.188:9000
- `sonarqube-postgresql`: Internal database connectivity

**Route (Needs Fix)**:
- Name: `sonarqube-sonarqube`  
- Host: `dev-unity-sonarqube.apps.silver.devops.gov.bc.ca` (conflicted)
- TLS: Edge termination configured

**Secrets**:
- `sonarqube-admin-password`: Admin credentials
- `sonarqube-postgresql`: Database credentials  
- `sonarqube-sonarqube-monitoring-passcode`: System monitoring
- `sonarqube-sonarqube-http-proxies`: Proxy configuration

### Required Updates for Production Use

## Deployment Templates for Option 3 (Updated Configurations)

### PostgreSQL Database Deployment
```yaml
# openshift/sonarqube-postgresql.yaml
apiVersion: v1
kind: DeploymentConfig
metadata:
  name: sonarqube-postgresql
  namespace: d18498-tools
spec:
  replicas: 1
  template:
    spec:
      containers:
      - name: postgresql
        image: registry.redhat.io/rhel8/postgresql-13
        env:
        - name: POSTGRESQL_DATABASE
          value: sonarqube
        - name: POSTGRESQL_USER
          valueFrom:
            secretKeyRef:
              name: sonarqube-db-secret
              key: username
        - name: POSTGRESQL_PASSWORD
          valueFrom:
            secretKeyRef:
              name: sonarqube-db-secret
              key: password
        volumeMounts:
        - name: postgresql-data
          mountPath: /var/lib/pgsql/data
      volumes:
      - name: postgresql-data
        persistentVolumeClaim:
          claimName: sonarqube-postgresql-pvc
```

### SonarQube Application Deployment  
```yaml
# openshift/sonarqube-app.yaml
apiVersion: v1
kind: DeploymentConfig
metadata:
  name: sonarqube
  namespace: d18498-tools
spec:
  replicas: 1
  template:
    spec:
      containers:
      - name: sonarqube
        image: sonarqube:community
        env:
        - name: SONAR_JDBC_URL
          value: jdbc:postgresql://sonarqube-postgresql:5432/sonarqube
        - name: SONAR_JDBC_USERNAME
          valueFrom:
            secretKeyRef:
              name: sonarqube-db-secret
              key: username
        - name: SONAR_JDBC_PASSWORD
          valueFrom:
            secretKeyRef:
              name: sonarqube-db-secret
              key: password
        volumeMounts:
        - name: sonarqube-data
          mountPath: /opt/sonarqube/data
        - name: sonarqube-logs
          mountPath: /opt/sonarqube/logs
        - name: sonarqube-extensions
          mountPath: /opt/sonarqube/extensions
      volumes:
      - name: sonarqube-data
        persistentVolumeClaim:
          claimName: sonarqube-data-pvc
      - name: sonarqube-logs
        persistentVolumeClaim:
          claimName: sonarqube-logs-pvc
      - name: sonarqube-extensions
        persistentVolumeClaim:
          claimName: sonarqube-extensions-pvc
```

### OpenShift Route Configuration
```yaml
# openshift/sonarqube-route.yaml
apiVersion: route.openshift.io/v1
kind: Route
metadata:
  name: sonarqube
  namespace: d18498-tools
spec:
  host: sonarqube-d18498-tools.apps.silver.devops.gov.bc.ca
  to:
    kind: Service
    name: sonarqube
  port:
    targetPort: 9000
  tls:
    termination: edge
    insecureEdgeTerminationPolicy: Redirect
```

## Option 1: Self-Hosted GitHub Runners Implementation

### Step 1: Create GitHub Personal Access Token

1. Go to GitHub → Settings → Developer settings → Personal access tokens → Fine-grained tokens
2. Create token with permissions:
   - Repository access: Selected repositories (Unity)
   - Repository permissions: Actions (Write), Administration (Write), Metadata (Read)
3. Save token securely

### Step 2: Create OpenShift Secret for GitHub Token

```bash
# Create secret with GitHub PAT
oc create secret generic github-runner-token \
  --from-literal=token='your_github_pat_here' \
  -n d18498-tools
```

### Step 3: Deploy GitHub Self-Hosted Runner
```yaml
# openshift/github-runners/github-runner-deployment.yaml
apiVersion: apps/v1
kind: Deployment
metadata:
  name: unity-github-runner
  namespace: d18498-tools
spec:
  replicas: 2
  selector:
    matchLabels:
      app: unity-github-runner
  template:
    metadata:
      labels:
        app: unity-github-runner
    spec:
      containers:
      - name: runner
        image: myoung34/github-runner:latest
        env:
        - name: REPO_URL
          value: "https://github.com/bcgov/Unity"
        - name: ACCESS_TOKEN
          valueFrom:
            secretKeyRef:
              name: github-runner-token
              key: token
        - name: RUNNER_NAME_PREFIX
          value: "openshift-runner"
        - name: RUNNER_WORKDIR
          value: "/tmp/runner/work"
        - name: RUNNER_GROUP
          value: "unity-runners"
        volumeMounts:
        - name: runner-data
          mountPath: /tmp/runner
        resources:
          requests:
            cpu: "1"
            memory: "2Gi"
          limits:
            cpu: "4" 
            memory: "8Gi"
      volumes:
      - name: runner-data
        emptyDir: {}
```

### Step 4: Deploy the Runner
```bash
# Apply the deployment
oc apply -f openshift/github-runners/github-runner-deployment.yaml -n d18498-tools

# Verify deployment
oc get pods -l app=unity-github-runner -n d18498-tools
oc logs -l app=unity-github-runner -n d18498-tools
```

### Step 5: Create SonarQube Token Secret
```bash
# Create SonarQube token secret
oc create secret generic sonarqube-token \
  --from-literal=token='your_sonarqube_token_here' \
  -n d18498-tools
```

### Step 6: Create GitHub Repository Variables
```bash
# Using GitHub CLI or via web interface
gh variable set SONAR_HOST_URL --body "https://sonarqube.econ.gov.bc.ca/sonar"

# Or via web: Repository → Settings → Secrets and variables → Actions → Variables
```

### Step 7: Create GitHub Repository Secrets
```bash
# Using GitHub CLI or via web interface  
gh secret set SONAR_TOKEN --body "your_sonarqube_token_here"

# Or via web: Repository → Settings → Secrets and variables → Actions → Secrets
```

## Option 2: Enhanced OpenShift BuildConfig Implementation

### Step 1: Create Custom Builder Image

Create Dockerfile for .NET + SonarQube builder:

```dockerfile
# openshift/builder/Dockerfile
FROM registry.redhat.io/ubi8/dotnet-90:latest

USER root

# Install SonarScanner for .NET
RUN curl -L https://github.com/SonarSource/sonar-scanner-msbuild/releases/download/9.0.0.100794/sonar-scanner-9.0.0.100794-net.zip -o sonar-scanner.zip && \
    unzip sonar-scanner.zip -d /opt/sonar-scanner && \
    rm sonar-scanner.zip && \
    chmod +x /opt/sonar-scanner/SonarScanner.MSBuild.dll

# Add SonarScanner to PATH
ENV PATH="/opt/sonar-scanner:${PATH}"

# Install additional tools
RUN yum install -y git curl && \
    yum clean all

USER 1001
```

### Step 2: Build Custom Builder Image
```bash
# Create BuildConfig for custom builder
oc new-build --dockerfile-path=openshift/builder/Dockerfile \
  --name=dotnet-sonarqube-builder \
  -n d18498-tools

# Start the build
oc start-build dotnet-sonarqube-builder -n d18498-tools

# Monitor build progress
oc logs -f bc/dotnet-sonarqube-builder -n d18498-tools
```

### Step 3: Create SonarQube Configuration Secret
```bash
# Create secret with SonarQube configuration
oc create secret generic unity-sonarqube-config \
  --from-literal=sonar-host-url='https://sonarqube.econ.gov.bc.ca/sonar' \
  --from-literal=sonar-token='your_sonarqube_token_here' \
  --from-literal=project-key='UnityScanKey' \
  -n d18498-tools
```

### Step 4: Create Enhanced BuildConfig Template

```yaml
# openshift/unity-buildconfig-sonarqube.yaml
apiVersion: template.openshift.io/v1
kind: Template
metadata:
  name: unity-sonarqube-buildconfig
parameters:
- name: SOURCE_REPOSITORY_URL
  value: https://github.com/bcgov/Unity
- name: SOURCE_REPOSITORY_REF
  value: dev
- name: APPLICATION_NAME
  value: unity-grantmanager-sonarqube
objects:
- apiVersion: build.openshift.io/v1
  kind: BuildConfig
  metadata:
    name: ${APPLICATION_NAME}
    labels:
      app: ${APPLICATION_NAME}
  spec:
    output:
      to:
        kind: ImageStreamTag
        name: ${APPLICATION_NAME}:latest
    source:
      git:
        uri: ${SOURCE_REPOSITORY_URL}
        ref: ${SOURCE_REPOSITORY_REF}
      contextDir: /applications/Unity.GrantManager
    strategy:
      customStrategy:
        from:
          kind: ImageStreamTag
          name: dotnet-sonarqube-builder:latest
        env:
        - name: SONAR_HOST_URL
          valueFrom:
            secretKeyRef:
              name: unity-sonarqube-config
              key: sonar-host-url
        - name: SONAR_TOKEN
          valueFrom:
            secretKeyRef:
              name: unity-sonarqube-config
              key: sonar-token
        - name: PROJECT_KEY
          valueFrom:
            secretKeyRef:
              name: unity-sonarqube-config
              key: project-key
    triggers:
    - type: ConfigChange
    - type: GitHub
      github:
        secret: webhook-secret-here
```

### Step 5: Create Build Script

```bash
# Create build script in your repo at scripts/openshift-build.sh
#!/bin/bash
set -e

echo "Starting Unity .NET build with SonarQube analysis..."

# Start SonarQube analysis
dotnet sonarscanner begin \
  /k:"${PROJECT_KEY}" \
  /d:sonar.host.url="${SONAR_HOST_URL}" \
  /d:sonar.token="${SONAR_TOKEN}" \
  /d:sonar.cs.vscoveragexml.reportsPaths="TestResults/**/*.xml"

# Restore dependencies
echo "Restoring dependencies..."
dotnet restore Unity.GrantManager.sln

# Build solution
echo "Building solution..."
dotnet build Unity.GrantManager.sln --no-restore

# Run tests with coverage
echo "Running tests with coverage..."
dotnet test Unity.GrantManager.sln \
  --no-build \
  --collect:"XPlat Code Coverage" \
  --logger trx \
  --results-directory TestResults

# End SonarQube analysis
echo "Finishing SonarQube analysis..."
dotnet sonarscanner end /d:sonar.token="${SONAR_TOKEN}"

echo "Build and analysis completed successfully!"
```

### Step 6: Deploy BuildConfig
```bash
# Process and apply the template
oc process -f openshift/unity-buildconfig-sonarqube.yaml \
  -p SOURCE_REPOSITORY_REF=dev \
  | oc apply -f - -n d18498-tools

# Start a build
oc start-build unity-grantmanager-sonarqube -n d18498-tools

# Monitor build logs
oc logs -f bc/unity-grantmanager-sonarqube -n d18498-tools
```

### Step 7: Configure GitHub Webhook
```bash
# Get webhook URL
WEBHOOK_URL=$(oc describe bc unity-grantmanager-sonarqube -n d18498-tools | grep "Webhook GitHub" | awk '{print $3}')

echo "Configure this webhook URL in GitHub:"
echo $WEBHOOK_URL

# In GitHub: Repository → Settings → Webhooks → Add webhook
# Payload URL: Use the webhook URL from above
# Content type: application/json
# Events: Just the push event
```

## Testing and Validation

### Test Option 1 (GitHub Runners)
```bash
# Check runner registration
oc logs -l app=unity-github-runner -n d18498-tools | grep -i "successfully added as a runner"

# Test workflow
# Create a test branch and push to trigger the GitHub workflow
git checkout -b test/sonarqube-option1
git commit --allow-empty -m "Test Option 1 SonarQube integration"
git push origin test/sonarqube-option1
```

### Test Option 2 (BuildConfig)
```bash
# Trigger a build manually
oc start-build unity-grantmanager-sonarqube -n d18498-tools

# Check build logs for SonarQube analysis
oc logs -f bc/unity-grantmanager-sonarqube -n d18498-tools

# Verify SonarQube project appears in UI
curl -u admin:admin https://sonarqube.econ.gov.bc.ca/sonar/api/projects/search?q=Unity
```

### GitHub Self-Hosted Runner Deployment

### GitHub Workflow with SonarQube Integration
```yaml
# .github/workflows/ci-sonarqube.yml
name: CI with SonarQube Analysis

on:
  push:
    branches: [dev, test, main]
  pull_request:
    branches: [dev, test, main]

jobs:
  sonarqube:
    runs-on: [self-hosted, unity-runners]
    
    steps:
    - uses: actions/checkout@v4
      with:
        fetch-depth: 0

    - uses: actions/setup-dotnet@v4
      with:
        dotnet-version: "9.0.x"

    - name: Restore dependencies
      run: dotnet restore applications/Unity.GrantManager/Unity.GrantManager.sln

    - name: Build
      run: dotnet build applications/Unity.GrantManager/Unity.GrantManager.sln --no-restore

    - name: Test with coverage
      run: |
        dotnet test applications/Unity.GrantManager/Unity.GrantManager.sln \
          --no-build \
          --collect:"XPlat Code Coverage" \
          --logger trx \
          --results-directory TestResults

    - name: SonarQube analysis
      env:
        SONAR_TOKEN: ${{ secrets.SONAR_TOKEN }}
        SONAR_HOST_URL: ${{ vars.SONAR_HOST_URL }}
      run: |
        dotnet tool install --global dotnet-sonarscanner
        dotnet sonarscanner begin \
          /k:"UnityScanKey" \
          /d:sonar.host.url="${SONAR_HOST_URL}" \
          /d:sonar.token="${SONAR_TOKEN}" \
          /d:sonar.cs.vscoveragexml.reportsPaths="TestResults/**/*.xml" \
          /d:sonar.branch.name="${GITHUB_REF_NAME}"
        
        dotnet build applications/Unity.GrantManager/Unity.GrantManager.sln
        
        dotnet sonarscanner end /d:sonar.token="${SONAR_TOKEN}"
```

## Additional Option 3 Considerations

**Monitoring and Maintenance**:
- SonarQube instance monitoring and alerting
- Database backup monitoring and automation
- Storage usage tracking and cleanup
- Performance monitoring and tuning
- Security updates and patch management

**Documentation Requirements**:
- SonarQube administration procedures
- Backup and restore documentation
- User management and project configuration guides
- Troubleshooting and maintenance procedures

**Performance Optimization**:
- SonarQube JVM tuning for optimal performance
- Database connection pool optimization
- Plugin evaluation and performance impact
- Quality gate optimization for Unity project needs
- Analysis performance monitoring and improvement