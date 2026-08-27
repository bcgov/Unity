/// <reference types="cypress" />

export {};

/**
 * CHEFS Bulk Submission Seeder
 *
 * Creates SUBMISSION_COUNT submitted form entries in CHEFS via the form's
 * API key (Basic Auth) — the same no-browser-login approach used by
 * cy.seedApprovalFlowSubmission() in cypress/support/commands.ts, chosen
 * there specifically to avoid the IDIR/MFA-picker flakiness a UI-driven
 * login carries. Writes all confirmation IDs to
 * cypress/scripts/bulk-submission-ids.json so that BulkPaymentApproval.cy.ts
 * can process them.
 *
 * Requires a `chefsApiKey` value for the current environment, set in
 * cypress/config/{env}.json (gitignored) — the form's API key from CHEFS
 * form management settings.
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

const SUBMISSION_COUNT: number = Number(Cypress.env("SUBMISSION_COUNT") || 10);

const isProd =
  (Cypress.env("CHEFS_ENV") || "").toLowerCase() === "prod" ||
  (Cypress.env("environment") || "").toLowerCase() === "prod";

// Distinct-enough project titles so the seeded submissions don't render as
// identical rows in the Applications list — cycled by index, not meant to be
// an exhaustive content-generation system.
const PROJECT_TITLES = [
  "Maple Ridge Community Resource Development Initiative",
  "Kamloops Renewable Energy Infrastructure Project",
  "Prince George Skills Training Expansion",
  "Nanaimo Waterfront Revitalization Program",
  "Kelowna Agricultural Innovation Hub",
  "Victoria Green Manufacturing Retrofit",
  "Fort St. John Resource Sector Modernization",
  "Chilliwack Small Business Growth Fund Project",
  "Terrace Indigenous Partnership Development",
  "Cranbrook Tourism Infrastructure Enhancement",
];

/** Clone the base payload and vary the fields that make each row identifiable. */
function buildSubmissionPayload(
  base: ChefsSubmissionPayload,
  index: number,
): ChefsSubmissionPayload {
  const payload = JSON.parse(JSON.stringify(base)) as ChefsSubmissionPayload;
  const data = payload.submission.data;

  const fundingRequest = 250000 + index * 25000;
  data._projectTitle = PROJECT_TITLES[index % PROJECT_TITLES.length];
  data._ApplicantName = `${base.submission.data._ApplicantName ?? "Applicant"} ${index + 1}`;
  data._fundingRequest = fundingRequest;
  data._totalProjectCost = Math.round(fundingRequest * 1.6);

  return payload;
}

(isProd ? describe.skip : describe)("CHEFS Bulk Submission Seeder", () => {
  let apiConfig: ChefsApiConfig;
  let submissionPayload: ChefsSubmissionPayload;
  let environment: ChefsEnvironment;
  let apiKey: string;

  before(() => {
    const envKey = (
      Cypress.env("CHEFS_ENV") ||
      Cypress.env("environment") ||
      "test"
    ).toLowerCase();

    apiKey = Cypress.env("chefsApiKey") as string;
    expect(
      apiKey,
      `Missing chefsApiKey for '${envKey}' — set it in cypress/config/${envKey}.json`,
    ).to.exist;

    cy.readFile<ChefsApiConfig>("cypress/scripts/chefs-api-config.json").then(
      (config) => {
        apiConfig = config;
        environment = config.environments[envKey];

        expect(
          environment,
          `Missing CHEFS environment configuration for '${envKey}'`,
        ).to.exist;

        cy.log(`Using environment: ${envKey}`);
        cy.log(`Submission count: ${SUBMISSION_COUNT}`);

        cy.readFile<ChefsSubmissionPayload>(
          "cypress/scripts/chefs-submission-payload.json",
        ).then((payload) => {
          submissionPayload = payload;
          submissionPayload.submission.metadata.origin = environment.baseURL;
          submissionPayload.submission.metadata.referrer = `${environment.baseURL}/app/form/submit?f=${environment.formId}`;
        });
      },
    );
  });

  it(`Create ${SUBMISSION_COUNT} bulk submissions`, () => {
    const confirmationIds: string[] = [];

    Cypress._.times(SUBMISSION_COUNT, (i) => {
      const submissionUrl = `${environment.baseURL}/app/api/v1/forms/${environment.formId}/versions/${environment.versionId}/submissions`;
      const basicCredentials = btoa(`${environment.formId}:${apiKey}`);

      cy.request({
        method: "POST",
        url: submissionUrl,
        headers: {
          ...apiConfig.headers,
          Authorization: `Basic ${basicCredentials}`,
        },
        body: {
          ...buildSubmissionPayload(submissionPayload, i),
          createdBy: `${Cypress.env("test1username")}@idir`,
          updatedBy: `${Cypress.env("test1username")}@idir`,
        },
        failOnStatusCode: false,
      }).then((response) => {
        if (response.status === 401) {
          throw new Error(
            "Authentication failed (401). Check that chefsApiKey is valid for this environment's form.",
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
