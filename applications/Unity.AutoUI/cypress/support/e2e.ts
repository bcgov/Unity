// ***********************************************************
declare const cy: any
declare const Cypress: any
declare function before(callback: () => void): void
declare function beforeEach(callback: () => void): void

// This support/e2e.ts file is processed and
// loaded automatically before test files.
//
// You can read more here:
// https://on.cypress.io/configuration
// ***********************************************************

import '../support/commands'

// Block the Unity Messaging realtime (SignalR) connection for every test.
//
// notifications-realtime-client.js connects unconditionally on every page
// load and shows an incoming chat message as a SweetAlert2 *toast*
// (`Swal.fire({ toast: true, ... })`) with no guard against another
// SweetAlert2 popup already being open. SweetAlert2 keeps one shared
// singleton for every popup on the page, so a chat message arriving at the
// same moment the app under test opens its own confirm dialog can corrupt
// that dialog — it's left in the DOM as an orphaned, permanently-hidden
// element (display: none) rather than actually being shown. Because this
// depends on someone else's message timing on a shared UAT test account,
// it surfaced as unpredictable failures at different, unrelated steps
// across otherwise-identical runs.
//
// The connection is negotiated over a plain HTTP POST before any transport
// (WebSocket etc.) is opened (no `skipNegotiation` in the client's
// `HubConnectionBuilder`), so blocking that one request is enough — no
// WebSocket-level interception needed. The client's own connection.start()
// call already has a real .catch() ("Keep normal page flow even when
// realtime connection is unavailable"), so this degrades cleanly with no
// uncaught exception for Cypress to trip over.
//
// Registered in BOTH a root-level `before()` and `beforeEach()` — this is a
// Razor Pages app (not an SPA), so most steps across a multi-test spec like
// ApprovalFlow.cy.ts involve a full page load, and notifications-realtime-
// client.js's unconditional startConnection() call re-runs on every single
// one of them. Cypress clears all `cy.intercept()` registrations between
// `it()` blocks regardless of `testIsolation`, so a single `before()` only
// protects whatever page load happens to occur during the first test — every
// later test's page loads run completely unprotected. `beforeEach()` alone
// isn't enough either: root-level `beforeEach()` hooks run *after* a nested
// describe's own top-level `before()` (Mocha hook ordering), and specs like
// ApprovalFlow.cy.ts log in inside their own `before()`, not `beforeEach()`,
// since testIsolation is off and login happens once for the whole spec — so
// a `beforeEach()`-only registration would miss that first login/page load.
// Together, `before()` covers the gap before the spec's own login, and
// `beforeEach()` re-establishes the block fresh before every subsequent
// test's page loads.
//
// The pattern must end in `/**`, not a bare `**`: Cypress's glob matching
// only treats `**` as "cross any number of path segments" when it's its own
// segment (bounded by `/`). A trailing `**` glued directly onto
// `notifications` doesn't cross the next `/`, so it silently fails to match
// `.../notifications/negotiate?negotiateVersion=1` — confirmed via a
// catch-all `cy.intercept('**', ...)` logger that saw the exact negotiate
// URL on every run while this route's own interception count stayed at 0.
//
// The connection is negotiated over a plain HTTP POST before any transport
// (WebSocket etc.) is opened (no `skipNegotiation` in the client's
// `HubConnectionBuilder`), so blocking that one request is enough — no
// WebSocket-level interception needed. The client's own connection.start()
// call already has a real .catch() ("Keep normal page flow even when
// realtime connection is unavailable"), so this degrades cleanly with no
// uncaught exception for Cypress to trip over.
function blockRealtimeMessaging(): void {
  cy.intercept('**/signalr/notifications/**', {
    statusCode: 503,
    body: {},
  }).as('blockedRealtimeMessaging')
}

before(blockRealtimeMessaging)
beforeEach(blockRealtimeMessaging)

// Ignore common errors that shouldn't fail tests
Cypress.on('uncaught:exception', (err: any) => {
  const environment = (Cypress.env('environment') as string | undefined)?.toLowerCase()

  if ((environment === 'dev' || environment === 'test') && err.message.includes('missing ) after argument list')) {
    return false
  }

  // ResizeObserver loop errors - benign browser notifications
  if (err.message.includes('ResizeObserver loop')) {
    return false
  }
  // Network errors that can occur during navigation
  if (err.message.includes('Network Error') || err.message.includes('net::ERR')) {
    return false
  }
  // Script errors from third-party resources
  if (err.message.includes('Script error')) {
    return false
  }
  // Chunk loading errors
  if (err.message.includes('Loading chunk') || err.message.includes('ChunkLoadError')) {
    return false
  }
  // Return true to fail tests on unexpected uncaught exceptions
  return true
})
