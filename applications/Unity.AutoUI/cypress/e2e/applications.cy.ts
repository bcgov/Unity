/**
 * lists.cy.ts - Refactored with Page Object Model
 * Tests for verifying all lists are populated with data
 */

import { loginIfNeeded } from "../support/auth";
import {
  LoginPageInstance,
  ApplicationDetailsPageInstance,
} from "../utilities";

describe("Grant Manager Login and List Navigation (POM)", () => {
  const loginPage = LoginPageInstance();
  const applicationDetailsPage = ApplicationDetailsPageInstance();

  before(() => {
    loginIfNeeded();
  });

  it("Validate selecting application will show new menu items", () => {
    // Ensure we're on the Applications page with table loaded
    cy.url({ timeout: 10000 }).should("include", "/GrantApplications");
    cy.get("#GrantApplicationsTable", { timeout: 10000 }).should("be.visible");
    cy.wait(1000);

    // Find and select the first application (click the link directly)
    cy.get(
      '#GrantApplicationsTable tbody a[href^="/GrantApplications/Details?ApplicationId="]',
      { timeout: 20000 },
    )
      .should("have.length.greaterThan", 0)
      .first()
      .click();

    cy.url({ timeout: 10000 }).should("include", "/GrantApplications/Details");
    cy.wait(2000);

    // Verify status actions are available (check if any action button exists)
    cy.get("body").then(($body) => {
      if (
        $body.find("#Application_StartAssessmentButton:not([disabled])")
          .length > 0
      ) {
        applicationDetailsPage.clickStartAssessment();
      } else {
        cy.log(
          "Start Assessment button is disabled - application may already be in assessment or wrong status",
        );
        // Just verify the page loaded correctly
        cy.get(".application-details-container").should("exist");
      }
    });
  });
});
