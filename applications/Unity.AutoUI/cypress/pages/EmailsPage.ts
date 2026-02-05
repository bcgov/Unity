import { BasePage } from "./BasePage";

/**
 * EmailsPage - Page Object for Email functionality within Application Details
 */
export class EmailsPage extends BasePage {
  private readonly selectors = {
    emailsTab: "#emails-tab",
    emailsHeading: "Emails",
    emailHistoryHeading: "Email History",
    newEmailButton: "#btn-new-email",
    emailToLabel: "Email To",

    // Email form fields
    templateDropdown: "#template",
    emailToField: "#EmailTo",
    emailCcField: "#EmailCC",
    emailBccField: "#EmailBCC",
    emailSubjectField: "#EmailSubject",

    // Action buttons
    saveButton: "#btn-save",
    sendButton: "#btn-send",
    confirmSendButton: "#btn-confirm-send",

    // Modal
    modalContent: "#modal-content",
    confirmationMessage: "Are you sure you want to send this email?",

    // Email history
    emailHistoryTable: "td.data-table-header",
  };

  constructor() {
    super();
  }

  /**
   * Open the Emails tab
   */
  openEmailsTab(timeout: number = 20000): void {
    cy.get(this.selectors.emailsTab, { timeout })
      .should("exist")
      .should("be.visible")
      .click();

    cy.contains(this.selectors.emailsHeading, { timeout }).should("exist");
    cy.contains(this.selectors.emailHistoryHeading, { timeout }).should(
      "exist",
    );
  }

  /**
   * Click New Email button
   */
  clickNewEmail(timeout: number = 20000): void {
    cy.get(this.selectors.newEmailButton, { timeout })
      .should("exist")
      .should("be.visible")
      .click();

    cy.contains(this.selectors.emailToLabel, { timeout }).should("exist");
  }

  /**
   * Select email template
   */
  selectTemplate(templateName: string, timeout: number = 20000): void {
    cy.intercept("GET", "/api/app/template/*/template-by-id").as(
      "loadTemplate",
    );

    cy.get(this.selectors.templateDropdown, { timeout })
      .should("exist")
      .should("be.visible")
      .select(templateName);

    cy.wait("@loadTemplate", { timeout });

    cy.get(this.selectors.templateDropdown)
      .find("option:selected")
      .should("have.text", templateName);
  }

  /**
   * Set Email To address
   */
  setEmailTo(email: string, timeout: number = 20000): void {
    cy.get(this.selectors.emailToField, { timeout })
      .should("exist")
      .should("be.visible")
      .clear()
      .type(email);

    cy.get(this.selectors.emailToField).should("have.value", email);
  }

  /**
   * Set Email CC address
   */
  setEmailCc(email: string, timeout: number = 20000): void {
    cy.get(this.selectors.emailCcField, { timeout })
      .should("exist")
      .should("be.visible")
      .clear()
      .type(email);

    cy.get(this.selectors.emailCcField).should("have.value", email);
  }

  /**
   * Set Email BCC address
   */
  setEmailBcc(email: string, timeout: number = 20000): void {
    cy.get(this.selectors.emailBccField, { timeout })
      .should("exist")
      .should("be.visible")
      .clear()
      .type(email);

    cy.get(this.selectors.emailBccField).should("have.value", email);
  }

  /**
   * Set Email Subject
   */
  setEmailSubject(subject: string, timeout: number = 20000): void {
    cy.get(this.selectors.emailSubjectField, { timeout })
      .should("exist")
      .should("be.visible")
      .clear()
      .type(subject);

    cy.get(this.selectors.emailSubjectField).should("have.value", subject);
  }

  /**
   * Save the email
   */
  saveEmail(timeout: number = 20000): void {
    cy.get(this.selectors.saveButton, { timeout })
      .should("exist")
      .should("be.visible")
      .click();

    cy.get(this.selectors.newEmailButton, { timeout }).should("be.visible");
  }

  /**
   * Select saved email from history by subject
   */
  selectEmailFromHistory(subject: string, timeout: number = 20000): void {
    cy.get("body", { timeout }).then(($body) => {
      const historyTableById = $body.find("#EmailHistoryTable");
      if (historyTableById.length > 0) {
        cy.get("#EmailHistoryTable", { timeout })
          .scrollIntoView()
          .should("exist")
          .within(() => {
            cy.contains("td", subject, { timeout }).should("exist").click();
          });
        return;
      }

      // Fallback: find the subject anywhere in a TD
      cy.contains("td", subject, { timeout }).should("exist").click();
    });

    cy.get(this.selectors.emailToField, { timeout }).should("be.visible");
    cy.get(this.selectors.emailCcField).should("be.visible");
    cy.get(this.selectors.emailBccField).should("be.visible");
    cy.get(this.selectors.emailSubjectField).should("be.visible");
    cy.get(this.selectors.sendButton).should("exist");
    cy.get(this.selectors.saveButton).should("exist");
  }

  /**
   * Send the email
   */
  sendEmail(timeout: number = 20000): void {
    cy.get(this.selectors.sendButton, { timeout })
      .should("exist")
      .should("be.visible")
      .should("not.be.disabled")
      .click();
  }

  /**
   * Confirm sending email in modal (handles both Bootstrap and SweetAlert2)
   */
  confirmSendEmail(timeout: number = 20000): void {
    // Wait until either a bootstrap modal is shown, or SweetAlert container appears, or confirm button exists
    cy.get("body", { timeout }).should(($b) => {
      const hasBootstrapShownModal = $b.find(".modal.show").length > 0;
      const hasSwal = $b.find(".swal2-container").length > 0;
      const hasConfirmBtn = $b.find("#btn-confirm-send").length > 0;
      expect(hasBootstrapShownModal || hasSwal || hasConfirmBtn).to.eq(true);
    });

    cy.get("body", { timeout }).then(($b) => {
      const hasSwal = $b.find(".swal2-container").length > 0;
      if (hasSwal) {
        // SweetAlert2 style
        cy.get(".swal2-container", { timeout }).should("be.visible");
        cy.contains(".swal2-container", "Are you sure", { timeout }).should(
          "exist",
        );

        if ($b.find(".swal2-confirm").length > 0) {
          cy.get(".swal2-confirm", { timeout }).should("be.visible").click();
        } else {
          cy.contains(".swal2-container button", "Yes", { timeout }).click();
        }
        return;
      }

      const hasBootstrapShownModal = $b.find(".modal.show").length > 0;
      if (hasBootstrapShownModal) {
        // Bootstrap modal
        cy.get(".modal.show", { timeout })
          .should("be.visible")
          .within(() => {
            cy.contains("Are you sure you want to send this email?", {
              timeout,
            }).should("exist");

            if (Cypress.$("#btn-confirm-send").length > 0) {
              cy.get("#btn-confirm-send", { timeout })
                .should("exist")
                .should("be.visible")
                .click();
            } else {
              cy.contains("button", "Confirm", { timeout }).click();
            }
          });
        return;
      }

      // Last resort: confirm button exists but modal might not be "visible"
      cy.get("#btn-confirm-send", { timeout })
        .should("exist")
        .click({ force: true });
    });
  }

  /**
   * Create and send a complete email
   */
  createAndSendEmail(
    templateName: string,
    to: string,
    cc: string,
    bcc: string,
    subject: string,
    timeout: number = 20000,
  ): void {
    this.clickNewEmail(timeout);
    this.selectTemplate(templateName, timeout);
    this.setEmailTo(to, timeout);
    this.setEmailCc(cc, timeout);
    this.setEmailBcc(bcc, timeout);
    this.setEmailSubject(subject, timeout);
    this.saveEmail(timeout);
    this.selectEmailFromHistory(subject, timeout);
    this.sendEmail(timeout);
    this.confirmSendEmail(timeout);
  }
}
