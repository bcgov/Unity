/// <reference types="cypress" />

export {};

/**
 * CHEFS Bulk Submission Seeder
 *
 * Creates SUBMISSION_COUNT submitted form entries in CHEFS via API and writes
 * all confirmation IDs to cypress/scripts/bulk-submission-ids.json so that
 * BulkPaymentApproval.cy.ts can process them.
 *
 * Configuration:
 *   SUBMISSION_COUNT — number of submissions to create (default: 10).
 *   Set via CYPRESS_SUBMISSION_COUNT env var or directly below.
 *
 * Configuration files:
 *   cypress/scripts/chefs-submission-payload.json  — form submission data
 *   cypress/scripts/chefs-api-config.json          — API config and headers
 */

// ─── Configuration ────────────────────────────────────────────────────────
/** Override via: CYPRESS_SUBMISSION_COUNT=5 npx cypress run ... */
const SUBMISSION_COUNT: number = Number(Cypress.env("SUBMISSION_COUNT") || 10);

// ─── Types ────────────────────────────────────────────────────────────────

interface ChefsEnvironment {
  baseURL: string;
  formId: string;
  versionId: string;
}

interface ChefsApiConfig {
  environments: Record<string, ChefsEnvironment>;
  headers: Record<string, string>;
}

interface ChefsSubmissionPayload {
  draft?: boolean;
  submission: {
    state: string;
    metadata: {
      origin: string;
      referrer: string;
    };
    data: Record<string, unknown>;
  };
}

// ─── Token Extraction Helpers ─────────────────────────────────────────────
// Identical to chefs-api-submission.cy.ts — extracts JWT from browser storage.

const TOKEN_PROPERTY_KEYS = [
  "access_token",
  "accessToken",
  "token",
  "id_token",
  "idToken",
];

function isJwtLike(value: string): boolean {
  return /^[A-Za-z0-9-_]+\.[A-Za-z0-9-_]+\.[A-Za-z0-9-_]+$/.test(value);
}

function extractTokenFromString(value: string): string {
  const trimmed = value.trim();
  if (trimmed.toLowerCase().startsWith("bearer ")) {
    const bearer = trimmed.replace(/^Bearer\s+/i, "").trim();
    if (isJwtLike(bearer)) return bearer;
  }
  if (isJwtLike(trimmed)) return trimmed;
  try {
    return extractTokenFromValue(JSON.parse(trimmed));
  } catch {
    return "";
  }
}

function extractTokenFromArray(values: unknown[]): string {
  for (const v of values) {
    const t = extractTokenFromValue(v);
    if (t) return t;
  }
  return "";
}

function extractTokenFromObject(value: Record<string, unknown>): string {
  for (const key of TOKEN_PROPERTY_KEYS) {
    const t = extractTokenFromValue(value[key]);
    if (t) return t;
  }
  return extractTokenFromArray(Object.values(value));
}

function extractTokenFromValue(value: unknown): string {
  if (typeof value === "string") return extractTokenFromString(value);
  if (Array.isArray(value)) return extractTokenFromArray(value);
  if (value && typeof value === "object")
    return extractTokenFromObject(value as Record<string, unknown>);
  return "";
}

function extractTokenFromStorage(win: Window): string {
  const storages = [win.localStorage, win.sessionStorage];
  for (const storage of storages) {
    for (let i = 0; i < storage.length; i++) {
      const key = storage.key(i);
      if (!key) continue;
      const raw = storage.getItem(key);
      if (!raw) continue;
      const t = extractTokenFromValue(raw);
      if (t) return t;
    }
  }
  return "";
}

function getChefsHostname(baseURL: string): string {
  return new URL(baseURL).hostname;
}

function waitForIdentityRedirectOrAuthenticatedChefsPage(
  baseURL: string,
  timeout: number,
): void {
  const chefsHostname = getChefsHostname(baseURL);
  cy.location("hostname", { timeout }).should((hostname) => {
    expect(
      hostname === chefsHostname || hostname.endsWith("gov.bc.ca"),
      `Expected CHEFS or BC Gov identity host, got '${hostname}'`,
    ).to.eq(true);
  });
}

function completeChefsLogin(environment: ChefsEnvironment, timeout: number): void {
  const chefsHostname = getChefsHostname(environment.baseURL);

  cy.visit(`${environment.baseURL}/app`);

  cy.get("#app > div > main > header > header > div > div.d-print-none", {
    timeout,
  })
    .should("exist")
    .click();

  cy.get(
    "#app > div > main > div.v-container.v-locale--is-ltr.text-center.main > div > div:nth-child(2) > div > button",
    { timeout },
  )
    .should("exist")
    .click();

  waitForIdentityRedirectOrAuthenticatedChefsPage(environment.baseURL, timeout);

  cy.location("hostname", { timeout }).then((hostname) => {
    if (hostname === chefsHostname) {
      cy.log("Already logged in to CHEFS");
      return;
    }

    cy.get("#user", { timeout })
      .should("be.visible")
      .clear()
      .type(Cypress.env("test1username"), { log: false });

    cy.get("#password", { timeout })
      .should("be.visible")
      .clear()
      .type(Cypress.env("test1password"), { log: false });

    cy.contains("Continue", { timeout }).should("be.visible").click();
    cy.location("hostname", { timeout }).should("eq", chefsHostname);
  });
}

function visitChefsForm(environment: ChefsEnvironment, timeout: number): void {
  cy.visit(`${environment.baseURL}/app/form/submit?f=${environment.formId}`);
  cy.location("hostname", { timeout }).should(
    "eq",
    getChefsHostname(environment.baseURL),
  );
  cy.location("pathname", { timeout }).should("include", "/app");
}

// ─── Spec ────────────────────────────────────────────────────────────────

const isProd =
  (Cypress.env("CHEFS_ENV") || "").toLowerCase() === "prod" ||
  (Cypress.env("environment") || "").toLowerCase() === "prod";

(isProd ? describe.skip : describe)("CHEFS Bulk Submission Seeder", () => {
  let apiConfig: ChefsApiConfig;
  let submissionPayload: ChefsSubmissionPayload;
  let environment: ChefsEnvironment;
  let authToken: string;

  before(() => {
    const authTimeout = 60000;

    cy.readFile("cypress/scripts/chefs-api-config.json").then((config) => {
      apiConfig = config;

      const envKey = (
        Cypress.env("CHEFS_ENV") ||
        Cypress.env("environment") ||
        "test"
      ).toLowerCase();

      environment = config.environments[envKey];

      expect(
        environment,
        `Missing CHEFS environment config for '${envKey}'`,
      ).to.exist;

      cy.log(`Using environment: ${envKey}`);
      cy.log(`Submission count: ${SUBMISSION_COUNT}`);

      cy.readFile("cypress/scripts/chefs-submission-payload.json").then(
        (payload) => {
          submissionPayload = payload;
          submissionPayload.submission.metadata.origin = environment.baseURL;
          submissionPayload.submission.metadata.referrer = `${environment.baseURL}/app/form/submit?f=${environment.formId}`;
        },
      );

      let capturedToken = "";

      cy.intercept("**/app/api/v1/**", (req) => {
        const authHeader = req.headers["authorization"] as string;
        if (authHeader && !capturedToken) {
          capturedToken = authHeader.replace(/^Bearer\s+/i, "");
        }
      }).as("chefsApiCalls");

      completeChefsLogin(environment, authTimeout);
      visitChefsForm(environment, authTimeout);

      cy.window({ timeout: authTimeout })
        .should((win) => {
          const tokenFromStorage = extractTokenFromStorage(win);
          const resolved = capturedToken || tokenFromStorage;
          expect(resolved, "Waiting for CHEFS auth token").to.not.equal("");
          if (!capturedToken && tokenFromStorage) capturedToken = tokenFromStorage;
        })
        .then(() => {
          authToken = capturedToken;
          cy.log("✅ Auth token captured");
        });
    });
  });

  // Creates SUBMISSION_COUNT submissions sequentially and writes all
  // confirmation IDs to bulk-submission-ids.json for BulkPaymentApproval.cy.ts.
  it(`Create ${SUBMISSION_COUNT} bulk submissions`, () => {
    const confirmationIds: string[] = [];

    Cypress._.times(SUBMISSION_COUNT, (i) => {
      const submissionUrl = `${environment.baseURL}/app/api/v1/forms/${environment.formId}/versions/${environment.versionId}/submissions`;

      cy.request({
        method: "POST",
        url: submissionUrl,
        headers: {
          ...apiConfig.headers,
          Authorization: `Bearer ${authToken}`,
          Origin: environment.baseURL,
          Referer: `${environment.baseURL}/app/form/submit?f=${environment.formId}`,
        },
        body: submissionPayload,
        failOnStatusCode: false,
      }).then((response) => {
        if (response.status === 401) {
          throw new Error(
            "Authentication failed (401). Refresh test1username/test1password credentials.",
          );
        }

        expect(response.status).to.be.oneOf([200, 201]);
        expect(response.body).to.have.property("id");

        const confirmationId = response.body.confirmationId || response.body.id;
        confirmationIds.push(confirmationId);
        cy.log(`✅ [${i + 1}/${SUBMISSION_COUNT}] Created: ${confirmationId}`);
      });
    });

    // cy.then() executes after all queued cy.request() calls complete
    cy.then(() => {
      expect(confirmationIds).to.have.length(SUBMISSION_COUNT);

      cy.writeFile("cypress/scripts/bulk-submission-ids.json", {
        submissionIds: confirmationIds,
        count: confirmationIds.length,
        createdAt: new Date().toISOString(),
      });

      cy.log(`✅ Wrote ${confirmationIds.length} IDs to bulk-submission-ids.json`);
    });
  });
});
