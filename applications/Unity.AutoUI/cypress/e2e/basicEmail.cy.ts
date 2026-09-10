// cypress/e2e/basicEmail.cy.ts
import { LoginPageInstance, NavigationPageInstance } from "../utilities";

describe("Send an email", () => {
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
  const loginPage = LoginPageInstance();
  const navPage = NavigationPageInstance();

  // The "Template Applied" swal2 dialog (see "Select Email Template") fires
  // asynchronously and can land after that test has already finished waiting
  // for it, leaking into whichever test runs next and blocking every field
  // underneath it. Cheap and idempotent — call at the top of each subsequent
  // test to mop up a straggler if one shows up.
  function dismissStrayModalIfPresent() {
    cy.get("body", { timeout: STANDARD_TIMEOUT }).then(($body) => {
      const modal = $body.find(".swal2-popup, .modal.show");
      if (modal.length === 0) {
        return;
      }

      const dismissButton = modal
        .find("button")
        .filter((_, element) => {
          return /^(apply template|confirm|apply|ok)$/i.test(
            (element.textContent || "").trim(),
          );
        })
        .first();

      if (dismissButton.length > 0) {
        cy.wrap(dismissButton).click({ force: true });
      }
    });
  }

  function openSavedEmailFromHistoryBySubject(subject: string) {
    cy.get("body", { timeout: STANDARD_TIMEOUT }).then(($body) => {
      const historyTableById = $body.find("#EmailHistoryTable");
      if (historyTableById.length > 0) {
        cy.get("#EmailHistoryTable", { timeout: STANDARD_TIMEOUT })
          .scrollIntoView()
          .should("be.visible")
          .within(() => {
            cy.contains("td", subject, { timeout: STANDARD_TIMEOUT })
              .should("exist")
              .click();
          });
        return;
      }

      cy.contains("td", subject, { timeout: STANDARD_TIMEOUT })
        .should("exist")
        .click();
    });
  }

  function confirmSendDialogIfPresent() {
    cy.get("body", { timeout: STANDARD_TIMEOUT }).should(($b) => {
      const hasBootstrapShownModal = $b.find(".modal.show").length > 0;
      const hasSwal = $b.find(".swal2-container").length > 0;
      const hasConfirmBtn = $b.find("#btn-confirm-send").length > 0;
      expect(hasBootstrapShownModal || hasSwal || hasConfirmBtn).to.eq(true);
    });

    cy.get("body", { timeout: STANDARD_TIMEOUT }).then(($b) => {
      const hasSwal = $b.find(".swal2-container").length > 0;
      if (hasSwal) {
        cy.get(".swal2-container", { timeout: STANDARD_TIMEOUT }).should(
          "be.visible",
        );
        cy.contains(".swal2-container", "Are you sure", {
          timeout: STANDARD_TIMEOUT,
        }).should("exist");

        if ($b.find(".swal2-confirm").length > 0) {
          cy.get(".swal2-confirm", { timeout: STANDARD_TIMEOUT })
            .should("be.visible")
            .click();
        } else {
          cy.contains(".swal2-container button", "Yes", {
            timeout: STANDARD_TIMEOUT,
          }).click();
        }
        return;
      }

      const hasBootstrapShownModal = $b.find(".modal.show").length > 0;
      if (hasBootstrapShownModal) {
        cy.get(".modal.show", { timeout: STANDARD_TIMEOUT })
          .should("be.visible")
          .within(() => {
            cy.contains("Are you sure you want to send this email?", {
              timeout: STANDARD_TIMEOUT,
            }).should("exist");

            if (Cypress.$("#btn-confirm-send").length > 0) {
              cy.get("#btn-confirm-send", { timeout: STANDARD_TIMEOUT })
                .should("exist")
                .should("be.visible")
                .click();
            } else {
              cy.contains("button", "Confirm", {
                timeout: STANDARD_TIMEOUT,
              }).click();
            }
          });
        return;
      }

      cy.get("#btn-confirm-send", { timeout: STANDARD_TIMEOUT })
        .should("exist")
        .click({ force: true });
    });
  }

  it("Login", () => {
    loginPage.login();
    loginPage.verifyOnGrantApplications();
  });

  it("Switch to Default Grants Program if available", () => {
    navPage.switchToDefaultGrantsProgramIfAvailable();
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
    cy.get("body").then(($body) => {
      if ($body.find(".swal2-container").length > 0) {
        cy.get(".swal2-container").then(($swal) => {
          if ($swal.find(".swal2-close").length > 0) {
            cy.get(".swal2-close").click({ force: true });
          } else if ($swal.find(".swal2-confirm").length > 0) {
            cy.get(".swal2-confirm").click({ force: true });
          } else {
            cy.get("body").type("{esc}", { force: true });
          }
        });
        cy.get(".swal2-container", { timeout: STANDARD_TIMEOUT }).should(
          "not.exist",
        );
      }
    });

    cy.get("#emails-tab", { timeout: STANDARD_TIMEOUT })
      .should("be.visible")
      .click();

    cy.get("#emails-tab", { timeout: STANDARD_TIMEOUT }).should(
      "have.class",
      "active",
    );
    cy.contains("Email History", { timeout: STANDARD_TIMEOUT }).should("exist");
  });

  it("Open New Email form", () => {
    cy.get("#btn-new-email", { timeout: STANDARD_TIMEOUT })
      .should("be.visible")
      .click();

    cy.get("#EmailTo", { timeout: STANDARD_TIMEOUT }).should("be.visible");
  });

  it("Select Email Template", () => {
    // UAT/PROD are still on the pre-redesign composer (#template, no
    // confirmation dialogs); DEV/TEST have the newer TinyMCE toolbar select
    // (no id, only this title attribute) plus a two-step swal2 confirmation.
    // Match either selector so this spec works across all four environments.
    cy.get('select[title="Select a template to apply"], #template', {
      timeout: STANDARD_TIMEOUT,
    })
      .should("be.visible")
      .select(TEMPLATE_NAME, { force: true });

    // Only the newer UI pops the "Apply Template?" / "Template Applied"
    // swal2 confirmations — skip them if they don't show up.
    cy.get("body", { timeout: STANDARD_TIMEOUT }).then(($body) => {
      if ($body.find(".swal2-popup").length > 0) {
        cy.contains(".swal2-popup button", "Apply Template", {
          timeout: STANDARD_TIMEOUT,
        }).click();

        // Once its async content fetch resolves, the app shows a second,
        // separate "Template Applied" success dialog (OK button only).
        cy.contains(".swal2-popup button", "OK", {
          timeout: STANDARD_TIMEOUT,
        }).click();
      }
    });

    // #EmailBody is a hidden textarea backing the rich-text editor.
    // Template selection populates the visible RTE but does not auto-sync
    // the backing field — trigger the change manually if still empty.
    cy.get("#EmailBody", { timeout: STANDARD_TIMEOUT }).then(($el) => {
      if (($el.val() as string).trim() === "") {
        cy.wrap($el)
          .invoke("val", "Test email body")
          .trigger("change", { force: true });
      }
    });
  });

  it("Set Email To address", () => {
    dismissStrayModalIfPresent();

    cy.get("#EmailTo", { timeout: STANDARD_TIMEOUT })
      .should("be.visible")
      .clear({ force: true })
      .type(TEST_EMAIL_TO, { force: true });

    cy.get("#EmailTo").should("have.value", TEST_EMAIL_TO);
  });

  it("Set Email CC address", () => {
    dismissStrayModalIfPresent();

    cy.get("#EmailCC", { timeout: STANDARD_TIMEOUT })
      .should("be.visible")
      .clear({ force: true })
      .type(TEST_EMAIL_CC, { force: true });

    cy.get("#EmailCC").should("have.value", TEST_EMAIL_CC);
  });

  it("Set Email BCC address", () => {
    dismissStrayModalIfPresent();

    // The BCC row (#bcc-input-row) is hidden until the BCC toggle is clicked.
    cy.get("body", { timeout: STANDARD_TIMEOUT }).then(($body) => {
      const bccRow = $body.find("#bcc-input-row");
      if (bccRow.length > 0 && !Cypress.$(bccRow[0]).is(":visible")) {
        cy.get("#btn-show-bcc").click();
      }
    });

    cy.get("#EmailBCC", { timeout: STANDARD_TIMEOUT })
      .should("be.visible")
      .clear({ force: true })
      .type(TEST_EMAIL_BCC, { force: true });

    cy.get("#EmailBCC").should("have.value", TEST_EMAIL_BCC);
  });

  it("Set Email Subject", () => {
    dismissStrayModalIfPresent();

    cy.get("#EmailSubject", { timeout: STANDARD_TIMEOUT })
      .should("be.visible")
      .clear({ force: true })
      .type(TEST_EMAIL_SUBJECT, { force: true });

    cy.get("#EmailSubject").should("have.value", TEST_EMAIL_SUBJECT);
  });

  it("Save the email", () => {
    dismissStrayModalIfPresent();

    cy.get("#btn-save-top, #btn-save", { timeout: STANDARD_TIMEOUT })
      .scrollIntoView()
      .should("be.visible")
      .click({ force: true });

    cy.contains("#EmailHistoryTable td", TEST_EMAIL_SUBJECT, {
      timeout: STANDARD_TIMEOUT,
    }).should("exist");
  });

  it("Select saved email from Email History", () => {
    dismissStrayModalIfPresent();
    openSavedEmailFromHistoryBySubject(TEST_EMAIL_SUBJECT);

    // Clicking the history row scrolled the table into view, not the
    // composer above it — scroll back up before checking field visibility.
    cy.get("#EmailForm", { timeout: STANDARD_TIMEOUT }).scrollIntoView();

    cy.get("#EmailTo", { timeout: STANDARD_TIMEOUT }).should("be.visible");
    cy.get("#EmailCC").should("be.visible");
    cy.get("#EmailBCC").should("be.visible");
    cy.get("#EmailSubject").should("be.visible");

    cy.get("#btn-send-top, #btn-send", { timeout: STANDARD_TIMEOUT }).should("exist");
    cy.get("#btn-save-top, #btn-save", { timeout: STANDARD_TIMEOUT }).should("exist");
  });

  it("Send the email", () => {
    dismissStrayModalIfPresent();

    cy.get("#btn-send-top, #btn-send", { timeout: STANDARD_TIMEOUT })
      .scrollIntoView()
      .should("be.visible")
      .should("not.be.disabled")
      .click({ force: true });
  });

  it("Confirm send email in dialog", () => {
    confirmSendDialogIfPresent();
  });

  it("Verify Logout", () => {
    cy.logout();
  });
});
