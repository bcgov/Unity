/// <reference types="cypress" />

/**
 * Approval Flow Regression Test - Full Approval Workflow
 *
 * This test validates the complete application approval workflow including:
 * - Dynamic submission ID fetching from API
 * - Searching and opening a submission
 * - Review and assessment process
 * - Payment info configuration
 * - Adding comments and attachments
 * - Approval action (confirmed via dialog)
 *
 * The submission ID is fetched dynamically from the API after login,
 * ensuring tests always run against valid, available data.
 */

import { ApplicationsListPage } from "../pages/ApplicationsListPage";
import { ApplicationDetailsPage } from "../pages/ApplicationDetailsPage";
import { ReviewAssessmentPage } from "../pages/ReviewAssessmentPage";
import { ApplicationDetailsRightTabPage } from "../pages/ApplicationDetailsRightTabPage";
import { loginIfNeeded } from "../support/auth";

// ============ Test Configuration ============
// Set submissionId to null for dynamic fetch, or provide a value to override
const TEST_CONFIG = {
  // Dynamic submission: set to null to fetch from API, or provide ID to use static value
  submissionId: null as string | null,
  grantProgram: "Default Grants Program",
  approvedAmount: "5000",
  supplierNumber: Cypress.env("environment") === "TEST" ? "2002712" : "2009366",
  paymentGroup: "Cheque" as const,
  testComment: "Test comment from automated regression test",
  // Options for dynamic submission fetching (only used when submissionId is null)
  // Results are sorted by submissionDate descending (latest first) by default
  fetchOptions: {
    // Filter by category (required for this test)
    categoryFilter: "Data Seeder",
    // Filter by status (uncomment to enable):
    // Available: 'Submitted', 'Under Assessment', 'Approved', 'Closed', 'Deferred'
    statusFilter: ["Submitted"],
    // Limit to submissions within N days (uncomment to enable):
    maxAge: 30,
    // Which submission to use after sorting (0 = latest, 1 = second-latest, etc.)
    // Use index > 0 to avoid picking the same submission as other concurrent tests
    index: 0,
  },
};

describe("Approval Flow Regression Test", () => {
  // Page object instances (reused across all tests)
  const listPage = new ApplicationsListPage();
  const detailsPage = new ApplicationDetailsPage();
  const reviewPage = new ReviewAssessmentPage();
  const rightTabPage = new ApplicationDetailsRightTabPage();

  // Dynamic submission ID - populated after login
  let submissionId: string;

  before(() => {
    Cypress.config("includeShadowDom", true);
    loginIfNeeded();
  });

  // ============ Dynamic Submission Fetch ============

  it("Fetch submission ID from API", () => {
    // Use static ID if provided, otherwise fetch dynamically
    if (TEST_CONFIG.submissionId) {
      submissionId = TEST_CONFIG.submissionId;
      cy.log(`📌 Using static submission ID: ${submissionId}`);
      return;
    }

    // Fetch submission ID dynamically from API using session cookies
    cy.fetchDynamicSubmission(TEST_CONFIG.fetchOptions).then((id) => {
      submissionId = id;
      cy.log(`✅ Fetched dynamic submission ID: ${submissionId}`);
    });
  });

  // ============ Navigation & Search ============

  it("Switch to grant program", () => {
    listPage.switchToGrantProgram(TEST_CONFIG.grantProgram);
  });

  it("Search for submission", () => {
    // Ensure submissionId is available before searching
    expect(submissionId, "Submission ID should be set").to.exist;
    listPage
      .selectQuickDateRange("alltime")
      .waitForTableRefresh()
      .searchForSubmission(submissionId);
  });

  it("Select submission and open details", () => {
    listPage.selectRowByText(submissionId).clickOpenButton();
  });

  // ============ Review & Assessment ============

  it("Navigate to Review and Assessment tab", () => {
    detailsPage.goToReviewAssessmentTab().verifyActiveTab("reviewAssessment");
  });

  it("Enter approval details and save", () => {
    reviewPage
      .verifyFormioLoaded()
      .enterApprovedAmount(TEST_CONFIG.approvedAmount)
      .setDecisionDateToToday()
      .clickSave();
  });

  it("Create and complete assessment", () => {
    // Wait for assessment section to load
    cy.wait(2000);
    reviewPage.scrollToAssessmentList();

    // Check if Create button exists and click it
    cy.get("body").then(($body) => {
      if ($body.find("#CreateButton").length > 0) {
        cy.get("#CreateButton").click({ force: true });
        cy.wait(1000);
      } else {
        cy.log("Create Assessment button not found - may already be created");
      }
    });

    // Check if Complete button exists and click it
    cy.get("body").then(($body) => {
      if ($body.find("#CompleteButton").length > 0) {
        cy.get("#CompleteButton").click({ force: true });
        cy.wait(1000);
      } else {
        cy.log(
          "Complete Assessment button not found - may already be completed",
        );
      }
    });
  });

  // ============ Payment Info ============

  it("Configure payment info", () => {
    // Reload page to get fresh data and avoid concurrency issues
    cy.reload();
    cy.wait(2000);
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
    // Dismiss any error modals from previous steps
    detailsPage.dismissErrorModalIfPresent();
    rightTabPage
      .goToCommentsTab()
      .addComment(TEST_CONFIG.testComment)
      .clickSaveComment();
  });

  it("Add an attachment", () => {
    // Dismiss any error modals from previous steps
    detailsPage.dismissErrorModalIfPresent();
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

  it("Test approval workflow (confirm)", () => {
    // Dismiss any error modals from previous steps
    detailsPage.dismissErrorModalIfPresent();
    detailsPage.clickApprove().waitForConfirmModal().clickConfirm();
  });

  // ============ Cleanup ============

  it("Logout", () => {
    cy.logout();
  });
});
