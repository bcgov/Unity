/// <reference types="cypress" />

export {};

/**
 * CHEFS Form Submission Seeder
 *
 * Creates exactly one submitted form entry in CHEFS via API and writes its
 * confirmation ID to cypress/scripts/last-submission-id.json so that
 * ApprovalFlow.cy.ts can pick it up without a dynamic API lookup.
 *
 * Configuration files:
 * - cypress/scripts/chefs-submission-payload.json  — form submission data
 * - cypress/scripts/chefs-api-config.json          — API config and headers
 */

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
    const bearerToken = trimmed.replace(/^Bearer\s+/i, "").trim();
    if (isJwtLike(bearerToken)) {
      return bearerToken;
    }
  }

  if (isJwtLike(trimmed)) {
    return trimmed;
  }

  try {
    return extractTokenFromValue(JSON.parse(trimmed));
  } catch {
    return "";
  }
}

function extractTokenFromArray(values: unknown[]): string {
  for (const value of values) {
    const token = extractTokenFromValue(value);
    if (token) {
      return token;
    }
  }

  return "";
}

function extractTokenFromObject(value: Record<string, unknown>): string {
  for (const key of TOKEN_PROPERTY_KEYS) {
    const token = extractTokenFromValue(value[key]);
    if (token) {
      return token;
    }
  }

  return extractTokenFromArray(Object.values(value));
}

function extractTokenFromValue(value: unknown): string {
  if (typeof value === "string") {
    return extractTokenFromString(value);
  }

  if (Array.isArray(value)) {
    return extractTokenFromArray(value);
  }

  if (value && typeof value === "object") {
    return extractTokenFromObject(value as Record<string, unknown>);
  }

  return "";
}

function extractTokenFromStorage(win: Window): string {
  const storages = [win.localStorage, win.sessionStorage];

  for (const storage of storages) {
    for (let index = 0; index < storage.length; index += 1) {
      const key = storage.key(index);
      if (!key) {
        continue;
      }

      const value = storage.getItem(key);
      if (!value) {
        continue;
      }

      const token = extractTokenFromValue(value);
      if (token) {
        return token;
      }
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
    const onChefs = hostname === chefsHostname;
    const onBcGovIdentity = hostname.endsWith("gov.bc.ca");

    expect(
      onChefs || onBcGovIdentity,
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

const isProd =
  (Cypress.env("CHEFS_ENV") || Cypress.env("environment") || "").toLowerCase() ===
  "prod";

(isProd ? describe.skip : describe)("CHEFS Approval Flow Seeder", () => {
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
        `Missing CHEFS environment configuration for '${envKey}'`,
      ).to.exist;

      cy.log(`Using environment: ${envKey}`);
      cy.log(`Base URL: ${environment.baseURL}`);
      cy.log(`Form ID: ${environment.formId}`);
      cy.log(`Version ID: ${environment.versionId}`);

      cy.readFile("cypress/scripts/chefs-submission-payload.json").then(
        (payload) => {
          submissionPayload = payload;
          submissionPayload.submission.metadata.origin = environment.baseURL;
          submissionPayload.submission.metadata.referrer = `${environment.baseURL}/app/form/submit?f=${environment.formId}`;

          cy.log(
            `Payload loaded with ${
              Object.keys(payload.submission.data).length
            } data fields`,
          );
          cy.log(`Metadata origin set to: ${environment.baseURL}`);
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
          const resolvedToken = capturedToken || tokenFromStorage;

          expect(
            resolvedToken,
            "Waiting for authenticated CHEFS API token from request or browser storage",
          ).to.not.equal("");

          if (!capturedToken && tokenFromStorage) {
            capturedToken = tokenFromStorage;
          }
        })
        .then(() => {
          authToken = capturedToken;
          cy.log("✅ Auth token captured from CHEFS login");
        });
    });
  });

  // Creates the single submission that ApprovalFlow.cy.ts will process.
  // The confirmation ID is written to last-submission-id.json and consumed
  // by the "Fetch submission ID from API" step in ApprovalFlow.cy.ts.
  it("Create approval flow submission", () => {
    const submissionUrl = `${environment.baseURL}/app/api/v1/forms/${environment.formId}/versions/${environment.versionId}/submissions`;

    cy.log(`Submitting to: ${submissionUrl}`);

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
      cy.log(`Response Status: ${response.status}`);
      cy.log(
        `Response Body: ${JSON.stringify(response.body).substring(0, 200)}...`,
      );

      if (response.status === 401) {
        cy.log("❌ 401 Unauthorized - Token is expired or invalid");
        cy.log("📖 See cypress/scripts/README.md for token refresh instructions");
        throw new Error(
          "Authentication failed (401). Check that test1username/test1password credentials in cypress.env.json are valid and that the CHEFS UI login succeeded during test setup.",
        );
      }

      expect(response.status).to.be.oneOf([200, 201]);
      expect(response.body).to.have.property("id");

      const confirmationId = response.body.confirmationId || response.body.id;
      cy.log(`✅ Submission created with ID: ${response.body.id}`);
      cy.log(`✅ Confirmation ID: ${confirmationId}`);

      cy.writeFile("cypress/scripts/last-submission-id.json", {
        submissionId: confirmationId,
        createdAt: new Date().toISOString(),
      });

      expect(response.body).to.have.property("formVersionId", environment.versionId);

      if (response.body.formId) {
        expect(response.body.formId).to.eq(environment.formId);
      } else {
        cy.log("⚠️ Response doesn't include formId (CHEFS version-dependent)");
      }
    });
  });
});
