// ***********************************************************
// This /support/index.d.ts file is processed and
// loaded automatically before test files.
//
// You can read more here:
// https://on.cypress.io/configuration
// ***********************************************************

declare namespace Cypress {
  interface Chainable {
    login(): void; // Custom command to login to Unity (legacy - prefer loginIfNeeded)
    loginIfNeeded(options?: { useMfa?: boolean; timeout?: number }): void; // Robust login helper that handles multiple auth states
    logout(): void; // Custom command to log out of Unity
    getSubmissionDetail(key: string): Chainable<string>; // Custom command to get submission details by key for the current environment.
    getMetabaseDetail(key: string): Chainable<string>; // Custom command to get metabase details by key for the current environment.
    metabaseLogin(): Chainable<void>; // Custom command to login to Metabase
    getChefsDetail(key: string): Chainable<string>; // Custom command to get chefs details by key for the current environment.
    chefsLogin(): Chainable<void>; // Custom command to login to Chefs
    chefsLogout(): Chainable<void>; // Custom command to log out of Chefs
    clearSessionStorage(): Chainable<void>; // Custom command to clear session storage.
    clearBrowserCache(): Chainable<void>; // Custom command to clear browser cache.
  }
}
