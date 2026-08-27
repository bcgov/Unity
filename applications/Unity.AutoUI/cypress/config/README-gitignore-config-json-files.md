# Cypress Environment Config Files

The `*.json` config files in this folder are **excluded from git** (via `.gitignore`) because they contain credentials.
Each developer must create their own local copies from the `.example` files provided.

In CI, `dev.json`/`test.json`/`uat.json`/`prod.json` are written at runtime from the `unity-cypress-config` Secret
(synced from Vault key `GH_UGM_CYPRESS_CONFIG`) by the Cypress Job — see `cypress-job-template.yaml` in
`tenant-gitops-d18498`.

## Setup

Copy the example file(s) for the environment(s) you need and fill in your credentials:

```bash
Copy-Item cypress/config/dev.json.example   cypress/config/dev.json
Copy-Item cypress/config/test.json.example  cypress/config/test.json
Copy-Item cypress/config/uat.json.example   cypress/config/uat.json
Copy-Item cypress/config/prod.json.example  cypress/config/prod.json
```
