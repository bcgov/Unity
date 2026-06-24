/// <reference types="cypress" />

export {};

/**
 * CHEFS Form Submission Seeder
 *
 * Creates exactly one submitted form entry in CHEFS via API and writes its
 * confirmation ID to cypress/scripts/last-submission-id.json so that
 * ApprovalFlow.cy.ts can pick it up without a dynamic API lookup.
 *
 * Reads submission payload from: cypress/scripts/chefs-submission-payload.json
 */

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

const ENVIRONMENTS: Record<string, { baseURL: string; formId: string; versionId: string }> = {
  test: {
    baseURL:   "https://chefs-test.apps.silver.devops.gov.bc.ca",
    formId:    "46e25863-0ead-4aa8-897f-51e45f79e137",
    versionId: "4ef52ead-2cc3-4bdb-a7b7-73be983a7838",
  },
  dev: {
    baseURL:   "https://chefs-dev.apps.silver.devops.gov.bc.ca",
    formId:    "233f47f9-b566-46c3-926a-73d565bf710f",
    versionId: "1e209d6b-46f5-4ddb-bc79-6e04033231cb",
  },
  uat: {
    baseURL:   "https://chefs-test.apps.silver.devops.gov.bc.ca",
    formId:    "f2f45aa7-62c5-49ca-8846-b214e02adb46",
    versionId: "1d4d73ec-00e7-4b57-98c9-49d1e0c7d15b",
  },
};

const ENV_KEY = (
  Cypress.env("CHEFS_ENV") || Cypress.env("environment") || "test"
).toLowerCase();

const { baseURL: BASE_URL, formId: FORM_ID, versionId: VERSION_ID } =
  ENVIRONMENTS[ENV_KEY] ?? ENVIRONMENTS["test"];

function getChefsHostname(): string {
  return new URL(BASE_URL).hostname;
}

function isJwtLike(value: string): boolean {
  return /^[A-Za-z0-9-_]+\.[A-Za-z0-9-_]+\.[A-Za-z0-9-_]+$/.test(value);
}

function extractToken(value: unknown): string {
  if (typeof value === "string") {
    const t = value.trim();
    if (t.toLowerCase().startsWith("bearer ")) {
      const bearer = t.replace(/^Bearer\s+/i, "").trim();
      if (isJwtLike(bearer)) return bearer;
    }
    if (isJwtLike(t)) return t;
    try { return extractToken(JSON.parse(t)); } catch { return ""; }
  }
  if (Array.isArray(value)) {
    for (const v of value) { const tok = extractToken(v); if (tok) return tok; }
  }
  if (value && typeof value === "object") {
    for (const v of Object.values(value as Record<string, unknown>)) {
      const tok = extractToken(v); if (tok) return tok;
    }
  }
  return "";
}

function extractTokenFromStorage(win: Window): string {
  for (const storage of [win.localStorage, win.sessionStorage]) {
    for (let i = 0; i < storage.length; i++) {
      const key = storage.key(i);
      if (!key) continue;
      const raw = storage.getItem(key);
      if (!raw) continue;
      const tok = extractToken(raw);
      if (tok) return tok;
    }
  }
  return "";
}

/**
 * Logs into CHEFS TEST using IDIR credentials.
 *
 * Flow: form URL → CHEFS login page (/app/login?idpHint=idir) → click "IDIR" →
 *       IDIR Keycloak (logontest/sfstest) → enter credentials → back to CHEFS.
 */
function completeChefsIdirLogin(timeout: number): void {
  const chefsHostname = getChefsHostname();

  // Visiting the form URL while unauthenticated → CHEFS redirects to /app/login?idpHint=idir
  cy.visit(`${BASE_URL}/app/form/submit?f=${FORM_ID}`);

  cy.location("pathname", { timeout }).should("include", "login");

  // Click the plain "IDIR" button (not "IDIR MFA")
  cy.contains("button, a", /^IDIR$/i, { timeout }).should("be.visible").click();

  // Wait for redirect to the BC Gov IDIR identity provider page
  cy.location("hostname", { timeout }).should((h) => {
    expect(
      h.includes("logontest") || h.includes("sfstest") || h.includes("loginproxy") || h.includes("idir"),
      `Expected an IDIR identity host, got '${h}'`,
    ).to.be.true;
  });

  // Enter IDIR credentials — logontest7/sfstest7 have #user + #password on one page
  cy.location("hostname", { timeout }).then((h) => {
    if (h === chefsHostname) {
      cy.log("Already authenticated with CHEFS — skipping IDIR credential entry");
      return;
    }

    cy.get("body", { timeout }).then(($body) => {
      if ($body.find("#user").length > 0) {
        cy.get("#user").should("be.visible").clear()
          .type(Cypress.env("test1username") as string, { log: false });
        cy.get("#password").should("be.visible").clear()
          .type(Cypress.env("test1password") as string, { log: false });
        // Submit is <input type="submit"> — cy.contains() (no element filter) matches it
        cy.contains(/^Continue$/i).should("be.visible").click();
      } else if ($body.find("#username").length > 0) {
        cy.get("#username").should("be.visible").clear()
          .type(Cypress.env("test1username") as string, { log: false });
        cy.get("#password").should("be.visible").clear()
          .type(Cypress.env("test1password") as string, { log: false });
        cy.get('[type="submit"]', { timeout }).should("be.visible").click();
      }
    });
  });

  cy.location("hostname", { timeout }).should("eq", chefsHostname);
  cy.log("IDIR login complete — back on CHEFS");
}

const isProd =
  (Cypress.env("CHEFS_ENV") || Cypress.env("environment") || "").toLowerCase() === "prod";

(isProd ? describe.skip : describe)("CHEFS Approval Flow Seeder", () => {
  let authToken = "";

  before(() => {
    const authTimeout = 60000;
    let capturedToken = "";

    cy.intercept(`${BASE_URL}/app/api/v1/**`, (req) => {
      const h = req.headers["authorization"] as string;
      if (h && !capturedToken) capturedToken = h.replace(/^Bearer\s+/i, "").trim();
    }).as("chefsApiCalls");

    completeChefsIdirLogin(authTimeout);

    // Visit form again while authenticated to trigger API calls that carry the token
    cy.visit(`${BASE_URL}/app/form/submit?f=${FORM_ID}`);

    cy.window({ timeout: authTimeout })
      .should((win) => {
        const fromStorage = extractTokenFromStorage(win);
        const resolved = capturedToken || fromStorage;
        expect(resolved, "Waiting for CHEFS IDIR auth token").to.not.equal("");
        if (!capturedToken && fromStorage) capturedToken = fromStorage;
      })
      .then(() => {
        authToken = capturedToken;
        cy.log(`✅ Auth token captured (${authToken.length} chars)`);
      });
  });

  // Creates the single submission that ApprovalFlow.cy.ts will process.
  // Writes the confirmation ID to last-submission-id.json.
  it("Create approval flow submission", () => {
    cy.readFile("cypress/scripts/chefs-submission-payload.json").then(
      (submissionPayload: ChefsSubmissionPayload) => {
        submissionPayload.submission.metadata.origin = BASE_URL;
        submissionPayload.submission.metadata.referrer = `${BASE_URL}/app/form/submit?f=${FORM_ID}`;

        const submissionUrl = `${BASE_URL}/app/api/v1/forms/${FORM_ID}/versions/${VERSION_ID}/submissions`;

        cy.log(`Submitting to: ${submissionUrl}`);

        cy.request({
          method: "POST",
          url: submissionUrl,
          headers: {
            Accept:        "application/json",
            "Content-Type": "application/json",
            Authorization: `Bearer ${authToken}`,
            Origin:        BASE_URL,
            Referer:       `${BASE_URL}/app/form/submit?f=${FORM_ID}`,
          },
          body: submissionPayload,
          failOnStatusCode: false,
        }).then((response) => {
          cy.log(`Response Status: ${response.status}`);
          cy.log(`Response Body: ${JSON.stringify(response.body).substring(0, 200)}...`);

          if (response.status === 401) {
            throw new Error(
              "Authentication failed (401). Check test1username/test1password in cypress.env.json.",
            );
          }

          expect(response.status).to.be.oneOf([200, 201]);
          expect(response.body).to.have.property("id");

          const confirmationId: string = response.body.confirmationId || response.body.id;
          cy.log(`✅ Submission created — confirmationId: ${confirmationId}`);

          cy.writeFile("cypress/scripts/last-submission-id.json", {
            submissionId: confirmationId,
            createdAt:    new Date().toISOString(),
          });
        });
      },
    );
  });
});
