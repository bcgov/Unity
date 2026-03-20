// ***********************************************************
// This support/e2e.ts file is processed and
// loaded automatically before test files.
//
// You can read more here:
// https://on.cypress.io/configuration
// ***********************************************************

import '../support/commands'

// Ignore common errors that shouldn't fail tests
Cypress.on('uncaught:exception', (err) => {
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
