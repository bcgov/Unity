/// <reference types="cypress" />

/**
 * CHEFS Form Submission API Test
 *
 * This test submits a form to CHEFS (Common Hosted Form Service) via API
 * All payloads and configuration are customizable via JSON files:
 * - cypress/scripts/chefs-submission-payload.json - Form submission data
 * - cypress/scripts/chefs-api-config.json - API configuration and headers
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

const isProd = (Cypress.env("CHEFS_ENV") || Cypress.env("environment") || "").toLowerCase() === "prod";

(isProd ? describe.skip : describe)("CHEFS Form Submission API", () => {
  let apiConfig: ChefsApiConfig;
  let submissionPayload: ChefsSubmissionPayload;
  let environment: ChefsEnvironment;
  let authToken: string;
  let createdSubmissionId: string;

  before(() => {
    // Load configuration from scripts directory
    cy.readFile("cypress/scripts/chefs-api-config.json").then((config) => {
      apiConfig = config;

      // Get environment from Cypress env or default to 'test'
      const envKey = (Cypress.env("CHEFS_ENV") || Cypress.env("environment") || "test").toLowerCase();
      environment = config.environments[envKey];

      cy.log(`Using environment: ${envKey}`);
      cy.log(`Base URL: ${environment.baseURL}`);
      cy.log(`Form ID: ${environment.formId}`);
      cy.log(`Version ID: ${environment.versionId}`);

      // Load submission payload and set metadata dynamically from environment
      cy.readFile("cypress/scripts/chefs-submission-payload.json").then(
        (payload) => {
          submissionPayload = payload;
          submissionPayload.submission.metadata.origin = environment.baseURL;
          submissionPayload.submission.metadata.referrer = `${environment.baseURL}/app/form/submit?f=${environment.formId}`;
          cy.log(
            `Payload loaded with ${
              Object.keys(payload.submission.data).length
            } data fields`
          );
          cy.log(`Metadata origin set to: ${environment.baseURL}`);
        }
      );

      // Capture token from ANY authenticated API call — handler fires for every matching request
      let capturedToken = "";
      cy.intercept(`${environment.baseURL}/app/api/v1/**`, (req) => {
        const authHeader = req.headers["authorization"] as string;
        if (authHeader && !capturedToken) {
          capturedToken = authHeader.replace(/^Bearer\s+/i, "");
        }
      }).as("chefsApiCalls");

      // Login to CHEFS via UI using credentials from cypress.env.json
      cy.visit(`${environment.baseURL}/app`);
      cy.get("#app > div > main > header > header > div > div.d-print-none")
        .should("exist")
        .click();
      cy.get(
        "#app > div > main > div.v-container.v-locale--is-ltr.text-center.main > div > div:nth-child(2) > div > button"
      )
        .should("exist")
        .click();
      cy.get("body").then(($body) => {
        if ($body.find("#user").length) {
          cy.get("#user").type(Cypress.env("test1username"), { log: false });
          cy.get("#password").type(Cypress.env("test1password"), { log: false });
          cy.contains("Continue").should("exist").click();
        } else {
          cy.log("Already logged in to CHEFS");
        }
      });

      // Poll until an authenticated API call is intercepted (skips pre-auth calls like /rbac/idps)
      cy.wrap(null, { timeout: 30000 }).should(() => {
        expect(capturedToken, "Waiting for authenticated CHEFS API call").to.not.equal("");
      }).then(() => {
        authToken = capturedToken;
        cy.log("✅ Auth token captured from CHEFS login");
      });
    });
  });

  it("should submit form via CHEFS API", () => {
    // Construct the submission URL
    const submissionUrl = `${environment.baseURL}/app/api/v1/forms/${environment.formId}/versions/${environment.versionId}/submissions`;

    cy.log(`Submitting to: ${submissionUrl}`);

    // Make the API request
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
      failOnStatusCode: false, // Don't fail immediately to capture response
    }).then((response) => {
      // Log response details
      cy.log(`Response Status: ${response.status}`);
      cy.log(
        `Response Body: ${JSON.stringify(response.body).substring(0, 200)}...`
      );

      // Handle 401 Unauthorized (expired/invalid token)
      if (response.status === 401) {
        cy.log("❌ 401 Unauthorized - Token is expired or invalid");
        cy.log(
          "📖 See cypress/scripts/README.md for token refresh instructions"
        );
        throw new Error(
          "Authentication failed (401). Check that test1username/test1password credentials in cypress.env.json are valid and that the CHEFS UI login succeeded during test setup."
        );
      }

      // Assertions
      expect(response.status).to.be.oneOf([200, 201]); // Success status codes
      expect(response.body).to.have.property("id"); // CHEFS returns submission ID

      // Store submission ID for use in the "retrieve submission by ID" test
      if (response.body.id) {
        createdSubmissionId = response.body.id;
        cy.log(`✅ Submission created with ID: ${response.body.id}`);
      }

      // Verify response structure
      expect(response.body).to.have.property(
        "formVersionId",
        environment.versionId
      );
      // Note: formId may not be in response, depends on CHEFS version
      if (response.body.formId) {
        expect(response.body.formId).to.eq(environment.formId);
      } else {
        cy.log("⚠️ Response doesn't include formId (CHEFS version-dependent)");
      }
    });
  });

  it("should submit form with custom data overrides", () => {
    const submissionUrl = `${environment.baseURL}/app/api/v1/forms/${environment.formId}/versions/${environment.versionId}/submissions`;

    // Create a customized payload
    const customPayload = JSON.parse(JSON.stringify(submissionPayload)); // Deep clone

    // Customize specific fields
    const timestamp = new Date().toISOString();
    customPayload.submission.data._ApplicantName = `AutoTest_${Date.now()}`;
    customPayload.submission.data._projectTitle = `Automated Test Project ${timestamp}`;
    customPayload.submission.data._ContactEmail = `autotest_${Date.now()}@example.com`;
    customPayload.submission.data._totalProjectCost = 1000000;
    customPayload.submission.data._fundingRequest = 750000;

    cy.log("Custom fields set:");
    cy.log(`- Applicant: ${customPayload.submission.data._ApplicantName}`);
    cy.log(`- Project: ${customPayload.submission.data._projectTitle}`);
    cy.log(`- Email: ${customPayload.submission.data._ContactEmail}`);

    cy.request({
      method: "POST",
      url: submissionUrl,
      headers: {
        ...apiConfig.headers,
        Authorization: `Bearer ${authToken}`,
        Origin: environment.baseURL,
        Referer: `${environment.baseURL}/app/form/submit?f=${environment.formId}`,
      },
      body: customPayload,
      failOnStatusCode: false,
    }).then((response) => {
      // Handle 401 Unauthorized
      if (response.status === 401) {
        cy.log("❌ 401 Unauthorized - Token is expired or invalid");
        cy.log(
          "📖 See cypress/scripts/README.md for token refresh instructions"
        );
        throw new Error(
          "Authentication failed (401). Check that test1username/test1password credentials in cypress.env.json are valid and that the CHEFS UI login succeeded during test setup."
        );
      }

      expect(response.status).to.be.oneOf([200, 201]);
      expect(response.body).to.have.property("id");

      if (response.body.id) {
        cy.log(`✅ Custom submission created with ID: ${response.body.id}`);
      }
    });
  });

  it("should handle draft submission", () => {
    const submissionUrl = `${environment.baseURL}/app/api/v1/forms/${environment.formId}/versions/${environment.versionId}/submissions`;

    // Create draft submission
    const draftPayload = JSON.parse(JSON.stringify(submissionPayload));
    draftPayload.draft = true; // Mark as draft
    draftPayload.submission.state = "draft"; // Change state to draft
    draftPayload.submission.data._ApplicantName = `Draft_${Date.now()}`;

    cy.log("Submitting as DRAFT");

    cy.request({
      method: "POST",
      url: submissionUrl,
      headers: {
        ...apiConfig.headers,
        Authorization: `Bearer ${authToken}`,
        Origin: environment.baseURL,
        Referer: `${environment.baseURL}/app/form/submit?f=${environment.formId}`,
      },
      body: draftPayload,
      failOnStatusCode: false,
    }).then((response) => {
      // Handle 401 Unauthorized
      if (response.status === 401) {
        cy.log("❌ 401 Unauthorized - Token is expired or invalid");
        cy.log(
          "📖 See cypress/scripts/README.md for token refresh instructions"
        );
        throw new Error(
          "Authentication failed (401). Check that test1username/test1password credentials in cypress.env.json are valid and that the CHEFS UI login succeeded during test setup."
        );
      }

      expect(response.status).to.be.oneOf([200, 201]);
      expect(response.body).to.have.property("id");

      if (response.body.draft !== undefined) {
        expect(response.body.draft).to.be.true;
        cy.log(`✅ Draft submission created with ID: ${response.body.id}`);
      }
    });
  });

  it("should retrieve submission by ID", () => {
    // This test depends on the first test creating a submission
    if (createdSubmissionId) {
      const retrieveUrl = `${environment.baseURL}/app/api/v1/submissions/${createdSubmissionId}`;

      cy.request({
        method: "GET",
        url: retrieveUrl,
        headers: {
          ...apiConfig.headers,
          Authorization: `Bearer ${authToken}`,
        },
        failOnStatusCode: false,
      }).then((response) => {
        // Handle 401 Unauthorized
        if (response.status === 401) {
          cy.log("❌ 401 Unauthorized - CHEFS login credentials may be invalid");
          throw new Error(
            "Authentication failed (401). Check that test1username/test1password credentials in cypress.env.json are valid and that the CHEFS UI login succeeded during test setup."
          );
        }

        expect(response.status).to.eq(200);

        // CHEFS API returns submission in a nested structure
        // Response: { submission: {...}, version: {...}, ... }
        if (response.body.submission) {
          expect(response.body.submission).to.have.property(
            "id",
            createdSubmissionId
          );
          cy.log(`✅ Retrieved submission: ${createdSubmissionId}`);
        } else if (response.body.id) {
          // Some CHEFS versions return id at root level
          expect(response.body.id).to.eq(createdSubmissionId);
          cy.log(`✅ Retrieved submission: ${createdSubmissionId}`);
        } else {
          cy.log("⚠️ Unexpected response structure - logging for debugging");
          cy.log(JSON.stringify(response.body, null, 2));
        }
      });
    } else {
      cy.log("⚠️  Skipping - No submission ID available");
    }
  });

  it("should submit form with file attachment", () => {
    const filePath = `${Cypress.config("projectRoot")}/cypress/fixtures/test-attachment.txt`;

    // Step 1: Upload the file to CHEFS
    cy.task("uploadChefsFile", {
      baseURL: environment.baseURL,
      authToken: authToken,
      filePath: filePath,
    }).then((fileRef: any) => {
      cy.log(`✅ File uploaded: ${JSON.stringify(fileRef)}`);

      // Step 2: Submit form with the file reference in simplefile
      const payloadWithFile = JSON.parse(JSON.stringify(submissionPayload));
      payloadWithFile.submission.data.simplefile = Array.isArray(fileRef) ? fileRef : [fileRef];

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
        body: payloadWithFile,
        failOnStatusCode: false,
      }).then((response) => {
        if (response.status === 401) {
          throw new Error(
            "Authentication failed (401). Check that test1username/test1password credentials in cypress.env.json are valid and that the CHEFS UI login succeeded during test setup."
          );
        }
        expect(response.status).to.be.oneOf([200, 201]);
        expect(response.body).to.have.property("id");
        cy.log(`✅ Submission with attachment created: ${response.body.id}`);
      });
    });
  });

  it("should update submission payload data and save back to file", () => {
    // Example: Modify payload and save it back
    const updatedPayload = JSON.parse(JSON.stringify(submissionPayload));

    // Update fields
    updatedPayload.submission.data._ApplicantName = "UpdatedApplicant";
    updatedPayload.submission.data._projectTitle = "Updated Project Title";

    // Write updated payload back to file
    cy.writeFile(
      "cypress/scripts/chefs-submission-payload-updated.json",
      updatedPayload
    );

    cy.log("✅ Updated payload saved to chefs-submission-payload-updated.json");
  });
});
