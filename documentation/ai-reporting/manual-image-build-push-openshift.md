# Manual Image Build and Push to OpenShift Registry

This guide covers how to manually build and push container images directly to the OpenShift internal registry for testing and development purposes.

## Prerequisites

- OpenShift CLI (`oc`) installed and configured
- Docker Desktop or compatible container runtime
- Access to the target OpenShift namespace

## Step-by-Step Process

### 1. Login to OpenShift

Ensure you're logged into the correct OpenShift cluster and namespace:

```bash
# Check current login status
oc whoami

# Should return your username, e.g., daryl.todosichuk@gov.bc.ca
```

### 2. Configure Docker Registry Access

Login Docker to the OpenShift registry using your current session token:

```bash
# Register Docker with OpenShift registry
oc registry login

# Alternative: Manual Docker login with token
docker login -u unused -p "$(oc whoami -t)" image-registry.apps.silver.devops.gov.bc.ca
```

**Expected Output:**
```
Saved credentials for image-registry.apps.silver.devops.gov.bc.ca
Login Succeeded
```

### 3. Build the Container Image

Navigate to the application directory and build the image with appropriate tags:

```bash
cd applications

# Build Unity.AI.Reporting.Frontend
docker build \
  --build-arg UAI_BUILD_VERSION=1.0.0 \
  --build-arg UAI_BUILD_REVISION=manual-build \
  -t unity-ai-reporting-frontend:latest \
  -f Unity.AI.Reporting.Frontend/Dockerfile \
  Unity.AI.Reporting.Frontend

# Build Unity.AI.Assessment.Frontend
docker build \
  -t unity-ai-assessment-frontend:latest \
  -f Unity.AI.Assessment.Frontend/Dockerfile \
  Unity.AI.Assessment.Frontend
```

**Build Args Explanation:**
- `UAI_BUILD_VERSION`: Semantic version for the build
- `UAI_BUILD_REVISION`: Git commit hash or custom identifier

### 4. Tag Image for OpenShift Registry

Tag the local image with the OpenShift registry path:

```bash
# For dev environment
docker tag unity-ai-reporting-frontend:latest \
  image-registry.apps.silver.devops.gov.bc.ca/d18498-dev/unity-ai-reporting-frontend:latest

# For test environment  
docker tag unity-ai-reporting-frontend:latest \
  image-registry.apps.silver.devops.gov.bc.ca/d18498-test/unity-ai-reporting-frontend:latest

# For prod environment
docker tag unity-ai-reporting-frontend:latest \
  image-registry.apps.silver.devops.gov.bc.ca/d18498-prod/unity-ai-reporting-frontend:latest
```

**Registry Path Format:**
```
image-registry.apps.silver.devops.gov.bc.ca/{namespace}/{image-name}:{tag}
```

### 5. Push Image to OpenShift Registry

Push the tagged image to the OpenShift internal registry:

```bash
# Push to dev environment
docker push image-registry.apps.silver.devops.gov.bc.ca/d18498-dev/unity-ai-reporting-frontend:latest

# Push to other environments as needed
docker push image-registry.apps.silver.devops.gov.bc.ca/d18498-test/unity-ai-reporting-frontend:latest
docker push image-registry.apps.silver.devops.gov.bc.ca/d18498-prod/unity-ai-reporting-frontend:latest
```

**Expected Output:**
```
The push refers to repository [image-registry.apps.silver.devops.gov.bc.ca/d18498-dev/unity-ai-reporting-frontend]
latest: digest: sha256:abc123... size: 856
```

### 6. Trigger Pod Restart

Force the StatefulSet to pull and use the new image:

```bash
# Restart the frontend StatefulSet
oc -n d18498-dev rollout restart statefulset dev-unity-ai-frontend

# Monitor rollout status
oc -n d18498-dev rollout status statefulset dev-unity-ai-frontend

# Check pod status
oc -n d18498-dev get pods -l app=unity-ai-frontend
```

### 7. Verify Deployment

Confirm the new image is running successfully:

```bash
# Check pod logs for successful startup
oc -n d18498-dev logs dev-unity-ai-frontend-0

# Verify pod is running
oc -n d18498-dev get pods -l app=unity-ai-frontend
```

**Success Indicators:**
- Pod status: `1/1 Running`
- Nginx logs show successful worker process startup
- No permission denied or crash loop errors

## Environment-Specific Registry Paths

| Environment | Namespace | Registry Path |
|-------------|-----------|---------------|
| **Development** | `d18498-dev` | `image-registry.apps.silver.devops.gov.bc.ca/d18498-dev/` |
| **Test** | `d18498-test` | `image-registry.apps.silver.devops.gov.bc.ca/d18498-test/` |
| **UAT** | `d18498-uat` | `image-registry.apps.silver.devops.gov.bc.ca/d18498-uat/` |
| **Production** | `d18498-prod` | `image-registry.apps.silver.devops.gov.bc.ca/d18498-prod/` |

## Complete Example Workflow

```bash
# 1. Login and setup
oc whoami
oc registry login

# 2. Build image
cd applications
docker build \
  --build-arg UAI_BUILD_VERSION=1.1.0 \
  --build-arg UAI_BUILD_REVISION=hotfix-123 \
  -t unity-ai-reporting-frontend:hotfix \
  -f Unity.AI.Reporting.Frontend/Dockerfile \
  Unity.AI.Reporting.Frontend

# 3. Tag for OpenShift
docker tag unity-ai-reporting-frontend:hotfix \
  image-registry.apps.silver.devops.gov.bc.ca/d18498-dev/unity-ai-reporting-frontend:latest

# 4. Push to registry
docker push image-registry.apps.silver.devops.gov.bc.ca/d18498-dev/unity-ai-reporting-frontend:latest

# 5. Deploy to OpenShift
oc -n d18498-dev rollout restart statefulset dev-unity-ai-frontend
oc -n d18498-dev rollout status statefulset dev-unity-ai-frontend

# 6. Verify success
oc -n d18498-dev get pods -l app=unity-ai-frontend
oc -n d18498-dev logs dev-unity-ai-frontend-0 --tail=20
```

## Troubleshooting

### Common Issues

**1. Authentication Errors**
```bash
# Error: 401 Unauthorized
# Solution: Re-authenticate
oc registry login
docker login -u unused -p "$(oc whoami -t)" image-registry.apps.silver.devops.gov.bc.ca
```

**2. Registry Connection Issues**
```bash
# Error: no such host
# Solution: Use external registry hostname
image-registry.apps.silver.devops.gov.bc.ca  # External 
image-registry.openshift-image-registry.svc  # Internal 
```

**3. Pod Not Updating**
```bash
# Force pod recreation
oc -n d18498-dev delete pod dev-unity-ai-frontend-0

# Check StatefulSet configuration
oc -n d18498-dev describe statefulset dev-unity-ai-frontend
```

**4. Image Pull Errors**
```bash
# Verify image exists in registry
oc -n d18498-dev get imagestream

# Check image pull policy
oc -n d18498-dev get statefulset dev-unity-ai-frontend -o yaml | grep imagePullPolicy
```

## Security Considerations

- **Token Expiry**: OpenShift tokens expire regularly; re-authenticate as needed
- **Registry Access**: Only push to namespaces you have access to
- **Image Scanning**: Images may be scanned for vulnerabilities in production
- **Build Arguments**: Avoid passing sensitive data as build arguments

## Integration with CI/CD

This manual process is useful for:
- **Development Testing**: Quick iteration during development
- **Hotfixes**: Emergency deployments outside normal CI/CD
- **Debugging**: Testing specific image versions
- **GitOps Bypass**: When automated pipelines are unavailable

For production deployments, prefer the automated GitOps pipeline via GitHub Actions.

## Related Documentation

- [GitHub Actions Docker Build Workflow](../.github/workflows/docker-build-dev.yml)
- [OpenShift Registry Documentation](https://docs.openshift.com/container-platform/4.11/registry/index.html)