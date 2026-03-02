// ***********************************************************
// This support/e2e.ts file is processed and
// loaded automatically before test files.
//
// You can read more here:
// https://on.cypress.io/configuration
// ***********************************************************

import '../support/commands'

// Ignore ResizeObserver loop errors - these are benign browser notifications
// that occur when ResizeObserver callbacks don't complete in a single animation frame
Cypress.on('uncaught:exception', (err) => {
  if (err.message.includes('ResizeObserver loop')) {
    return false
  }
  // Return true to fail the test for other errors
  return true
})
