/// <reference types="cypress" />

export {};

/**
 * CHEFS Bulk Submission Seeder
 *
 * Creates SUBMISSION_COUNT submitted form entries in CHEFS via API and writes
 * all confirmation IDs to cypress/scripts/bulk-submission-ids.json so that
 * BulkPaymentApproval.cy.ts can process them.
 */

const SUBMISSION_COUNT: number = Number(Cypress.env("SUBMISSION_COUNT") || 10);

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
    if (bearerToken) {
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

function completeChefsLogin(
  environment: ChefsEnvironment,
  timeout: number,
): void {
  const chefsHostname = getChefsHostname(environment.baseURL);

  cy.visit(`${environment.baseURL}/app`);

  cy.window({ timeout }).then((win) => {
    const existingToken = extractTokenFromStorage(win);
    if (existingToken) {
      cy.log("CHEFS token already present in browser storage");
      return;
    }

    cy.contains("button, a, [role='button']", /LOG\s*IN|LOGIN/i, {
      timeout,
    })
      .first()
      .click({ force: true });

    cy.get("body", { timeout }).then(($body) => {
      if (
        $body.find(
          "button:contains('IDIR'), a:contains('IDIR'), [role='button']:contains('IDIR')",
        ).length
      ) {
        cy.contains("button, a, [role='button']", /IDIR/i, { timeout })
          .first()
          .click({ force: true });
      }
    });
  });

  waitForIdentityRedirectOrAuthenticatedChefsPage(environment.baseURL, timeout);

  cy.location("hostname", { timeout }).then((currentHostname) => {
    if (currentHostname === chefsHostname) {
      cy.log("CHEFS session appears authenticated");
      return;
    }

    const usernameSelector =
      "input#user, input[name='user'], input[name='username'], input[type='text']";
    const passwordSelector =
      "input#password, input[name='password'], input[type='password']";

    cy.get("body", { timeout }).then(($identityBody) => {
      const hasUsernameField = $identityBody.find(usernameSelector).length > 0;
      const hasPasswordField = $identityBody.find(passwordSelector).length > 0;

      if (!hasUsernameField || !hasPasswordField) {
        return;
      }

      cy.get(
        "input#user:visible, input[name='user']:visible, input[name='username']:visible, input[type='text']:visible",
        { timeout },
      )
        .first()
        .clear()
        .type(Cypress.env("test1username"), { log: false });

      cy.get(
        "input#password:visible, input[name='password']:visible, input[type='password']:visible",
        { timeout },
      )
        .first()
        .clear()
        .type(Cypress.env("test1password"), { log: false });

      cy.contains("button, input[type='submit']", /continue|log\s*in/i, {
        timeout,
      })
        .first()
        .click({ force: true });
    });
  });

  cy.location("hostname", { timeout }).should("eq", chefsHostname);
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

      expect(environment, `Missing CHEFS environment config for '${envKey}'`).to
        .exist;

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
      let tokenSource = "unknown";

      cy.intercept("POST", "**/protocol/openid-connect/token", (req) => {
        req.continue((res) => {
          if (capturedToken) {
            return;
          }

          const responseToken = extractTokenFromValue(res.body);
          if (!responseToken) {
            return;
          }

          capturedToken = responseToken;
          try {
            tokenSource = `intercept:token-endpoint ${new URL(req.url).hostname}`;
          } catch {
            tokenSource = "intercept:token-endpoint";
          }
        });
      }).as("oidcTokenEndpoint");

      const expectedChefsHost = getChefsHostname(environment.baseURL);

      cy.intercept("**/*", (req) => {
        const authHeader = req.headers.authorization as string;
        if (!authHeader || capturedToken) {
          return;
        }

        try {
          const parsed = new URL(req.url);
          const isChefsApiRequest =
            parsed.hostname === expectedChefsHost &&
            parsed.pathname.startsWith("/app/api/v1/");

          if (!isChefsApiRequest) {
            return;
          }

          capturedToken = authHeader.replace(/^Bearer\s+/i, "");
          tokenSource = `intercept:${req.method} ${parsed.hostname}${parsed.pathname}`;
        } catch {
          // Ignore malformed URLs from non-network shim events.
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
            "Waiting for authenticated CHEFS API token from token endpoint, API request, or browser storage",
          ).to.not.equal("");

          if (!capturedToken && tokenFromStorage) {
            capturedToken = tokenFromStorage;
            tokenSource = "storage";
          }
        })
        .then(() => {
          authToken = capturedToken;
          cy.log(
            `Auth token captured from runtime CHEFS login (${tokenSource})`,
          );
        });
    });
  });

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
        cy.log(`Created [${i + 1}/${SUBMISSION_COUNT}]: ${confirmationId}`);
      });
    });

    cy.then(() => {
      expect(confirmationIds).to.have.length(SUBMISSION_COUNT);

      cy.writeFile("cypress/scripts/bulk-submission-ids.json", {
        submissionIds: confirmationIds,
        count: confirmationIds.length,
        createdAt: new Date().toISOString(),
      });

      cy.log(`Wrote ${confirmationIds.length} IDs to bulk-submission-ids.json`);
    });
  });
});
