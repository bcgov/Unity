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

  const openApplicantDetails = () => {
    PageFactory.clearCache();
    return PageFactory.getApplicantsViewPage()
      .visitApplicantsList()
      .searchAndOpenApplicant(APPLICANT_NAME);
  };

  it("validates status actions and core UI styling", () => {
    openApplicantDetails()
      .openStatusActions()
      .verifyCoreUiStyling()
      .verifyStatusActionOptions(["Active", "Inactive"])
      .closeOpenMenus();
  });

  LEFT_TABS.forEach((tabName) => {
    it(`validates left tab: ${tabName}`, () => {
      openApplicantDetails().openLeftTab(tabName).verifyLeftTabContent(tabName);
    });
  });

  RIGHT_TABS.forEach((tabName) => {
    it(`validates right tab: ${tabName}`, () => {
      openApplicantDetails()
        .openRightTab(tabName)
        .verifyRightTabContent(tabName);
    });
  });
});
