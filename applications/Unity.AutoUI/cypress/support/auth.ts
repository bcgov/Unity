/**
 * Authentication helper for Unity webapp
 * Handles multiple authentication states and provides robust login flow
 */

interface LoginOptions {
  baseUrl?: string;
  useMfa?: boolean;
  timeout?: number;
}

/**
 * Detects if we're on the Keycloak login provider selection page
 */
function isKeycloakPage($body: JQuery<HTMLElement>): boolean {
  return (
    $body.find(".login-pf-page").length > 0 ||
    $body.find("#social-idir").length > 0 ||
    $body.find("#social-azureidir").length > 0
  );
}

/**
 * Detects if we're already logged in
 */
function isLoggedIn($body: JQuery<HTMLElement>): boolean {
  return (
    $body.find('button:contains("VIEW APPLICATIONS")').length > 0 ||
    $body.find("#GrantApplicationsTable").length > 0
  );
}

/**
 * Detects if we're on the login landing page
 */
function isLoginPage($body: JQuery<HTMLElement>): boolean {
  return $body.find('button:contains("LOGIN")').length > 0;
}

/**
 * Handles the Keycloak IDIR selection and login form
 */
function handleKeycloakLogin(useMfa: boolean, timeout: number): void {
  cy.log("🔑 Handling Keycloak login flow");

  cy.get("body", { timeout }).then(($body) => {
    // Click appropriate IDIR provider
    if (useMfa && $body.find("#social-azureidir").length > 0) {
      cy.log("Selecting IDIR - MFA");
      cy.get("#social-azureidir", { timeout }).should("be.visible").click();
    } else if ($body.find("#social-idir").length > 0) {
      cy.log("Selecting IDIR");
      cy.get("#social-idir", { timeout }).should("be.visible").click();
    } else {
      throw new Error(
        "Expected Keycloak IDIR provider buttons but none found. Available: " +
          $body
            .find("a[id^='social-']")
            .map((_, el) => el.id)
            .get()
            .join(", "),
      );
    }
  });

  // Handle username/password form if it appears
  cy.get("body", { timeout }).then(($loginBody) => {
    if ($loginBody.find("#user").length > 0) {
      cy.log("Entering IDIR credentials");

      cy.get("#user", { timeout })
        .should("be.visible")
        .type(Cypress.env("test1username"), { log: false });

      cy.get("#password", { timeout })
        .should("be.visible")
        .type(Cypress.env("test1password"), { log: false });

      // Look for Continue button or submit the form
      cy.get("body").then(($formBody) => {
        if ($formBody.find('button:contains("Continue")').length > 0) {
          cy.contains("button", "Continue", { timeout }).click();
        } else if ($formBody.find("input[type='submit']").length > 0) {
          cy.get("input[type='submit']", { timeout }).click();
        } else {
          cy.log("⚠️ No submit button found, attempting form submission");
          cy.get("#user").parents("form").submit();
        }
      });
    } else {
      cy.log("✓ Already authenticated, skipping credentials");
    }
  });
}

/**
 * Ensures we end up at the GrantApplications page
 */
function ensureGrantApplicationsPage(timeout: number): void {
  cy.location("pathname", { timeout }).then((pathname) => {
    if (pathname.includes("/GrantApplications")) {
      cy.log("✓ Already at GrantApplications page");
      return;
    }

    // Check if VIEW APPLICATIONS button exists
    cy.get("body", { timeout }).then(($body) => {
      if ($body.find('button:contains("VIEW APPLICATIONS")').length > 0) {
        cy.log("Clicking VIEW APPLICATIONS button");
        cy.contains("button", "VIEW APPLICATIONS", { timeout })
          .should("be.visible")
          .click();
      }
    });
  });

  // Final assertion - we should be at GrantApplications
  cy.location("pathname", { timeout }).should("include", "/GrantApplications");
  cy.log("✓ Successfully navigated to GrantApplications");
}

/**
 * Robust login helper that handles multiple Unity webapp states
 *
 * @example
 * // In your test
 * import { loginIfNeeded } from "../support/auth";
 *
 * beforeEach(() => {
 *   loginIfNeeded();
 * });
 */
export function loginIfNeeded(options: LoginOptions = {}): void {
  const baseUrl = options.baseUrl || Cypress.env("webapp.url");
  const useMfa = options.useMfa || false;
  const timeout = options.timeout || 20000;

  cy.log("🚀 Starting loginIfNeeded()");

  // Visit the base URL
  cy.visit(baseUrl);

  // Wait for page to stabilize
  cy.get("body", { timeout }).should("be.visible");

  // Detect current state and handle accordingly
  cy.get("body", { timeout }).then(($body) => {
    const url = $body.prop("baseURI") || "";

    // State 1: Already logged in
    if (isLoggedIn($body)) {
      cy.log("✓ Already logged in");
      ensureGrantApplicationsPage(timeout);
      return;
    }

    // State 2: On Keycloak login selection page
    if (isKeycloakPage($body)) {
      cy.log("📋 Detected Keycloak login page");
      handleKeycloakLogin(useMfa, timeout);
      ensureGrantApplicationsPage(timeout);
      return;
    }

    // State 3: On login landing page
    if (isLoginPage($body)) {
      cy.log("🔓 Detected login landing page");
      cy.contains("button", "LOGIN", { timeout }).should("be.visible").click();

      // After clicking LOGIN, we should reach Keycloak
      cy.get("body", { timeout }).then(($nextBody) => {
        if (isKeycloakPage($nextBody)) {
          handleKeycloakLogin(useMfa, timeout);
          ensureGrantApplicationsPage(timeout);
        } else if (isLoggedIn($nextBody)) {
          cy.log("✓ Already logged in after LOGIN click");
          ensureGrantApplicationsPage(timeout);
        } else {
          throw new Error(
            `Unexpected state after clicking LOGIN. URL: ${url}\n` +
              `Has LOGIN button: ${$nextBody.find('button:contains("LOGIN")').length > 0}\n` +
              `Has VIEW APPLICATIONS: ${$nextBody.find('button:contains("VIEW APPLICATIONS")').length > 0}\n` +
              `Has Keycloak page: ${isKeycloakPage($nextBody)}`,
          );
        }
      });
      return;
    }

    // Unexpected state - provide diagnostic info
    throw new Error(
      `Unable to determine authentication state at ${url}\n` +
        `Has LOGIN button: ${isLoginPage($body)}\n` +
        `Has VIEW APPLICATIONS: ${isLoggedIn($body)}\n` +
        `Has Keycloak page: ${isKeycloakPage($body)}\n` +
        `Current pathname: ${new URL(url).pathname}`,
    );
  });
}

/**
 * Quick logout helper
 */
export function logout(): void {
  cy.log("🚪 Logging out");
  const baseUrl = Cypress.env("webapp.url");
  cy.request("GET", baseUrl + "Account/Logout");
  cy.wait(1000);
  cy.visit(baseUrl);
  cy.log("✓ Logged out");
}
