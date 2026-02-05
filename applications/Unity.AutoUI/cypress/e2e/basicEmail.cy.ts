import { PageFactory } from "../utilities/PageFactory";

describe("Send an email", () => {
  const loginPage = PageFactory.getLoginPage();
  const applicationsPage = PageFactory.getApplicationsPage();
  const emailsPage = PageFactory.getEmailsPage();

  const TEST_EMAIL_TO = Cypress.env("TEST_EMAIL_TO") as string;
  const TEST_EMAIL_CC = Cypress.env("TEST_EMAIL_CC") as string;
  const TEST_EMAIL_BCC = Cypress.env("TEST_EMAIL_BCC") as string;
  const TEMPLATE_NAME = "Test Case 1";
  const STANDARD_TIMEOUT = 20000;

  // Only suppress the noisy ResizeObserver error that Unity throws in TEST.
  // Everything else should still fail the test.
  Cypress.on("uncaught:exception", (err) => {
    const msg = err && err.message ? err.message : "";
    if (msg.indexOf("ResizeObserver loop limit exceeded") >= 0) {
      return false;
    }
    return true;
  });

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

  function switchToDefaultGrantsProgramIfAvailable() {
    cy.get("body").then(($body) => {
      const hasUserInitials = $body.find(".unity-user-initials").length > 0;

      if (!hasUserInitials) {
        cy.log("Skipping tenant switch: no user initials menu found");
        return;
      }

      cy.get(".unity-user-initials").click();

      cy.get("body").then(($body2) => {
        const switchLink = $body2
          .find("#user-dropdown a.dropdown-item")
          .filter((_, el) => {
            return (el.textContent || "").trim() === "Switch Grant Programs";
          });

        if (switchLink.length === 0) {
          cy.log(
            'Skipping tenant switch: "Switch Grant Programs" not present for this user/session',
          );
          cy.get("body").click(0, 0);
          return;
        }

        cy.wrap(switchLink.first()).click();

        cy.url({ timeout: STANDARD_TIMEOUT }).should(
          "include",
          "/GrantPrograms",
        );

        cy.get("#search-grant-programs", { timeout: STANDARD_TIMEOUT })
          .should("be.visible")
          .clear()
          .type("Default Grants Program");

        cy.get("#UserGrantProgramsTable", { timeout: STANDARD_TIMEOUT })
          .should("be.visible")
          .within(() => {
            cy.contains("tbody tr", "Default Grants Program", {
              timeout: STANDARD_TIMEOUT,
            })
              .should("exist")
              .within(() => {
                cy.contains("button", "Select").should("be.enabled").click();
              });
          });

        cy.location("pathname", { timeout: STANDARD_TIMEOUT }).should((p) => {
          expect(
            p.indexOf("/GrantApplications") >= 0 || p.indexOf("/auth/") >= 0,
          ).to.eq(true);
        });
      });
    });
  }

  it("Login", () => {
    loginPage.login();
  });

  it("Switch to Default Grants Program if available", () => {
    switchToDefaultGrantsProgramIfAvailable();
  });

  it("Handle IDIR if required", () => {
    cy.get("body").then(($body) => {
      if ($body.find("#social-idir").length > 0) {
        cy.get("#social-idir").should("be.visible").click();
      }
    });

    cy.location("pathname", { timeout: 30000 }).should(
      "include",
      "/GrantApplications",
    );
  });

  it("Open an application from the list", () => {
    cy.url().should("include", "/GrantApplications");

    cy.get(
      '#GrantApplicationsTable tbody a[href^="/GrantApplications/Details?ApplicationId="]',
      { timeout: STANDARD_TIMEOUT },
    ).should("have.length.greaterThan", 0);

    cy.get(
      '#GrantApplicationsTable tbody a[href^="/GrantApplications/Details?ApplicationId="]',
    )
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

  it("Confirm send email in dialog", () => {
    emailsPage.confirmSendEmail(STANDARD_TIMEOUT);
  });

  it("Verify Logout", () => {
    loginPage.logout();
  });
});
