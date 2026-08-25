/// <reference types="cypress" />

export {};

/**
 * CHEFS Form Submission Seeder (standalone entry point)
 *
 * Thin wrapper around cy.seedApprovalFlowSubmission() so `npm run test:seed`
 * / `npm run test:approval-flow` can still seed a submission as a separate
 * step before ApprovalFlow.cy.ts runs. The actual seeding logic lives in
 * cypress/support/commands.ts so ApprovalFlow.cy.ts can also call it
 * directly as a fallback when no existing submission matches its search
 * criteria, without requiring this spec to run first.
 *
 * Configuration files:
 * - cypress/scripts/chefs-submission-payload.json  - form submission data
 * - cypress/scripts/chefs-api-config.json          - API config and headers
 */

const isProd =
  (
    Cypress.env("CHEFS_ENV") ||
    Cypress.env("environment") ||
    ""
  ).toLowerCase() === "prod";

(isProd ? describe.skip : describe)("CHEFS Approval Flow Seeder", () => {
  it("Create approval flow submission", () => {
    cy.seedApprovalFlowSubmission();
  });
});
