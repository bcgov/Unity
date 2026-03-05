/// <reference types="cypress" />

/**
 * Sample Regression Test - Full Approval Workflow
 *
 * This test validates the complete application approval workflow including:
 * - Searching and opening a submission
 * - Review and assessment process
 * - Payment info configuration
 * - Adding comments and attachments
 * - Approval action (cancelled for test purposes)
 */

import { ApplicationsListPage } from "../pages/ApplicationsListPage";
import { ApplicationDetailsPage } from "../pages/ApplicationDetailsPage";
import { ReviewAssessmentPage } from "../pages/ReviewAssessmentPage";
import { ApplicationDetailsRightTabPage } from "../pages/ApplicationDetailsRightTabPage";
import { loginIfNeeded } from "../support/auth";

// ============ Test Configuration ============
// These values can be modified to test different submissions
const TEST_CONFIG = {
  submissionId: "84A888BD",
  grantProgram: "Default Grants Program",
  approvedAmount: "5000",
  supplierNumber: "2002712",
  paymentGroup: "Cheque" as const,
  testComment: "Test comment from automated regression test",
};

describe("Sample Regression Test", () => {
  // Page object instances (reused across all tests)
  const listPage = new ApplicationsListPage();
  const detailsPage = new ApplicationDetailsPage();
  const reviewPage = new ReviewAssessmentPage();
  const rightTabPage = new ApplicationDetailsRightTabPage();

  before(() => {
    Cypress.config("includeShadowDom", true);
    loginIfNeeded();
  });

  // ============ Navigation & Search ============

  it("Switch to grant program", () => {
    listPage.switchToGrantProgram(TEST_CONFIG.grantProgram);
  });

  it("Search for submission", () => {
    listPage
      .selectQuickDateRange("alltime")
      .waitForTableRefresh()
      .searchForSubmission(TEST_CONFIG.submissionId);
  });

  it("Select submission and open details", () => {
    listPage
      .selectRowByText(TEST_CONFIG.submissionId)
      .clickOpenButton();
  });

  // ============ Review & Assessment ============

  it("Navigate to Review and Assessment tab", () => {
    detailsPage
      .goToReviewAssessmentTab()
      .verifyActiveTab("reviewAssessment");
  });

  it("Enter approval details and save", () => {
    reviewPage
      .verifyFormioLoaded()
      .enterApprovedAmount(TEST_CONFIG.approvedAmount)
      .setDecisionDateToToday()
      .clickSave();
  });

  it("Create and complete assessment", () => {
    reviewPage
      .scrollToAssessmentList()
      .clickCreateAssessment()
      .clickCompleteAssessment();
  });

  // ============ Payment Info ============

  it("Configure payment info", () => {
    detailsPage
      .goToPaymentInfoTab()
      .enterSupplierNumber(TEST_CONFIG.supplierNumber)
      .clickElsewhere()
      .clickPaymentInfoSave();
  });

  it("Validate and edit site info", () => {
    detailsPage
      .verifySiteInfoTablePopulated()
      .verifySiteInfoTableHasData()
      .clickSiteInfoEdit()
      .waitForEditSiteModal()
      .selectPaymentGroup(TEST_CONFIG.paymentGroup)
      .clickSaveChanges();
  });

  // ============ Comments & Attachments ============

  it("Add a comment", () => {
    rightTabPage
      .goToCommentsTab()
      .addComment(TEST_CONFIG.testComment)
      .clickSaveComment();
  });

  it("Add an attachment", () => {
    rightTabPage.goToAttachmentsTab();
    cy.wait(1000); // Allow tab content to load

    // Store initial count to verify upload
    rightTabPage.getAttachmentsCount().then((initialCount) => {
      cy.log(`Initial attachment count: ${initialCount}`);

      // Generate unique filename to ensure new file is added
      const timestamp = Date.now();
      const uniqueFileName = `test-attachment-${timestamp}.txt`;

      // Upload file with unique content
      rightTabPage.uploadUniqueAttachment(uniqueFileName, timestamp);

      // Verify upload success
      cy.contains("Successful").should("be.visible");
      cy.wait(2000); // Allow UI to update

      // Verify count increased
      rightTabPage.getAttachmentsCount().then((newCount) => {
        cy.log(`New attachment count: ${newCount}`);
        expect(newCount).to.be.greaterThan(initialCount);
      });

      // Verify file appears in list
      rightTabPage.verifyAttachmentExists(uniqueFileName);
      cy.screenshot("attachment-upload-complete");
    });
  });

  // ============ Approval Action ============

  it("Test approval workflow (cancel)", () => {
    detailsPage
      .clickApprove()
      .waitForConfirmModal()
      .clickCancel();
  });

  // ============ Cleanup ============

  it("Logout", () => {
    cy.logout();
  });
});
