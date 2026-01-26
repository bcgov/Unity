# oc/docker CLI script to tag and push Unity AI images
# Make sure you're logged in first: 'oc login --web --server=https://api.silver.devops.gov.bc.ca:6443

$OC_REGISTRY = "image-registry.apps.silver.devops.gov.bc.ca"
$OC_TARGET_PROJECT = "d18498-dev"

# Authenticate
oc registry login
docker login -u unused -p $(oc whoami -t) $OC_REGISTRY

# Tag and push unity-ai-platform-reporting
docker tag unity-ai-reporting $OC_REGISTRY/$OC_TARGET_PROJECT/dev-unity-ai-reporting
docker push $OC_REGISTRY/$OC_TARGET_PROJECT/dev-unity-ai-reporting

# Debugging commands forced restart process for frontend StatefulSet
# Scale down
oc -n d18498-dev scale statefulset/dev-unity-ai-reporting --replicas=0
# Restart the frontend StatefulSet
oc -n d18498-dev rollout restart statefulset dev-unity-ai-reporting
# Scale up to desired replicas 
oc -n d18498-dev scale statefulset/dev-unity-ai-reporting --replicas=1