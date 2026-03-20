/// <reference types="cypress" />

import { BasePage } from "./BasePage";

/**
 * ApplicationDetailsRightTabPage - Page Object for the right panel tabs on Application Details
 * Handles: Details, Emails, Comments, Attachments, Links, History tabs
 */
export class ApplicationDetailsRightTabPage extends BasePage {
  private readonly STANDARD_TIMEOUT = 20000;

  // Right panel container
  private readonly container = ".right-card";

  // Tab button selectors
  private readonly tabs = {
    details: "#details-tab",
    emails: "#emails-tab",
    comments: "#comments-tab",
    attachments: "#attachments-tab",
    links: "#links-tab",
    history: "#history-tab",
  };

  // Tab content selectors
  private readonly tabContent = {
    details: "#details",
    emails: "#emails",
    comments: "#comments",
    attachments: "#attachments",
    links: "#links",
    history: "#history",
  };

  // Count badge selectors
  private readonly countBadges = {
    emails: "#application_emails_count",
    comments: "#application_comments_count",
    attachments: "#application_attachment_count",
    links: "#application_links_count",
  };

  // Details tab selectors
  private readonly detailsSection = {
    applicationStatusWidget: "#applicationStatusWidget",
    applicationTagsWidget: "#applicationTagsWidget",
    summaryWidgetArea: "#summaryWidgetArea",
    summaryTable: ".summary-table",
  };

  // Assessment section selectors (in Details tab)
  private readonly assessmentSection = {
    reviewDetails: "#reviewDetails",
    assessmentId: "#AssessmentId",
    financialAnalysis: "#financialAnalysis",
    economicImpact: "#economicImpact",
    inclusiveGrowth: "#inclusiveGrowth",
    cleanGrowth: "#cleanGrowth",
    subTotal: "#subTotal",
    saveAssessmentScoresBtn: "#saveAssessmentScoresBtn",
    recommendationSelect: "#recommendation_select",
    recommendationResetBtn: "#recommendation_reset_btn",
  };

  // Email section selectors
  private readonly emailSection = {
    newEmailBtn: "#btn-new-email",
    emailForm: "#EmailForm",
    templateSelect: "#template",
    emailTo: "#EmailTo",
    emailCC: "#EmailCC",
    emailBCC: "#EmailBCC",
    emailFrom: "#EmailFrom",
    emailSubject: "#EmailSubject",
    emailBody: "#EmailBody",
    saveBtn: "#btn-save",
    sendBtn: "#btn-send",
    cancelBtn: "#btn-cancel-email",
    confirmSendBtn: "#btn-confirm-send",
  };

  // Comments section selectors
  private readonly commentsSection = {
    commentTextArea: "#comments .comment-input",
    addCommentSaveBtn: "#comments .add-comment-save-button",
    addCommentCancelBtn: "#comments .add-comment-cancel-button",
    commentsContainer: "#comments .comments-container",
  };

  // Attachments section selectors
  private readonly attachmentsSection = {
    attachmentsTable: "#ApplicationAttachmentsTable",
    submissionAttachmentsTable: ".submission-attachments-table, [id*='SubmissionAttachments']",
    uploadBtn: "#application_upload_btn",
    uploadInput: "#application_upload",
    addAttachmentsBtn: "button:contains('Add Attachments'), .add-attachments-btn, [id*='addAttachment']",
  };

  // Links section selectors
  private readonly linksSection = {
    linksTable: "#ApplicationLinksTable",
    addLinkBtn: "#addLinkBtn",
  };

  // History section selectors
  private readonly historySection = {
    historyTable: "#ApplicationHistoryTable",
  };

  constructor() {
    super();
  }

  // ============ Tab Navigation Methods ============

  /**
   * Go to Details tab
   */
  goToDetailsTab(): this {
    cy.get(this.tabs.details, { timeout: this.STANDARD_TIMEOUT })
      .should("be.visible")
      .click({ force: true });
    return this;
  }

  /**
   * Go to Emails tab
   */
  goToEmailsTab(): this {
    cy.get(this.tabs.emails, { timeout: this.STANDARD_TIMEOUT })
      .should("be.visible")
      .click({ force: true });
    return this;
  }

  /**
   * Go to Comments tab
   */
  goToCommentsTab(): this {
    cy.get(this.tabs.comments, { timeout: this.STANDARD_TIMEOUT })
      .should("be.visible")
      .click({ force: true });
    return this;
  }

  /**
   * Go to Attachments tab
   */
  goToAttachmentsTab(): this {
    cy.get(this.tabs.attachments, { timeout: this.STANDARD_TIMEOUT })
      .should("be.visible")
      .click({ force: true });
    return this;
  }

  /**
   * Go to Links tab
   */
  goToLinksTab(): this {
    cy.get(this.tabs.links, { timeout: this.STANDARD_TIMEOUT })
      .should("be.visible")
      .click({ force: true });
    return this;
  }

  /**
   * Go to History tab
   */
  goToHistoryTab(): this {
    cy.get(this.tabs.history, { timeout: this.STANDARD_TIMEOUT })
      .should("be.visible")
      .click({ force: true });
    return this;
  }

  /**
   * Verify active tab
   */
  verifyActiveTab(
    tabName: "details" | "emails" | "comments" | "attachments" | "links" | "history"
  ): this {
    cy.get(this.tabs[tabName], { timeout: this.STANDARD_TIMEOUT })
      .should("have.class", "active");
    return this;
  }

  // ============ Count Badge Methods ============

  /**
   * Get emails count
   */
  getEmailsCount(): Cypress.Chainable<number> {
    return cy
      .get(this.countBadges.emails, { timeout: this.STANDARD_TIMEOUT })
      .invoke("text")
      .then((text) => parseInt(text, 10) || 0);
  }

  /**
   * Get comments count
   */
  getCommentsCount(): Cypress.Chainable<number> {
    return cy
      .get(this.countBadges.comments, { timeout: this.STANDARD_TIMEOUT })
      .invoke("text")
      .then((text) => parseInt(text, 10) || 0);
  }

  /**
   * Get attachments count
   */
  getAttachmentsCount(): Cypress.Chainable<number> {
    return cy
      .get(this.countBadges.attachments, { timeout: this.STANDARD_TIMEOUT })
      .invoke("text")
      .then((text) => parseInt(text, 10) || 0);
  }

  /**
   * Get links count
   */
  getLinksCount(): Cypress.Chainable<number> {
    return cy
      .get(this.countBadges.links, { timeout: this.STANDARD_TIMEOUT })
      .invoke("text")
      .then((text) => parseInt(text, 10) || 0);
  }

  // ============ Details Tab Methods ============

  /**
   * Verify application status
   */
  verifyApplicationStatus(expectedStatus: string): this {
    cy.get(this.detailsSection.applicationStatusWidget, { timeout: this.STANDARD_TIMEOUT })
      .should("contain.text", expectedStatus);
    return this;
  }

  /**
   * Get summary field value by label
   */
  getSummaryFieldValue(label: string): Cypress.Chainable<string> {
    return cy
      .get(this.detailsSection.summaryTable, { timeout: this.STANDARD_TIMEOUT })
      .contains(".display-input-label", label)
      .siblings(".display-input")
      .invoke("text")
      .then((text) => text.trim());
  }

  /**
   * Verify summary field value
   */
  verifySummaryFieldValue(label: string, expectedValue: string): this {
    cy.get(this.detailsSection.summaryTable, { timeout: this.STANDARD_TIMEOUT })
      .contains(".display-input-label", label)
      .siblings(".display-input")
      .should("contain.text", expectedValue);
    return this;
  }

  // ============ Assessment Scores Methods ============

  /**
   * Enter financial analysis score
   */
  enterFinancialAnalysis(score: string): this {
    cy.get(this.assessmentSection.financialAnalysis, { timeout: this.STANDARD_TIMEOUT })
      .clear()
      .type(score);
    return this;
  }

  /**
   * Enter economic impact score
   */
  enterEconomicImpact(score: string): this {
    cy.get(this.assessmentSection.economicImpact, { timeout: this.STANDARD_TIMEOUT })
      .clear()
      .type(score);
    return this;
  }

  /**
   * Enter inclusive growth score
   */
  enterInclusiveGrowth(score: string): this {
    cy.get(this.assessmentSection.inclusiveGrowth, { timeout: this.STANDARD_TIMEOUT })
      .clear()
      .type(score);
    return this;
  }

  /**
   * Enter clean growth score
   */
  enterCleanGrowth(score: string): this {
    cy.get(this.assessmentSection.cleanGrowth, { timeout: this.STANDARD_TIMEOUT })
      .clear()
      .type(score);
    return this;
  }

  /**
   * Click save assessment scores button
   */
  clickSaveAssessmentScores(): this {
    cy.get(this.assessmentSection.saveAssessmentScoresBtn, { timeout: this.STANDARD_TIMEOUT })
      .should("not.be.disabled")
      .click({ force: true });
    return this;
  }

  /**
   * Select recommendation
   */
  selectRecommendation(recommendation: "true" | "false"): this {
    cy.get(this.assessmentSection.recommendationSelect, { timeout: this.STANDARD_TIMEOUT })
      .select(recommendation);
    return this;
  }

  /**
   * Click reset recommendation button
   */
  clickResetRecommendation(): this {
    cy.get(this.assessmentSection.recommendationResetBtn, { timeout: this.STANDARD_TIMEOUT })
      .click({ force: true });
    return this;
  }

  // ============ Email Methods ============

  /**
   * Click New Email button
   */
  clickNewEmail(): this {
    cy.get(this.emailSection.newEmailBtn, { timeout: this.STANDARD_TIMEOUT })
      .should("be.visible")
      .click({ force: true });
    return this;
  }

  /**
   * Select email template
   */
  selectEmailTemplate(templateName: string): this {
    cy.get(this.emailSection.templateSelect, { timeout: this.STANDARD_TIMEOUT })
      .select(templateName);
    return this;
  }

  /**
   * Enter email To address
   */
  enterEmailTo(email: string): this {
    cy.get(this.emailSection.emailTo, { timeout: this.STANDARD_TIMEOUT })
      .clear()
      .type(email);
    return this;
  }

  /**
   * Enter email CC address
   */
  enterEmailCC(email: string): this {
    cy.get(this.emailSection.emailCC, { timeout: this.STANDARD_TIMEOUT })
      .clear()
      .type(email);
    return this;
  }

  /**
   * Enter email BCC address
   */
  enterEmailBCC(email: string): this {
    cy.get(this.emailSection.emailBCC, { timeout: this.STANDARD_TIMEOUT })
      .clear()
      .type(email);
    return this;
  }

  /**
   * Enter email subject
   */
  enterEmailSubject(subject: string): this {
    cy.get(this.emailSection.emailSubject, { timeout: this.STANDARD_TIMEOUT })
      .clear()
      .type(subject);
    return this;
  }

  /**
   * Enter email body
   */
  enterEmailBody(body: string): this {
    cy.get(this.emailSection.emailBody, { timeout: this.STANDARD_TIMEOUT })
      .clear()
      .type(body);
    return this;
  }

  /**
   * Click Save email button
   */
  clickSaveEmail(): this {
    cy.get(this.emailSection.saveBtn, { timeout: this.STANDARD_TIMEOUT })
      .should("be.visible")
      .click({ force: true });
    return this;
  }

  /**
   * Click Send email button
   */
  clickSendEmail(): this {
    cy.get(this.emailSection.sendBtn, { timeout: this.STANDARD_TIMEOUT })
      .should("be.visible")
      .click({ force: true });
    return this;
  }

  /**
   * Click Confirm Send email button
   */
  clickConfirmSendEmail(): this {
    cy.get(this.emailSection.confirmSendBtn, { timeout: this.STANDARD_TIMEOUT })
      .should("be.visible")
      .click({ force: true });
    return this;
  }

  /**
   * Click Cancel email button
   */
  clickCancelEmail(): this {
    cy.get(this.emailSection.cancelBtn, { timeout: this.STANDARD_TIMEOUT })
      .should("be.visible")
      .click({ force: true });
    return this;
  }

  // ============ Comments Methods ============

  /**
   * Add a comment
   */
  addComment(comment: string): this {
    cy.get(this.commentsSection.commentTextArea, { timeout: this.STANDARD_TIMEOUT })
      .first()
      .clear()
      .type(comment);
    return this;
  }

  /**
   * Click save comment button
   */
  clickSaveComment(): this {
    cy.get(this.commentsSection.addCommentSaveBtn, { timeout: this.STANDARD_TIMEOUT })
      .first()
      .click({ force: true });
    return this;
  }

  /**
   * Click cancel comment button
   */
  clickCancelComment(): this {
    cy.get(this.commentsSection.addCommentCancelBtn, { timeout: this.STANDARD_TIMEOUT })
      .first()
      .click({ force: true });
    return this;
  }

  /**
   * Verify comment exists
   */
  verifyCommentExists(commentText: string): this {
    cy.get(this.commentsSection.commentsContainer, { timeout: this.STANDARD_TIMEOUT })
      .should("contain.text", commentText);
    return this;
  }

  // ============ Attachments Methods ============

  /**
   * Click upload attachment button
   */
  clickUploadAttachment(): this {
    cy.get(this.attachmentsSection.uploadBtn, { timeout: this.STANDARD_TIMEOUT })
      .should("be.visible")
      .click({ force: true });
    return this;
  }

  /**
   * Upload file attachment
   * Uses the Submission Attachments file input
   */
  uploadAttachment(filePath: string): this {
    // Try the submission attachments file input first
    const selectors = [
      "#addSubmissionAttachmentsFile",
      "#application_upload",
      "#attachments input[type='file']",
      "input[type='file'][id*='attachment']",
      "input[type='file'][id*='Attachment']",
      "input[type='file']",
    ];

    cy.get("body").then(($body) => {
      let fileInput = null;

      for (const selector of selectors) {
        const $el = $body.find(selector);
        if ($el.length > 0) {
          fileInput = selector;
          break;
        }
      }

      if (fileInput) {
        cy.get(fileInput).first().selectFile(filePath, { force: true });
      } else {
        // Click Add Attachments button to trigger file input
        cy.contains("Add Attachments", { timeout: this.STANDARD_TIMEOUT })
          .click({ force: true });
        cy.wait(500);
        cy.get("input[type='file']").first().selectFile(filePath, { force: true });
      }
    });

    // Wait for upload to complete
    cy.wait(3000);
    return this;
  }

  /**
   * Upload a unique attachment with generated content
   * @param fileName - The filename to use
   * @param timestamp - Timestamp for unique content
   */
  uploadUniqueAttachment(fileName: string, timestamp: number): this {
    cy.get("#attachments input[type='file']", { timeout: this.STANDARD_TIMEOUT })
      .first()
      .selectFile(
        {
          contents: Cypress.Buffer.from(`Test attachment content - ${timestamp}`),
          fileName: fileName,
          mimeType: "text/plain",
        },
        { force: true }
      );
    return this;
  }

  /**
   * Verify attachments table has rows
   */
  verifyAttachmentsTableHasRows(): this {
    cy.get(this.attachmentsSection.attachmentsTable, { timeout: this.STANDARD_TIMEOUT })
      .find("tbody tr")
      .should("have.length.at.least", 1);
    return this;
  }

  /**
   * Verify attachment exists by name
   */
  verifyAttachmentExists(fileName: string): this {
    cy.get("#attachments", { timeout: this.STANDARD_TIMEOUT })
      .should("contain.text", fileName);
    return this;
  }

  // ============ Links Methods ============

  /**
   * Click add link button
   */
  clickAddLink(): this {
    cy.get(this.linksSection.addLinkBtn, { timeout: this.STANDARD_TIMEOUT })
      .should("be.visible")
      .click({ force: true });
    return this;
  }

  /**
   * Verify links table has rows
   */
  verifyLinksTableHasRows(): this {
    cy.get(this.linksSection.linksTable, { timeout: this.STANDARD_TIMEOUT })
      .find("tbody tr")
      .should("have.length.at.least", 1);
    return this;
  }

  // ============ History Methods ============

  /**
   * Verify history table has rows
   */
  verifyHistoryTableHasRows(): this {
    cy.get(this.historySection.historyTable, { timeout: this.STANDARD_TIMEOUT })
      .find("tbody tr")
      .should("have.length.at.least", 1);
    return this;
  }

  /**
   * Verify history contains action
   */
  verifyHistoryContainsAction(action: string): this {
    cy.get(this.historySection.historyTable, { timeout: this.STANDARD_TIMEOUT })
      .should("contain.text", action);
    return this;
  }

  // ============ Verification Methods ============

  /**
   * Verify right panel is visible
   */
  verifyRightPanelVisible(): this {
    cy.get(this.container, { timeout: this.STANDARD_TIMEOUT })
      .should("be.visible");
    return this;
  }

  /**
   * Verify all tabs are visible
   */
  verifyAllTabsVisible(): this {
    cy.get(this.tabs.details, { timeout: this.STANDARD_TIMEOUT }).should("be.visible");
    cy.get(this.tabs.emails, { timeout: this.STANDARD_TIMEOUT }).should("be.visible");
    cy.get(this.tabs.comments, { timeout: this.STANDARD_TIMEOUT }).should("be.visible");
    cy.get(this.tabs.attachments, { timeout: this.STANDARD_TIMEOUT }).should("be.visible");
    cy.get(this.tabs.links, { timeout: this.STANDARD_TIMEOUT }).should("be.visible");
    cy.get(this.tabs.history, { timeout: this.STANDARD_TIMEOUT }).should("be.visible");
    return this;
  }
}
