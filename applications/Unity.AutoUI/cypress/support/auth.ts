/**
 * Authentication helper for Unity webapp
 * Handles multiple authentication states and provides robust login flow
 */

interface LoginOptions {
  baseUrl?: string;
  useMfa?: boolean;
  timeout?: number;
  username?: string;
  password?: string;
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

function hasCredentialForm($body: JQuery<HTMLElement>): boolean {
  return (
    $body.find("#user, input[name='user'], input[name='username']").length > 0 &&
    $body.find("#password, input[name='password'], input[type='password']").length > 0
  );
}

function hasViewApplicationsButton($body: JQuery<HTMLElement>): boolean {
  return $body.find('button:contains("VIEW APPLICATIONS")').length > 0;
}

function waitForCredentialFormOrAuthenticatedPage(timeout: number): void {
  cy.get("body", { timeout }).should(($body) => {
    const pathname = $body[0]?.ownerDocument?.location?.pathname ?? "";

    const isReady =
      pathname.includes("/GrantApplications") ||
      hasViewApplicationsButton($body) ||
      hasCredentialForm($body);

    expect(
      isReady,
      `expected credential form, VIEW APPLICATIONS button, or /GrantApplications. Current path: ${pathname}`,
    ).to.equal(true);
  });
}

function getExistingSelector(
  $body: JQuery<HTMLElement>,
  selectors: string[],
): string {
  const selector = selectors.find((candidate) => $body.find(candidate).length > 0);

  if (!selector) {
    throw new Error(
      `None of the expected selectors were found: ${selectors.join(", ")}`,
    );
  }

  return selector;
}

/**
 * Handles the Keycloak IDIR selection and login form
 */
function handleKeycloakLogin(
  options: LoginOptions,
  useMfa: boolean,
  timeout: number,
): void {
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

  waitForCredentialFormOrAuthenticatedPage(timeout);

  // Handle username/password form if it appears
  cy.get("body", { timeout }).then(($loginBody) => {
    if (!hasCredentialForm($loginBody)) {
      cy.log("✓ Already authenticated, skipping credentials");
      return;
    }

    cy.log("Entering IDIR credentials");

    const username = options.username || Cypress.env("test1username");
    const password = options.password || Cypress.env("test1password");
    const usernameSelector = getExistingSelector($loginBody, [
      "#user",
      "input[name='user']",
      "input[name='username']",
    ]);
    const passwordSelector = getExistingSelector($loginBody, [
      "#password",
      "input[name='password']",
      "input[type='password']",
    ]);

    cy.get(usernameSelector, { timeout })
      .should("be.visible")
      .clear()
      .type(username, { log: false });

    cy.get(passwordSelector, { timeout })
      .should("be.visible")
      .clear()
      .type(password, { log: false });

    // Look for Continue button or submit the form
    cy.get("body").then(($formBody) => {
      if ($formBody.find('button:contains("Continue")').length > 0) {
        cy.contains("button", "Continue", { timeout }).click();
      } else if ($formBody.find("input[type='submit']").length > 0) {
        cy.get("input[type='submit']", { timeout }).click();
      } else if ($formBody.find("button[type='submit']").length > 0) {
        cy.get("button[type='submit']", { timeout }).click();
      } else {
        cy.log("⚠️ No submit button found, attempting form submission");
        cy.get(usernameSelector).parents("form").submit();
      }
    });
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
 * Performs the actual login flow
 */
function performLogin(options: LoginOptions = {}): void {
  const baseUrl = options.baseUrl || (Cypress.env("webapp.url") as string);
  const useMfa = options.useMfa || false;
  const timeout = options.timeout || 20000;

  cy.visit(baseUrl);

  cy.get("body", { timeout }).then(($body) => {
    // Check if already logged in
    if (isLoggedIn($body)) {
      cy.log("✓ Already logged in");
      return;
    }

    // Click LOGIN button if on landing page
    if (isLoginPage($body)) {
      cy.log("Clicking LOGIN button");
      cy.contains("button", "LOGIN", { timeout }).click();
    }
  });

  // Handle Keycloak login if needed
  cy.get("body", { timeout }).then(($body) => {
    if (isKeycloakPage($body)) {
      handleKeycloakLogin(options, useMfa, timeout);
    }
  });

  // Wait for login to complete - verify we're at GrantApplications or can navigate there
  ensureGrantApplicationsPage(timeout);
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
export function loginIfNeeded(
  options: LoginOptions = {},
): void {
  const username = options.username || (Cypress.env("test1username") as string);
  const baseUrl = options.baseUrl || (Cypress.env("webapp.url") as string);
  const sessionId = `unity-${baseUrl}-${username}`;

  cy.session(
    sessionId,
    () => {
      performLogin(options);
    },
    {
      validate() {
        // Lightweight validation: check auth cookies exist without visiting
        cy.getCookie(".AspNetCore.Cookies").then((cookie) => {
          if (!cookie) {
            throw new Error("Session expired - auth cookie missing");
          }
        });
      },
      cacheAcrossSpecs: true,
    },
  );

  cy.then(() => {
    cy.visit(baseUrl);
    ensureGrantApplicationsPage(options.timeout || 20000);
  });
}
