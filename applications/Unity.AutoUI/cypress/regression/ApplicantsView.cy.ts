/// <reference types="cypress" />

import { PageFactory } from "../utilities/PageFactory";
import { loginIfNeeded } from "../support/auth";

const isProd =
  (Cypress.env("CHEFS_ENV") || "").toLowerCase() === "prod" ||
  (Cypress.env("environment") || "").toLowerCase() === "prod";

const APPLICANT_NAME = Cypress.env("applicantName") || "velangtest2";

const LEFT_TABS = [
  "Applicant Info",
  "Contacts",
  "Addresses",
  "Submissions",
  "History",
  "Payments",
] as const;

const RIGHT_TABS = [
  "Details",
  "Email",
  "Comments",
  "Links",
  "Attachments",
  "History",
] as const;

(isProd ? describe.skip : describe)("Applicants View Regression", () => {
  before(() => {
    // Reuse authenticated session and land in the app.
    loginIfNeeded();
  });

  it("validates status actions, left tabs, and right pane tabs", () => {
    // Start each run from a clean page-factory cache.
    PageFactory.clearCache();

    const applicantsViewPage = PageFactory.getApplicantsViewPage();

    applicantsViewPage
      .visitApplicantsList()
      .searchAndOpenApplicant(APPLICANT_NAME)
      .openStatusActions()
      .verifyStatusActionOptions(["Active", "Inactive"])
      .closeOpenMenus();

    // Validate each main details tab renders expected content.
    LEFT_TABS.forEach((tabName) => {
      applicantsViewPage.openLeftTab(tabName).verifyLeftTabContent(tabName);
    });

    // Validate each right pane tab and its core content.
    RIGHT_TABS.forEach((tabName) => {
      applicantsViewPage.openRightTab(tabName).verifyRightTabContent(tabName);
    });
  });
});
