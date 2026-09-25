# Unity.GrantManager.Angular

The Angular front end for the **strangler-fig migration** of Unity Portal's UI. It is a
genuinely separate deployable (own container image, own OpenShift Deployment/Service)
that shares one hostname with the existing ASP.NET Core MVC app through a Route path
split: `/app/*` → this app, everything else → MVC.

So far one page has been migrated: **Program Details**, a section of Configuration
Management.

- Architecture, request flows, styling strategy, gotchas and roadmap:
  [`documentation/angular-strangler-fig/architecture.md`](../../documentation/angular-strangler-fig/architecture.md)
- The dev-only reverse proxy and its troubleshooting:
  [`local-dev-gateway/README.md`](local-dev-gateway/README.md)

> **Plain `ng serve` on `http://localhost:4200` will not work.** The app needs to be on
> the same origin as the backend or OIDC login, cookies and CSRF all fail, and every
> `/api/angular-app/*` call 401s. Use the setup below. (The stock `.vscode/launch.json`
> "ng serve" configuration is Angular CLI scaffolding and points at 4200 — ignore it.)

## Prerequisites

| Need | Notes |
|---|---|
| Node.js 22 | Matches `node:22-alpine` in the [`Dockerfile`](Dockerfile). |
| Angular CLI 20 | Installed locally by `npm install` — the run commands and `scripts/start-local-dev.ps1` invoke it as `npx ng`, so no global install is needed. A global `npm install -g @angular/cli` also works if you prefer typing bare `ng`. |
| .NET SDK 10 | For the MVC backend. |
| OpenSSL | Only for the one-time cert export. Git for Windows ships one. |
| A backend that already runs locally | See [`applications/Unity.GrantManager/README.md`](../Unity.GrantManager/README.md): `abp install-libs`, then run `Unity.GrantManager.DbMigrator` to create and seed the database. |
| A login with access to the page | The Program Details entry point requires the `system_admin` role, the `SettingManagement.Enable` feature, and the `SettingManagement.EditProgramDetails` permission. |

## One-time setup

```powershell
# 1. Front-end dependencies (this app, and the dev gateway's single dependency)
cd applications\Unity.GrantManager.Angular
npm install
cd local-dev-gateway
npm install

# 2. Trust the ASP.NET Core dev cert, and export it to PEM so the Angular dev server
#    and the gateway can both serve HTTPS with the same cert the backend uses.
#    Export OUTSIDE the repo - never commit private keys.
dotnet dev-certs https --check --trust

$certDir = "$env:USERPROFILE\.dev-certs"
New-Item -ItemType Directory -Force -Path $certDir
dotnet dev-certs https -ep "$certDir\localhost.pfx" -p devcert --trust
openssl pkcs12 -in "$certDir\localhost.pfx" -clcerts -nokeys -out "$certDir\localhost-cert.pem" -passin pass:devcert
openssl pkcs12 -in "$certDir\localhost.pfx" -nocerts -nodes  -out "$certDir\localhost-key.pem"  -passin pass:devcert
```

## Running it

Three processes, all over HTTPS with that same cert:

| Process | Port | |
|---|---|---|
| MVC backend | 44343 | Moved off its usual 44342 — the gateway takes that port |
| Angular dev server | 4300 | Serve-path `/app/`, configuration `development-app-path` |
| local-dev-gateway | **44342** | The origin you actually browse to |

One script starts all three in their own windows, killing anything already bound to
those ports first:

```powershell
applications\Unity.GrantManager.Angular\scripts\start-local-dev.ps1
```

Or start them by hand — see
[`local-dev-gateway/README.md`](local-dev-gateway/README.md) for the individual
commands and for troubleshooting.

### Then

1. Browse to **`https://localhost:44342`** (not 44343, not 4300, not 4200) and log in
   as normal. Sessions don't survive restarting the backend.
2. Open the user dropdown → **Configuration Management**. That page is still MVC.
3. Click **Program Details** in its side menu — this navigates to
   `/app/configuration-management/program-details`, which is served by Angular. That
   handoff, and the fact that it looks and behaves identically, is the POC.

## Other commands

```powershell
npm test                                   # Karma/Jasmine unit tests
npx ng build --configuration production --base-href=/app/   # what the Dockerfile builds
```

The production image is a static nginx build ([`Dockerfile`](Dockerfile),
[`nginx.conf`](nginx.conf)) serving the app under `/app/` on port 8080 — the OpenShift
Route forwards the `/app` prefix intact, so the base href must match.
