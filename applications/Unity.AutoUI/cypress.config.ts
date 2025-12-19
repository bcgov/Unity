import { defineConfig } from 'cypress';
// https://docs.cypress.io/guides/references/configuration
export default defineConfig({
  e2e: {
    setupNodeEvents(on, config) {
      // implement node event listeners here
    },
    baseUrl: 'https://developer.gov.bc.ca/',
    defaultCommandTimeout: 20000, // Time, in milliseconds, to wait until most DOM based commands are considered timed out.
    viewportWidth: 1440,  // Default width in pixels.
    viewportHeight: 900,  // Default height in pixels.
    chromeWebSecurity: false, // Chromium-based browser's Web Security for same-origin policy and insecure mixed content.
    testIsolation: false,  // Set true to ensure a clean browser context between test cases.
    retries:  // The number of times to retry a failing test. 
      {
        "runMode": 3, 
        "openMode": 0
       },
    experimentalMemoryManagement: true,
    numTestsKeptInMemory: 3
  }
});