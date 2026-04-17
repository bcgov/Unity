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
  assignOwner: "Unity User1",
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

const STATUS_ACTIONS = {
  menuButton: "#ApplicationActionDropdown .dropdown-toggle",
  menu: "#ApplicationActionDropdown .dropdown-menu",
  startReview: "#Application_StartReviewButton",
  completeReview: "#Application_CompleteReviewButton",
  startAssessment: "#Application_StartAssessmentButton",
  completeAssessment: "#Application_CompleteAssessmentButton",
  approve: "#Application_ApproveButton",
};

const ADJUDICATION_ACTIONS = {
  completeAssessment: "#AdjudicationTeamLeadActionBar #CompleteButton",
};

const BREADCRUMB_STATUS_SELECTOR =
  ".application-details-breadcrumb .application-status";
const APPLICATIONS_PATH = "GrantApplications";

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
    listPage.waitForNoBlockingOverlay();
    cy.reload();
    navPage.goToPayments();
    cy.location("pathname", { timeout: 20000 }).should("include", "Payment");
    cy.get("#search", { timeout: 20000 })
      .should("be.visible")
      .clear()
      .type(submissionId);
    cy.contains("tr", submissionId, { timeout: 20000 }).should("exist");
  }

  function navigateToApplicationsList(): void {
    cy.location("pathname", { timeout: 20000 }).then((pathname) => {
      if (!pathname.includes(`/${APPLICATIONS_PATH}`)) {
        cy.visit(`${Cypress.env("webapp.url")}${APPLICATIONS_PATH}`);
      }
    });
    listPage.waitForNoBlockingOverlay();
  }

  /**
   * Seeded submissions can take time to appear in the Grant Applications list.
   * Poll the list with a refresh until the row becomes searchable.
   */
  function waitForSubmissionToAppearInList(
    attempt = 1,
    maxAttempts = 8,
  ): Cypress.Chainable<void> {
    navigateToApplicationsList();
    dismissBlockingModalIfPresent();

    listPage
      .selectQuickDateRange("alltime")
      .waitForTableRefresh()
      .searchForSubmission(submissionId);

    return cy.get("body").then(($body) => {
      const hasRow = $body.find(`tr:contains("${submissionId}")`).length > 0;

      if (hasRow) {
        cy.log(
          `Submission ${submissionId} found in applications list on attempt ${attempt}`,
        );
        return;
      }

      if (attempt >= maxAttempts) {
        throw new Error(
          `Submission ${submissionId} was not visible in the applications list after ${maxAttempts} attempts`,
        );
      }

      cy.log(
        `Submission ${submissionId} not visible yet. Refreshing applications list (attempt ${attempt} of ${maxAttempts})`,
      );
      cy.wait(5000);
      cy.reload();
      return waitForSubmissionToAppearInList(attempt + 1, maxAttempts);
    });
  }

  function ensureSiteInfoReady(
    attempt = 1,
    maxAttempts = 4,
  ): Cypress.Chainable<void> {
    detailsPage.dismissErrorModalIfPresent();
    detailsPage.goToPaymentInfoTab();
    cy.wait(1000);

    return cy.get("body").then(($body) => {
      const hasTokenError =
        $body.text().includes("GetAuthTokenAsync") ||
        $body.text().includes("Error retrieving Token");
      const rows = $body.find("#SiteInfoTable tbody tr");
      const firstRowText = rows.first().text().replace(/\s+/g, " ").trim();
      const hasData =
        rows.length > 0 && !/no data available/i.test(firstRowText);

      if (!hasTokenError && hasData) {
        cy.log(`Site info ready on attempt ${attempt}`);
        return;
      }

      if (attempt >= maxAttempts) {
        throw new Error(
          `Site info was not ready after ${maxAttempts} attempts`,
        );
      }

      cy.log(
        `Site info not ready yet. Re-activating payment info content (attempt ${attempt} of ${maxAttempts})`,
      );
      detailsPage.dismissErrorModalIfPresent();
      detailsPage.goToFundingAgreementTab();
      cy.wait(1000);
      detailsPage.goToPaymentInfoTab();
      cy.wait(3000);
      return ensureSiteInfoReady(attempt + 1, maxAttempts);
    });
  }

  function waitForBlockingUiToClear(): void {
    cy.get(".swal2-container", { timeout: 20000 }).should("not.exist");
    cy.get(".modal.show", { timeout: 20000 }).should("not.exist");
    cy.get(".modal-backdrop", { timeout: 20000 }).should("not.exist");
  }

  function openStatusActionsMenu(): void {
    waitForBlockingUiToClear();
    detailsPage.dismissErrorModalIfPresent();
    cy.get(STATUS_ACTIONS.menuButton, { timeout: 20000 })
      .filter(":visible")
      .first()
      .scrollIntoView()
      .should("be.visible")
      .and("not.contain.text", "Processing...")
      .click({ force: true });
    cy.get(STATUS_ACTIONS.menu, { timeout: 20000 }).should("be.visible");
  }

  function clickStatusAction(actionSelector: string): void {
    openStatusActionsMenu();
    cy.get(actionSelector, { timeout: 20000 })
      .filter(":visible")
      .first()
      .should("be.visible")
      .and("not.be.disabled")
      .click({ force: true });
  }

  function clickStatusActionIfEnabled(
    actionSelector: string,
    actionName: string,
  ): void {
    openStatusActionsMenu();
    cy.get("body").then(($body) => {
      const $action = $body.find(actionSelector).filter(":visible");
      if ($action.length === 0) {
        cy.log(`${actionName} not present in dropdown; likely already progressed`);
        cy.get("body").click(0, 0); // close menu
        return;
      }
      if ($action.is(":disabled")) {
        cy.log(`${actionName} is disabled; likely already progressed`);
        cy.get("body").click(0, 0); // close menu
        return;
      }
      cy.wrap($action.first()).click({ force: true });
      confirmStatusActionIfNeeded();
    });
  }

  function confirmStatusActionIfNeeded(): void {
    cy.wait(500);
    cy.get("body").then(($body) => {
      if ($body.find(".swal2-popup .swal2-confirm").length > 0) {
        cy.get(".swal2-popup .swal2-confirm", { timeout: 20000 })
          .should("be.visible")
          .click({ force: true });
        cy.get(".swal2-container", { timeout: 20000 }).should("not.exist");
        return;
      }

      if (
        $body.find(".modal.show .modal-content:contains('Confirm Action')")
          .length > 0
      ) {
        cy.get(".modal.show", { timeout: 20000 })
          .should("be.visible")
          .within(() => {
            cy.contains("button", /^Confirm$/i, { timeout: 20000 })
              .should("be.visible")
              .click({ force: true });
          });
        cy.contains(".modal.show .modal-content", "Confirm Action", {
          timeout: 20000,
        }).should("not.exist");
        cy.get(".modal-backdrop", { timeout: 20000 }).should("not.exist");
      }
    });
  }

  function dismissBlockingModalIfPresent(): void {
    cy.get("body").then(($body) => {
      if ($body.find(".swal2-container .swal2-confirm").length > 0) {
        cy.get(".swal2-container .swal2-confirm", { timeout: 20000 })
          .should("be.visible")
          .click({ force: true });
        cy.get(".swal2-container", { timeout: 20000 }).should("not.exist");
      }

      if ($body.find(".modal.show").length > 0) {
        cy.get(".modal.show", { timeout: 20000 }).should("not.exist");
        cy.get(".modal-backdrop", { timeout: 20000 }).should("not.exist");
      }
    });
  }

  function selectFirstAvailableOptionByLabel(labelText: string): void {
    cy.get("body").then(($body) => {
      if ($body.find(`label:contains('${labelText}'):visible`).length === 0) {
        cy.log(`Label not found (skip): ${labelText}`);
        return;
      }

      cy.contains("label", labelText, { timeout: 10000 })
        .first()
        .then(($label) => {
          const fieldId = ($label.attr("for") || "").trim();
          if (!fieldId) {
            cy.log(`No 'for' attribute on label (skip): ${labelText}`);
            return;
          }

          const selector = `#${fieldId}`;
          if ($body.find(selector).length === 0) {
            cy.log(`Field not found by id (skip): ${selector}`);
            return;
          }

          cy.get(selector, { timeout: 10000 }).then(($select) => {
            const options = $select.find("option").toArray();
            const candidate = options.find((opt) => {
              const value = (opt.getAttribute("value") || "").trim();
              const text = (opt.textContent || "").trim().toLowerCase();
              return value !== "" && text !== "please choose...";
            });

            const value = candidate?.getAttribute("value");
            if (value) {
              cy.wrap($select).select(value, { force: true });
            }
          });
        });
    });
  }

  function populateReviewFieldsRequiredForCompleteReview(): void {
    detailsPage.goToReviewAssessmentTab();

    reviewPage.verifyFormioLoaded();
    cy.get("#ApprovalView_ApprovedAmount", { timeout: 30000 })
      .should("be.visible")
      .and("not.be.disabled");

    cy.get("body").then(($body) => {
      if ($body.find("#ApprovalView_ApprovedAmount").length > 0) {
        reviewPage.enterApprovedAmount(TEST_CONFIG.approvedAmount);
      } else {
        cy.log("Approved amount field not present yet; skipping amount entry");
      }

      if ($body.find("#ApprovalView_FinalDecisionDate").length > 0) {
        reviewPage.setDecisionDateToToday();
      } else {
        cy.log("Decision date field not present yet; skipping date entry");
      }
    });

    selectFirstAvailableOptionByLabel("Likelihood of Funding");
    selectFirstAvailableOptionByLabel("Due Diligence Status");
    selectFirstAvailableOptionByLabel("Assessment Result");
    reviewPage.clickSave();
  }

  function assignSubmissionFromList(ownerName: string): void {
    dismissBlockingModalIfPresent();
    waitForSubmissionToAppearInList();

    listPage
      .waitForNoBlockingOverlay()
      .selectQuickDateRange("alltime")
      .waitForTableRefresh()
      .searchForSubmission(submissionId)
      .selectRowByText(submissionId);

    cy.get("#assignApplication", { timeout: 20000 })
      .should("exist")
      .and("not.have.class", "action-bar-btn-unavailable")
      .and("be.visible")
      .click({ force: true });

    cy.contains(".modal-title", "Assessment Users", { timeout: 20000 }).should(
      "be.visible",
    );

    cy.get("#AssigneeId", { timeout: 20000 })
      .should("be.visible")
      .select(ownerName);

    cy.get("#user-tags-input", { timeout: 20000 })
      .should("be.visible")
      .clear()
      .type(ownerName, { delay: 0 });

    cy.get("body").then(($body) => {
      if (
        $body.find(".tags-suggestion-container .tags-suggestion-element")
          .length > 0
      ) {
        cy.get(".tags-suggestion-container .tags-suggestion-element", {
          timeout: 10000,
        })
          .contains(ownerName)
          .click({ force: true });
      } else {
        cy.get("#user-tags-input").type("{enter}");
      }
    });

    cy.contains(".modal-footer button", "Save", { timeout: 20000 })
      .should("be.visible")
      .and("not.be.disabled")
      .click({ force: true });

    cy.contains(".modal-title", "Assessment Users", { timeout: 20000 }).should(
      "not.exist",
    );
    listPage.waitForNoBlockingOverlay();
  }

  function clickAdjudicationActionIfEnabled(actionSelector: string): void {
    cy.get("body").then(($body) => {
      if ($body.find(actionSelector).length > 0) {
        cy.get(actionSelector, { timeout: 20000 }).then(($button) => {
          if (!$button.is(":disabled")) {
            cy.wrap($button).click({ force: true });
            confirmStatusActionIfNeeded();
          } else {
            cy.log(`Action is disabled: ${actionSelector}`);
          }
        });
      } else {
        cy.log(`Action not present: ${actionSelector}`);
      }
    });
  }

  /** Select the submissionId row and open the Approve Payments modal. */
  function selectRowAndOpenApproveModal(): void {
    waitForBlockingUiToClear();
    cy.contains("tr", submissionId, { timeout: 20000 })
      .scrollIntoView()
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

  it("Assign submission on application list", () => {
    assignSubmissionFromList(TEST_CONFIG.assignOwner);
  });

  it("Select submission and open details", () => {
    dismissBlockingModalIfPresent();
    waitForSubmissionToAppearInList();
    listPage.waitForNoBlockingOverlay();

    cy.location("pathname", { timeout: 20000 }).then((pathname) => {
      if (pathname.includes("/GrantApplications/Details")) {
        cy.log("Already on details page after assignment");
      } else {
        listPage
          .selectQuickDateRange("alltime")
          .waitForTableRefresh()
          .searchForSubmission(submissionId)
          .selectRowByText(submissionId)
          .clickOpenButton();
      }
    });

    cy.get(BREADCRUMB_STATUS_SELECTOR, { timeout: 20000 })
      .should("be.visible")
      .invoke("text")
      .then((statusText) => {
        const normalized = statusText.trim().toLowerCase();
        expect(
          ["submitted", "assigned"],
          "Expected initial status to be Submitted or Assigned",
        ).to.include(normalized);
      });
  });

  // ============ Review & Assessment ============

  it("Start review from Status Actions", () => {
    clickStatusActionIfEnabled(STATUS_ACTIONS.startReview, "Start Review");
  });

  it("Complete review from Status Actions", () => {
    populateReviewFieldsRequiredForCompleteReview();
    clickStatusActionIfEnabled(
      STATUS_ACTIONS.completeReview,
      "Complete Review",
    );
  });

  it("Start assessment from Status Actions", () => {
    clickStatusActionIfEnabled(
      STATUS_ACTIONS.startAssessment,
      "Start Assessment",
    );
  });

  it("Navigate to Review and Assessment tab", () => {
    detailsPage.goToReviewAssessmentTab().verifyActiveTab("reviewAssessment");
  });

  it("Create assessment", () => {
    cy.wait(2000); // Allow assessment section to fully load
    reviewPage.scrollToAssessmentList();

    cy.get("body").then(($body) => {
      if ($body.find("#CreateButton").length > 0) {
        cy.get("#CreateButton").click({ force: true });
        // Give the new assessment row time to render before subsequent actions.
        cy.wait(1000); // Needed because row creation animation can delay DOM readiness.
      } else {
        cy.log("Create Assessment button not found - may already be created");
      }
    });
  });

  it("Complete assessment from adjudication action bar", () => {
    clickAdjudicationActionIfEnabled(ADJUDICATION_ACTIONS.completeAssessment);
  });

  // ============ Payment Info ============

  it("Configure payment info", () => {
    cy.reload(); // Reload to get fresh data and avoid concurrency issues
    // Wait briefly for async payment tab dependencies to stabilize after reload.
    cy.wait(2000); // Prevents save attempts before payment controls are initialized.
    detailsPage
      .goToPaymentInfoTab()
      .enterSupplierNumber(TEST_CONFIG.supplierNumber)
      .clickElsewhere()
      .clickPaymentInfoSave();
  });

  it("Validate and edit site info", () => {
    ensureSiteInfoReady();
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

  it("Enter approval details and save", () => {
    detailsPage.goToReviewAssessmentTab().verifyActiveTab("reviewAssessment");
    reviewPage
      .verifyFormioLoaded()
      .enterApprovedAmount(TEST_CONFIG.approvedAmount)
      .setDecisionDateToToday()
      .clickSave();
  });

  // ============ Application Approval ============

  it("Test approval workflow (confirm)", () => {
    cy.reload(); // Refresh to ensure all changes are reflected before approval
    detailsPage.dismissErrorModalIfPresent();
    clickStatusAction(STATUS_ACTIONS.completeAssessment);
    confirmStatusActionIfNeeded();

    cy.get(STATUS_ACTIONS.menuButton, { timeout: 20000 })
      .filter(":visible")
      .first()
      .should("be.visible")
      .and("not.contain.text", "Processing...");

    clickStatusAction(STATUS_ACTIONS.approve);
    detailsPage.waitForConfirmModal().clickConfirm();
  });

  // ============ Post-Approval Verification ============

  it("Navigate back to applications list", () => {
    cy.visit(`${Cypress.env("webapp.url")}${APPLICATIONS_PATH}`);
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
    listPage
      .waitForNoBlockingOverlay()
      .selectRowByText(submissionId)
      .clickPaymentButtonWithWait();
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

    listPage.verifyPaymentModalClosed();
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
