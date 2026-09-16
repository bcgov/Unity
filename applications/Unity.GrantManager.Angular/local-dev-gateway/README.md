# Local dev gateway

Local-only tooling. Puts the Angular app and the MVC backend on **one origin**
(`https://localhost:44342`), the same way the real OpenShift Route path split
(`/app` → Angular service, `/` → MVC service) will in every real environment.
Without this, things that assume a single origin break in ways that don't happen
in production:

- The Keycloak client (`unity-4899`) only trusts `redirect_uri=https://localhost:44342/signin-oidc` — a different port fails OIDC login outright.
- The backend's relative `Response.Redirect(...)` calls and computed `redirect_uri` only land in the right place if the backend *appears* to be on the same host:port the browser is actually using.

Not part of the deployed app — nothing here ships.

## One-time setup

1. Make sure the ASP.NET Core dev cert is trusted (usually already done if you've
   run this app before):
   ```powershell
   dotnet dev-certs https --check --trust
   ```
2. Export it to PEM once, into a folder **outside the repo** (never commit private
   keys):
   ```powershell
   $certDir = "$env:USERPROFILE\.dev-certs"
   New-Item -ItemType Directory -Force -Path $certDir
   dotnet dev-certs https -ep "$certDir\localhost.pfx" -p devcert --trust
   # Convert to PEM (requires openssl - Git for Windows ships one):
   openssl pkcs12 -in "$certDir\localhost.pfx" -clcerts -nokeys -out "$certDir\localhost-cert.pem" -passin pass:devcert
   openssl pkcs12 -in "$certDir\localhost.pfx" -nocerts -nodes -out "$certDir\localhost-key.pem" -passin pass:devcert
   ```
3. Install front-end dependencies - this folder's one dependency, and the Angular
   app's:
   ```powershell
   cd applications\Unity.GrantManager.Angular
   npm install
   cd local-dev-gateway
   npm install
   ```
4. Make sure the backend itself already runs locally (database created and seeded via
   `Unity.GrantManager.DbMigrator`) - see
   [`applications/Unity.GrantManager/README.md`](../../Unity.GrantManager/README.md).

## Every time: three things running, in order

`..\scripts\start-local-dev.ps1` does all three at once, in their own windows,
killing anything already bound to those ports first. To run them by hand instead:

**1. Backend**, over HTTPS, on port 44343 (not its usual 44342 — the gateway
takes that port):
```powershell
cd applications\Unity.GrantManager\src\Unity.GrantManager.Web
dotnet run --urls https://localhost:44343
```

**2. Angular**, over HTTPS, on port 4300, with the `/app/` serve-path
configuration (plain `ng serve` on the default port/config will *not* work under
`/app/` — see `angular.json`'s `development-app-path` configuration):
```powershell
cd applications\Unity.GrantManager.Angular
npx ng serve --configuration=development-app-path --port 4300 --serve-path=/app/ `
  --ssl --ssl-cert "$env:USERPROFILE\.dev-certs\localhost-cert.pem" --ssl-key "$env:USERPROFILE\.dev-certs\localhost-key.pem"
```

**3. This gateway**, on port 44342 (the one you actually browse to):
```powershell
cd applications\Unity.GrantManager.Angular\local-dev-gateway
$env:LOCAL_GATEWAY_CERT = "$env:USERPROFILE\.dev-certs\localhost-cert.pem"
$env:LOCAL_GATEWAY_KEY = "$env:USERPROFILE\.dev-certs\localhost-key.pem"
npm start
```

Then browse to **`https://localhost:44342`** — not 44343, not 4300, not
`http://localhost:4200` (a plain default `ng serve`). Log in as normal; sessions
don't survive stopping/restarting the backend.

## Troubleshooting

- **`invalid_request: Invalid parameter: redirect_uri` from Keycloak** — something
  is listening on port 44342 that isn't this gateway (the real backend running
  directly on 44342, most likely). Stop it and move it to 44343.
- **Certificate warnings in the browser** — you're on `http://` where `https://`
  was expected, or `LOCAL_GATEWAY_CERT`/`LOCAL_GATEWAY_KEY` point at the wrong
  files. Re-run the one-time export steps above.
- **`ng : The term 'ng' is not recognized`** - you dropped the `npx` prefix and have
  no global Angular CLI. Use `npx ng serve ...` as shown above, or install the CLI
  globally (`npm install -g @angular/cli`).
- **Program Details (or any `/app/*` route) 404s** — the gateway isn't running,
  or Angular isn't actually up on port 4300 with `--serve-path=/app/`.
