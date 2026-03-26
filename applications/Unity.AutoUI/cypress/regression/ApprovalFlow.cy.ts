/// <reference types="cypress" />

/**
 * Approval Flow Regression Test - Full Approval Workflow
 *
 * This test validates the complete application approval workflow including:
 * - Submission ID resolution (seeded file → static override → dynamic API fetch)
 * - Searching and opening a submission
 * - Review and assessment process
 * - Payment info configuration
 * - Adding comments and attachments
 * - Application approval (confirmed via dialog)
 * - Payment request submission
 * - L1 and L2 payment approvals (two separate users)
 * - Post-approval status and date validation on the Payments table
 */

import { ApplicationsListPage } from "../pages/ApplicationsListPage";
import { ApplicationDetailsPage } from "../pages/ApplicationDetailsPage";
import { ReviewAssessmentPage } from "../pages/ReviewAssessmentPage";
import { ApplicationDetailsRightTabPage } from "../pages/ApplicationDetailsRightTabPage";
import { NavigationPage } from "../pages/NavigationPage";
import { loginIfNeeded } from "../support/auth";

const isProd =
  (
    Cypress.env("CHEFS_ENV") ||
    Cypress.env("environment") ||
    ""
  ).toLowerCase() === "prod";

// ============ Test Configuration ============
const TEST_CONFIG = {
  // Set to null to resolve ID automatically, or provide a value to force a specific submission
  submissionId: null as string | null,
  grantProgram: "Default Grants Program",
  approvedAmount: "5000",
  supplierNumber: Cypress.env("environment") === "TEST" ? "2002712" : "2009366",
  paymentGroup: "Cheque" as const,
  testComment: "Test comment from automated regression test",
  // Options used when fetching dynamically (ignored when submissionId is set or seeded file exists)
  fetchOptions: {
    categoryFilter: "Data Seeder",
    // Available statuses: 'Submitted', 'Under Assessment', 'Approved', 'Closed', 'Deferred'
    statusFilter: ["Submitted"],
    maxAge: 30, // Only consider submissions created within the last N days
    index: 0, // 0 = latest; increment to avoid collision with concurrent tests
  },
};

(isProd ? describe.skip : describe)("Approval Flow Regression Test", () => {
  // Page object instances reused across all tests
  const listPage = new ApplicationsListPage();
  const detailsPage = new ApplicationDetailsPage();
  const reviewPage = new ReviewAssessmentPage();
  const rightTabPage = new ApplicationDetailsRightTabPage();
  const navPage = new NavigationPage();

  // Resolved after "Fetch submission ID from API" — shared across all subsequent tests
  let submissionId: string;

  // ============ Shared Helpers ============

  /** Navigate to the Payments tab and filter the table by submissionId. */
  function navigateToPaymentsAndSearch(): void {
    cy.reload;
    navPage.goToPayments();
    cy.location("pathname", { timeout: 20000 }).should("include", "Payment");
    cy.get("#search", { timeout: 20000 })
      .should("be.visible")
      .clear()
      .type(submissionId);
    cy.contains("tr", submissionId, { timeout: 20000 }).should("exist");
  }

  /** Select the submissionId row and open the Approve Payments modal. */
  function selectRowAndOpenApproveModal(): void {
    cy.contains("tr", submissionId, { timeout: 20000 })
      .find(".checkbox-select")
      .click({ force: true });
    cy.contains("button", "Approve", { timeout: 20000 })
      .should("be.visible")
      .click();
    cy.contains(".modal-title", "Approve Payments", { timeout: 20000 }).should(
      "be.visible",
    );
  }

  /**
   * Validate the Approve Payments modal fields, enter an auto-generated note,
   * submit the approval and assert the modal closes.
   * @param notePrefix - Short prefix to distinguish L1 vs L2 notes (max length enforced at 50 chars total)
   */
  function validateAndSubmitApproveModal(notePrefix: string): void {
    cy.get("#ApplicationCount").should("have.value", "1");
    cy.get("#UpdateTotalAmount").should("not.have.value", "0");
    cy.get("#Note").should("be.visible");

    const approvalNote = `${notePrefix}-${submissionId}-${Date.now()}`.slice(
      0,
      50,
    );
    cy.get("#Note").clear().type(approvalNote);

    cy.get("#btnSubmitPayment")
      .should("be.visible")
      .and("not.be.disabled")
      .click();

    cy.contains(".modal-title", "Approve Payments", { timeout: 20000 }).should(
      "not.exist",
    );
    cy.log(`✅ Payment approved with note: ${approvalNote}`);
  }

  // ============ Setup ============

  before(() => {
    Cypress.config("includeShadowDom", true);
    loginIfNeeded();
  });

  // ============ Dynamic Submission Fetch ============

  it("Fetch submission ID from API", () => {
    // Priority 1: ID written by the seed script when running test:approval-flow
    cy.task("readJsonIfExists", "cypress/scripts/last-submission-id.json").then(
      (result) => {
        const seeded = result as {
          submissionId?: string;
          createdAt?: string;
        } | null;
        if (seeded?.submissionId) {
          submissionId = seeded.submissionId;
          cy.log(`📌 Using seeded submission ID: ${submissionId}`);
          // Clear file to prevent reuse on the next standalone run
          cy.writeFile("cypress/scripts/last-submission-id.json", {});
          return;
        }

        // Priority 2: static override in TEST_CONFIG
        if (TEST_CONFIG.submissionId) {
          submissionId = TEST_CONFIG.submissionId;
          cy.log(`📌 Using static submission ID: ${submissionId}`);
          return;
        }

        // Priority 3: fetch the latest matching submission from the Unity API
        cy.fetchDynamicSubmission(TEST_CONFIG.fetchOptions).then((id) => {
          submissionId = id;
          cy.log(`✅ Fetched dynamic submission ID: ${submissionId}`);
        });
      },
    );
  });

  // ============ Navigation & Search ============

  it("Switch to grant program", () => {
    listPage.switchToGrantProgram(TEST_CONFIG.grantProgram);
  });

  it("Search for submission", () => {
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
    cy.wait(2000); // Allow assessment section to fully load
    reviewPage.scrollToAssessmentList();

    cy.get("body").then(($body) => {
      if ($body.find("#CreateButton").length > 0) {
        cy.get("#CreateButton").click({ force: true });
        cy.wait(1000);
      } else {
        cy.log("Create Assessment button not found - may already be created");
      }
    });

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
    cy.reload(); // Reload to get fresh data and avoid concurrency issues
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
    detailsPage.dismissErrorModalIfPresent();
    rightTabPage
      .goToCommentsTab()
      .addComment(TEST_CONFIG.testComment)
      .clickSaveComment();
  });

  it("Add an attachment", () => {
    detailsPage.dismissErrorModalIfPresent();
    rightTabPage.goToAttachmentsTab();
    cy.wait(1000); // Allow tab content to load

    rightTabPage.getAttachmentsCount().then((initialCount) => {
      cy.log(`Initial attachment count: ${initialCount}`);

      const timestamp = Date.now();
      const uniqueFileName = `test-attachment-${timestamp}.txt`;

      rightTabPage.uploadUniqueAttachment(uniqueFileName, timestamp);

      cy.contains("Successful").should("be.visible");
      cy.wait(2000); // Allow UI to update after upload

      rightTabPage.getAttachmentsCount().then((newCount) => {
        cy.log(`New attachment count: ${newCount}`);
        expect(newCount).to.be.greaterThan(initialCount);
      });

      rightTabPage.verifyAttachmentExists(uniqueFileName);
      cy.screenshot("attachment-upload-complete");
    });
  });

  // ============ Application Approval ============

  it("Test approval workflow (confirm)", () => {
    cy.reload(); // Refresh to ensure all changes are reflected before approval
    detailsPage.dismissErrorModalIfPresent();
    detailsPage.clickApprove().waitForConfirmModal().clickConfirm();
  });

  // ============ Post-Approval Verification ============

  it("Navigate back to applications list", () => {
    cy.visit(`${Cypress.env("webapp.url")}GrantApplications`);
    listPage.switchToGrantProgram(TEST_CONFIG.grantProgram);
  });

  it("Verify application status is Approved", () => {
    expect(submissionId, "Submission ID should be set").to.exist;
    listPage
      .selectQuickDateRange("alltime")
      .waitForTableRefresh()
      .searchForSubmission(submissionId);

    cy.contains("tr", submissionId, { timeout: 20000 }).should(
      "contain.text",
      "Approved",
    );
  });

  // ============ Payment Request ============

  it("Select approved application and submit payment request", () => {
    listPage.selectRowByText(submissionId).clickPaymentButtonWithWait();
    listPage.waitForPaymentModalVisible();

    // Description field has a max length of 40 chars
    const paymentDescription = `AutoTest-${submissionId}`.slice(0, 40);

    // Modal uses divs (not a table) — target description input by id suffix
    cy.get("#payment-modal input[id$='__Description']")
      .should("be.visible")
      .clear()
      .type(paymentDescription);

    cy.contains("button", "Submit Payment Requests")
      .should("be.visible")
      .and("not.be.disabled")
      .click();

    cy.get("#payment-modal", { timeout: 20000 }).should("not.be.visible");
    cy.log(`✅ Payment request submitted: ${paymentDescription}`);
  });

  // ============ L1 Payment Approval (User 1) ============

  it("Navigate to Payments tab and search for submission", () => {
    navigateToPaymentsAndSearch();
  });

  it("Select payment row and open Approve Payments modal", () => {
    selectRowAndOpenApproveModal();
  });

  it("L1 Approval - Validate modal details, enter note and approve", () => {
    validateAndSubmitApproveModal("AutoApproval");
  });

  // ============ L2 Payment Approval (User 2) ============

  it("Logout user1 and login as user2 for L2 approval", () => {
    cy.logout();
    cy.clearCookies();
    cy.clearLocalStorage();
    loginIfNeeded({
      username: Cypress.env("test2username") as string,
      password: Cypress.env("test2password") as string,
    });
  });

  it("Navigate to Payments tab and search for submission (L2)", () => {
    listPage.switchToGrantProgram(TEST_CONFIG.grantProgram);
    navigateToPaymentsAndSearch();
  });

  it("Select payment row and open Approve Payments modal (L2)", () => {
    selectRowAndOpenApproveModal();
  });

  it("L2 Approval - Validate modal details, enter note and approve", () => {
    validateAndSubmitApproveModal("L2Approval");
  });

  // ============ Post-L2 Validation ============

  it("Validate payment status and approval dates on Payments table", () => {
    const today = listPage.getTodayIsoLocal();

    // Re-search to ensure the table reflects the latest state after L2 approval
    cy.get("#search", { timeout: 20000 })
      .should("be.visible")
      .clear()
      .type(submissionId);

    cy.contains("tr", submissionId, { timeout: 20000 }).should(($row) => {
      const text = $row.text();
      expect(
        text.includes("Sent to Accounts Payable") ||
          text.includes("Submitted to CAS"),
        'Expected row to contain "Sent to Accounts Payable" or "Submitted to CAS"',
      ).to.be.true;
    });

    // Validate date columns by resolving each column's index from its header title
    ["Updated On", "L1 Approval Date", "L2 Approval Date"].forEach((header) => {
      cy.get(".dt-scroll-head span.dt-column-title")
        .contains(header)
        .closest("th")
        .invoke("index")
        .then((colIndex) => {
          cy.contains("tr", submissionId)
            .find("td")
            .eq(colIndex)
            .should("contain.text", today);
        });
    });
  });

  // ============ Cleanup ============

  it("Logout", () => {
    cy.logout();
  });
});
