# Cypress Environment Config Files

The `*.json` config files in this folder are **excluded from git** (via `.gitignore`) because they contain credentials.
Each developer must create their own local copies from the `.example` files provided.

In CI these files are not used — `.github/workflows/cypress-e2e-runner.yml` writes the env's config to
`cypress.env.json` instead, taken from the `CYPRESS_CONFIG_<ENV>` key of either the `UNITY_CYPRESS_CONFIG` repo secret
(default static-IP runner) or the `unity-cypress-config` Secret synced from Vault key `GH_UGM_CYPRESS_CONFIG`
(`runner=openshift` fallback, see `cypress-job-template.yaml` in `tenant-gitops-ce395f`).

## Setup

Copy the example file(s) for the environment(s) you need and fill in your credentials:

```bash
Copy-Item cypress/config/dev.json.example   cypress/config/dev.json
Copy-Item cypress/config/test.json.example  cypress/config/test.json
Copy-Item cypress/config/uat.json.example   cypress/config/uat.json
Copy-Item cypress/config/prod.json.example  cypress/config/prod.json
```
