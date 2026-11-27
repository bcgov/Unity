# Cypress Environment Config Files

Local Cypress runs read credentials and per-environment settings from this folder. The filled-in `*.json` files are
**excluded from git** (via `.gitignore`) because they contain credentials. Create your own from the `.example` files.

## Local setup — pick one method

### Option A: one file for all environments (recommended)

```powershell
Copy-Item cypress/config/unity-cypress-config.json.example cypress/config/unity-cypress-config.json
```

Fill in the blocks for the environments you use: `CYPRESS_CONFIG_DEV`, `_DEV2`, `_TEST`, `_UAT`, `_PROD`.
This file has the same shape as the CI secret (see below), so one copy can be pasted straight into it.

### Option B: one file per environment

```powershell
Copy-Item cypress/config/dev.json.example   cypress/config/dev.json
Copy-Item cypress/config/dev2.json.example  cypress/config/dev2.json
Copy-Item cypress/config/test.json.example  cypress/config/test.json
Copy-Item cypress/config/uat.json.example   cypress/config/uat.json
Copy-Item cypress/config/prod.json.example  cypress/config/prod.json
```

### How the file is chosen

For the selected environment (`--env environment=<env>`, else `UNITY_CYPRESS_ENV`, else `dev`), `cypress.config.ts` uses:

1. `cypress/config/<env>.json` if it exists, otherwise
2. the `CYPRESS_CONFIG_<ENV>` block of `cypress/config/unity-cypress-config.json`
   (e.g. `--env environment=dev2` → `CYPRESS_CONFIG_DEV2`).

A per-env file always wins, so if you switch to Option A, delete any old `<env>.json` files — otherwise they
silently shadow the combined file. Both methods work with `npm run test:e2e`; `CypressTestLauncher.bat` currently
requires a per-env `<env>.json` (Option B).

### Fields

| Field | Used for |
|---|---|
| `webapp.url`, `environment` | Target Unity URL and environment name |
| `test1username` / `test1password` (+ `test2…`) | IDIR test accounts for login |
| `TEST_EMAIL_TO` / `_CC` / `_BCC` | Email specs |
| `chefsApiKey` | CHEFS form API key for `cy.seedApprovalFlowSubmission()` (the form is set per env in `cypress/scripts/chefs-api-config.json`) |
| `CHEFS_ENV` (optional) | Overrides `environment` for CHEFS lookups |

## CI

CI does not use these files. `.github/workflows/cypress-e2e-runner.yml` writes the env's config to
`cypress.env.json` instead, taken from the `CYPRESS_CONFIG_<ENV>` key of either the `UNITY_CYPRESS_CONFIG` repo secret
(default static-IP runner) or the `unity-cypress-config` Secret synced from Vault key `GH_UGM_CYPRESS_CONFIG`
(`runner=openshift` fallback, see `cypress-job-template.yaml` in `tenant-gitops-ce395f`).

`unity-cypress-config.json.example` is the template for that combined value. Each `CYPRESS_CONFIG_<ENV>` block has
the same fields as that env's `<env>.json.example`; `_DEV2` is for local runs only (CI never reads it). Paste the
whole filled-in JSON into the `UNITY_CYPRESS_CONFIG` GitHub secret and into Vault key `GH_UGM_CYPRESS_CONFIG`, and keep
the two in sync, because both runner modes expect the same shape.

**Never commit a filled-in copy.** `.gitignore` covers `unity-cypress-config.json` and every `<env>.json` in this folder.
When you add or remove a field, update both the per-env `.example` file and the matching block in
`unity-cypress-config.json.example`.
