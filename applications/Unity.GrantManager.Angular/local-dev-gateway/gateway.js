// Local-only dev gateway: fronts both the Angular dev server and the real
// ASP.NET Core backend on ONE origin, mirroring what the OpenShift Route path
// split (path "/app" -> angular service, path "/" -> web service) does in every
// real environment. Without this, OIDC's redirect_uri and any relative-path
// Response.Redirect from the backend can only ever resolve back to the backend's
// own origin, never to a separate Angular dev-server port - that's a local-
// topology gap, not a production bug. See ../../local-dev-gateway/README.md (or
// the strangler-fig migration plan) for the full explanation and setup steps.
//
// Not part of the deployed app - this only exists to make local testing possible
// before the real OpenShift Routes exist.
const https = require('https');
const fs = require('fs');
const path = require('path');
const httpProxy = require('http-proxy');

const GATEWAY_PORT = Number(process.env.LOCAL_GATEWAY_PORT || 44342);
const ANGULAR_TARGET = process.env.LOCAL_GATEWAY_ANGULAR_TARGET || 'https://localhost:4300';
const BACKEND_TARGET = process.env.LOCAL_GATEWAY_BACKEND_TARGET || 'https://localhost:44343';
const CERT_PATH = process.env.LOCAL_GATEWAY_CERT;
const KEY_PATH = process.env.LOCAL_GATEWAY_KEY;

if (!CERT_PATH || !KEY_PATH) {
  console.error(
    'LOCAL_GATEWAY_CERT and LOCAL_GATEWAY_KEY must both be set to PEM file paths.\n' +
      'See README.md in this folder for how to export the trusted ASP.NET Core dev cert to PEM.'
  );
  process.exit(1);
}

const proxy = httpProxy.createProxyServer({
  secure: false, // upstream targets use the same dev cert but skip verification anyway - defensive
  changeOrigin: false // preserve the original Host header so the backend computes
                       // redirect_uri / relative redirects against this gateway's
                       // own host:port, not its own listening port
});

proxy.on('error', (err, req, res) => {
  console.error('[proxy error]', req.url, err.message);
  if (res && !res.headersSent) {
    res.writeHead(502);
    res.end('Bad gateway: ' + err.message);
  }
});

const server = https.createServer(
  {
    cert: fs.readFileSync(path.resolve(CERT_PATH)),
    key: fs.readFileSync(path.resolve(KEY_PATH))
  },
  (req, res) => {
    const target = req.url.startsWith('/app') ? ANGULAR_TARGET : BACKEND_TARGET;
    console.log(`[gateway] ${req.method} ${req.url} -> ${target}`);
    proxy.web(req, res, { target });
  }
);

server.on('upgrade', (req, socket, head) => {
  const target = req.url.startsWith('/app') ? ANGULAR_TARGET : BACKEND_TARGET;
  proxy.ws(req, socket, head, { target });
});

server.listen(GATEWAY_PORT, () => {
  console.log(`Local gateway listening on https://localhost:${GATEWAY_PORT}`);
  console.log(`  /app/*  -> ${ANGULAR_TARGET}`);
  console.log(`  /*      -> ${BACKEND_TARGET}`);
});
