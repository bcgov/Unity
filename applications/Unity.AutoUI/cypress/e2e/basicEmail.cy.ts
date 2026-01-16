import { PageFactory } from "../utilities/PageFactory";

describe("Send an email", () => {
  const loginPage = PageFactory.getLoginPage();
  const navigationPage = PageFactory.getNavigationPage();
  const emailsPage = PageFactory.getEmailsPage();

  const TEST_EMAIL_TO = "grantmanagementsupport@gov.bc.ca";
  const TEST_EMAIL_CC = "UnitySupport@gov.bc.ca";
  const TEST_EMAIL_BCC = "UNITYSUP@Victoria1.gov.bc.ca";
  const TEMPLATE_NAME = "Test Case 1";
  const STANDARD_TIMEOUT = 20000;

  const now = new Date();
  const timestamp =
    now.getFullYear() +
    "-" +
    String(now.getMonth() + 1).padStart(2, "0") +
    "-" +
    String(now.getDate()).padStart(2, "0") +
    " " +
    String(now.getHours()).padStart(2, "0") +
    ":" +
    String(now.getMinutes()).padStart(2, "0") +
    ":" +
    String(now.getSeconds()).padStart(2, "0");

  const TEST_EMAIL_SUBJECT = `Smoke Test Email ${timestamp}`;

  it("Login", () => {
    loginPage.quickLogin();
  });

  it("Switch to Default Grants Program if available", () => {
    navigationPage.switchToTenantIfAvailable("Default Grants Program");
  });

  it("Handle IDIR if required", () => {
    cy.get("body").then(($body) => {
      if ($body.find("#social-idir").length > 0) {
        cy.get("#social-idir").should("be.visible").click();
      }
    });

    cy.location("pathname", { timeout: 30000 }).should(
      "include",
      "/GrantApplications"
    );
  });

  it("Open an application from the list", () => {
    cy.url().should("include", "/GrantApplications");

    cy.get(
      '#GrantApplicationsTable tbody a[href^="/GrantApplications/Details?ApplicationId="]',
      { timeout: STANDARD_TIMEOUT }
    )
      .should("have.length.greaterThan", 0)
      .first()
      .click();

    cy.url().should("include", "/GrantApplications/Details");
  });

  it("Open Emails tab", () => {
    emailsPage.openEmailsTab(STANDARD_TIMEOUT);
  });

  it("Open New Email form", () => {
    emailsPage.clickNewEmail(STANDARD_TIMEOUT);
  });

  it("Select Email Template", () => {
    emailsPage.selectTemplate(TEMPLATE_NAME, STANDARD_TIMEOUT);
  });

  it("Set Email To address", () => {
    emailsPage.setEmailTo(TEST_EMAIL_TO, STANDARD_TIMEOUT);
  });

  it("Set Email CC address", () => {
    emailsPage.setEmailCc(TEST_EMAIL_CC, STANDARD_TIMEOUT);
  });

  it("Set Email BCC address", () => {
    emailsPage.setEmailBcc(TEST_EMAIL_BCC, STANDARD_TIMEOUT);
  });

  it("Set Email Subject", () => {
    emailsPage.setEmailSubject(TEST_EMAIL_SUBJECT, STANDARD_TIMEOUT);
  });

  it("Save the email", () => {
    emailsPage.saveEmail(STANDARD_TIMEOUT);
  });

  it("Select saved email from Email History", () => {
    emailsPage.selectEmailFromHistory(TEST_EMAIL_SUBJECT, STANDARD_TIMEOUT);
  });

  it("Send the email", () => {
    emailsPage.sendEmail(STANDARD_TIMEOUT);
  });

  it("Confirm send email in modal", () => {
    emailsPage.confirmSendEmail(STANDARD_TIMEOUT);
  });

  it("Verify Logout", () => {
    loginPage.quickLogout();
  });
});
