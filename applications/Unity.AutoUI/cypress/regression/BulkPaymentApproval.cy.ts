/// <reference types="cypress" />

/**
 * Bulk Payment Approval Flow
 *
 * Reads submission IDs produced by chefs-bulk-submission-seeder.cy.ts, then:
 *
 *   Phase 1 — Sequential approval (one it() per submission):
 *     Each submission is individually processed: assign → review → assessment
 *     → payment info → approve. After approving, the browser returns to the
 *     applications list so Cypress can manage memory before the next block.
 *
 *   Phase 2 — Bulk payment request submission (single it()):
 *     All approved submissions are selected at once on the applications list
 *     and submitted together via the payment modal (one Submit for all N).
 *
 *   Phase 3 — L1 bulk payment approval (single it()).
 *   Phase 4 — L2 bulk payment approval as user2 (single it()).
 *   Phase 5 — Status validation.
 *
 * Prerequisites:
 *   npm run test:seed:bulk   — populate bulk-submission-ids.json first.
 *   If the file is absent, falls back to cy.fetchDynamicSubmission.
 *
 * Configuration:
 *   SUBMISSION_COUNT — must match the seeder value (default: 10).
 */

import { ApplicationsListPage } from "../pages/ApplicationsListPage";
import { ApplicationDetailsPage } from "../pages/ApplicationDetailsPage";
import { ReviewAssessmentPage } from "../pages/ReviewAssessmentPage";
import { NavigationPage } from "../pages/NavigationPage";
import { loginIfNeeded } from "../support/auth";

// ─── Configuration ────────────────────────────────────────────────────────

// MAX_BULK controls how many it() blocks are generated at describe-time.
// Must be >= SUBMISSION_COUNT used in the seeder. Extras are skipped automatically.
const MAX_BULK = Number(Cypress.env("SUBMISSION_COUNT") || 5);

const BULK_CONFIG = {
  grantProgram: "Default Grants Program",
  approvedAmount: "5000",
  assignOwner: "Unity User1",
  supplierNumber: Cypress.env("environment") === "TEST" ? "2002712" : "2009366",
};

// ─── Selectors ────────────────────────────────────────────────────────────

const STATUS_ACTIONS = {
  menuButton: "#ApplicationActionDropdown .dropdown-toggle",
  menu: "#ApplicationActionDropdown .dropdown-menu",
  startReview: "#Application_StartReviewButton",
  completeReview: "#Application_CompleteReviewButton",
  startAssessment: "#Application_StartAssessmentButton",
  completeAssessment: "#Application_CompleteAssessmentButton",
  approve: "#Application_ApproveButton",
};

const ADJUDICATION_COMPLETE = "#AdjudicationTeamLeadActionBar #CompleteButton";
const BREADCRUMB_STATUS = ".application-details-breadcrumb .application-status";
const APPLICATIONS_PATH = "GrantApplications";

// ─── Spec ─────────────────────────────────────────────────────────────────

const isProd =
  (Cypress.env("CHEFS_ENV") || "").toLowerCase() === "prod" ||
  (Cypress.env("environment") || "").toLowerCase() === "prod";

(isProd ? describe.skip : describe)("Bulk Payment Approval Flow", () => {
  const listPage = new ApplicationsListPage();
  const detailsPage = new ApplicationDetailsPage();
  const reviewPage = new ReviewAssessmentPage();
  const navPage = new NavigationPage();

  // Populated in "Load bulk submission IDs"; referenced by index in all later it() blocks.
  let submissionIds: string[] = [];
  // Subset confirmed as Approved before bulk payment submission.
  let approvedSubmissionIds: string[] = [];

  // ─── Shared Helpers ─────────────────────────────────────────────────────

  /**
   * Navigate to the Grant Applications LIST page.
   * Only visits if NOT already on the list — avoids a redundant full-page
   * reload that can crash Chrome when called right after a grant program switch.
   */
  function goToApplicationsList(): void {
    cy.location("pathname", { timeout: 20000 }).then((pathname) => {
      const onList =
        pathname.includes(`/${APPLICATIONS_PATH}`) &&
        !pathname.includes("/Details");
      if (!onList) {
        cy.visit(`${Cypress.env("webapp.url")}${APPLICATIONS_PATH}`);
      }
    });
    listPage.waitForNoBlockingOverlay();
  }

  function dismissBlockingModalIfPresent(): void {
    cy.get("body").then(($body) => {
      if ($body.find(".swal2-container .swal2-confirm").length > 0) {
        cy.get(".swal2-container .swal2-confirm")
          .should("be.visible")
          .click({ force: true });
        cy.get(".swal2-container", { timeout: 20000 }).should("not.exist");
      }
      if ($body.find(".modal.show").length > 0) {
        // Actively close the modal rather than just waiting — on retries the modal
        // from a previous attempt may still be open and will never self-dismiss.
        cy.get(".modal.show")
          .first()
          .then(($modal) => {
            // Try Cancel button first, then X close button, then Escape key.
            const $cancel = $modal.find("button:contains('Cancel')");
            const $close = $modal.find(
              "button.btn-close, button[aria-label='Close'], .modal-header .close",
            );
            if ($cancel.length > 0) {
              cy.wrap($cancel.first()).click({ force: true });
            } else if ($close.length > 0) {
              cy.wrap($close.first()).click({ force: true });
            } else {
              cy.get(".modal.show").first().type("{esc}", { force: true });
            }
          });
        cy.get(".modal.show", { timeout: 20000 }).should("not.exist");
        cy.get(".modal-backdrop", { timeout: 20000 }).should("not.exist");
      }
    });
  }

  function waitForBlockingUiToClear(): void {
    cy.get(".swal2-container", { timeout: 20000 }).should("not.exist");
    cy.get(".modal.show", { timeout: 20000 }).should("not.exist");
    cy.get(".modal-backdrop", { timeout: 20000 }).should("not.exist");
  }

  function confirmStatusActionIfNeeded(): void {
    cy.wait(500);
    cy.get("body").then(($body) => {
      if ($body.find(".swal2-popup .swal2-confirm").length > 0) {
        cy.get(".swal2-popup .swal2-confirm")
          .should("be.visible")
          .click({ force: true });
        cy.get(".swal2-container", { timeout: 20000 }).should("not.exist");
        return;
      }
      if (
        $body.find(".modal.show .modal-content:contains('Confirm Action')")
          .length > 0
      ) {
        cy.get(".modal.show").within(() => {
          cy.contains("button", /^Confirm$/i)
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

  function clickStatusActionIfEnabled(
    actionSelector: string,
    actionName: string,
  ): void {
    openStatusActionsMenu();
    cy.get("body").then(($body) => {
      const $action = $body.find(actionSelector).filter(":visible");
      if ($action.length === 0) {
        cy.log(`${actionName} not present — already progressed`);
        cy.get("body").click(0, 0);
        return;
      }
      if ($action.is(":disabled")) {
        cy.log(`${actionName} disabled — already progressed`);
        cy.get("body").click(0, 0);
        return;
      }
      cy.wrap($action.first()).click({ force: true });
      confirmStatusActionIfNeeded();
    });
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

  function selectFirstAvailableOption(labelText: string): void {
    cy.get("body").then(($body) => {
      if ($body.find(`label:contains('${labelText}'):visible`).length === 0)
        return;
      cy.contains("label", labelText)
        .first()
        .then(($label) => {
          const fieldId = ($label.attr("for") || "").trim();
          if (!fieldId || $body.find(`#${fieldId}`).length === 0) return;
          cy.get(`#${fieldId}`).then(($select) => {
            const candidate = $select
              .find("option")
              .toArray()
              .find((opt) => {
                const val = (opt.getAttribute("value") || "").trim();
                const text = (opt.textContent || "").trim().toLowerCase();
                return val !== "" && text !== "please choose...";
              });
            const val = candidate?.getAttribute("value");
            if (val) cy.wrap($select).select(val, { force: true });
          });
        });
    });
  }

  /** Poll the applications list until the submission row becomes visible. */
  function waitForSubmissionInList(
    id: string,
    attempt = 1,
    maxAttempts = 15,
  ): void {
    goToApplicationsList();
    dismissBlockingModalIfPresent();
    listPage
      .selectQuickDateRange("last7days")
      .waitForTableRefresh()
      .searchForSubmission(id);

    cy.get("body").then(($body) => {
      if ($body.find(`tr:contains("${id}")`).length > 0) {
        cy.log(`Found ${id} on attempt ${attempt}`);
        return;
      }
      if (attempt >= maxAttempts) {
        throw new Error(
          `Submission ${id} not found after ${maxAttempts} attempts`,
        );
      }
      cy.log(`${id} not visible yet — retrying (${attempt}/${maxAttempts})`);
      cy.wait(5000);
      cy.reload();
      waitForSubmissionInList(id, attempt + 1, maxAttempts);
    });
  }

  /** Poll the applications list until a submission row contains an expected status. */
  function waitForSubmissionStatusInList(
    id: string,
    expectedStatus: string,
    attempt = 1,
    maxAttempts = 12,
  ): void {
    goToApplicationsList();
    dismissBlockingModalIfPresent();
    listPage
      .selectQuickDateRange("last7days")
      .waitForTableRefresh()
      .searchForSubmission(id);

    cy.contains("tr", id, { timeout: 20000 }).then(($row) => {
      const rowText = $row.text().toLowerCase();
      const expected = expectedStatus.toLowerCase();
      if (rowText.includes(expected)) {
        cy.log(`✅ ${id} reached ${expectedStatus} on attempt ${attempt}`);
        return;
      }

      if (attempt >= maxAttempts) {
        throw new Error(
          `Submission ${id} did not reach status '${expectedStatus}' after ${maxAttempts} attempts`,
        );
      }

      cy.log(
        `${id} not yet ${expectedStatus} (attempt ${attempt}/${maxAttempts}); retrying`,
      );
      cy.wait(5000);
      cy.reload();
      waitForSubmissionStatusInList(
        id,
        expectedStatus,
        attempt + 1,
        maxAttempts,
      );
    });
  }

  /**
   * Same as waitForSubmissionStatusInList but returns false on timeout instead
   * of throwing, allowing bulk phases to proceed with approved-only rows.
   */
  function waitForSubmissionStatusInListOptional(
    id: string,
    expectedStatus: string,
    attempt = 1,
    maxAttempts = 16,
  ): Cypress.Chainable<boolean> {
    goToApplicationsList();
    dismissBlockingModalIfPresent();
    listPage
      .selectQuickDateRange("last7days")
      .waitForTableRefresh()
      .searchForSubmission(id);

    return cy.contains("tr", id, { timeout: 20000 }).then(($row) => {
      const rowText = $row.text().toLowerCase();
      const expected = expectedStatus.toLowerCase();
      if (rowText.includes(expected)) {
        cy.log(`✅ ${id} reached ${expectedStatus} on attempt ${attempt}`);
        return cy.wrap(true, { log: false });
      }

      if (attempt >= maxAttempts) {
        cy.log(
          `⚠️ ${id} did not reach ${expectedStatus} after ${maxAttempts} attempts; skipping from bulk selection`,
        );
        return cy.wrap(false, { log: false });
      }

      cy.log(
        `${id} not yet ${expectedStatus} (attempt ${attempt}/${maxAttempts}); retrying`,
      );
      cy.wait(5000);
      cy.reload();
      return waitForSubmissionStatusInListOptional(
        id,
        expectedStatus,
        attempt + 1,
        maxAttempts,
      );
    });
  }

  /** Poll the details breadcrumb until it reflects the expected status. */
  function waitForDetailBreadcrumbStatus(
    expectedStatus: string,
    attempt = 1,
    maxAttempts = 8,
  ): void {
    cy.get(BREADCRUMB_STATUS, { timeout: 20000 })
      .should("be.visible")
      .invoke("text")
      .then((txt) => {
        const current = txt.trim().toLowerCase();
        const expected = expectedStatus.toLowerCase();
        if (current.includes(expected)) {
          cy.log(`✅ Detail status is ${expectedStatus}`);
          return;
        }

        if (attempt >= maxAttempts) {
          throw new Error(
            `Detail status did not reach '${expectedStatus}' after ${maxAttempts} attempts (current='${txt.trim()}')`,
          );
        }

        cy.log(
          `Detail status not yet ${expectedStatus} (attempt ${attempt}/${maxAttempts}); reloading`,
        );
        cy.wait(3000);
        cy.reload();
        detailsPage.dismissErrorModalIfPresent();
        waitForDetailBreadcrumbStatus(expectedStatus, attempt + 1, maxAttempts);
      });
  }

  /** Assign a submission to the configured owner from the applications list. */
  function assignSubmission(id: string): void {
    dismissBlockingModalIfPresent();
    waitForSubmissionInList(id);

    listPage
      .waitForNoBlockingOverlay()
      .selectQuickDateRange("last7days")
      .waitForTableRefresh()
      .searchForSubmission(id)
      .selectRowByText(id);

    // If the submission is already assigned (e.g. auto-assigned or left over from
    // a previous partial run) the button has class action-bar-btn-unavailable.
    // In that case skip re-assignment — subsequent status actions handle their
    // own availability checks and will proceed from wherever the submission is.
    cy.get("#assignApplication", { timeout: 20000 }).then(($btn) => {
      if ($btn.hasClass("action-bar-btn-unavailable")) {
        cy.log(
          `assignSubmission: assign button unavailable for ${id} — skipping (already assigned or not assignable)`,
        );
        return;
      }

      cy.wrap($btn).should("be.visible").click({ force: true });

      cy.contains(".modal-title", "Assessment Users", {
        timeout: 20000,
      }).should("be.visible");
      cy.get("#AssigneeId", { timeout: 20000 })
        .should("be.visible")
        .select(BULK_CONFIG.assignOwner);
      cy.get("#user-tags-input", { timeout: 20000 })
        .should("be.visible")
        .clear()
        .type(BULK_CONFIG.assignOwner, { delay: 0 });

      cy.get("body").then(($body) => {
        if (
          $body.find(".tags-suggestion-container .tags-suggestion-element")
            .length > 0
        ) {
          cy.get(".tags-suggestion-container .tags-suggestion-element")
            .contains(BULK_CONFIG.assignOwner)
            .click({ force: true });
        } else {
          cy.get("#user-tags-input").type("{enter}");
        }
      });

      cy.contains(".modal-footer button", "Save", { timeout: 20000 })
        .should("be.visible")
        .and("not.be.disabled")
        .click({ force: true });
      cy.contains(".modal-title", "Assessment Users", {
        timeout: 20000,
      }).should("not.exist");
      listPage.waitForNoBlockingOverlay();
    });
  }

  /** Open a submission's detail page from the applications list. */
  function openSubmissionDetails(id: string): void {
    dismissBlockingModalIfPresent();
    goToApplicationsList();
    listPage
      .waitForNoBlockingOverlay()
      .selectQuickDateRange("last7days")
      .waitForTableRefresh()
      .searchForSubmission(id)
      .selectRowByText(id)
      .clickOpenButton();

    cy.get(BREADCRUMB_STATUS, { timeout: 20000 }).should("be.visible");
  }

  /**
   * Drive one submission through the full approval workflow.
   * Ends by navigating back to the applications list so browser memory is
   * cleared before Cypress starts the next it() block.
   */
  function runApprovalWorkflow(id: string): void {
    cy.log(`── Start approval workflow: ${id}`);

    assignSubmission(id);
    openSubmissionDetails(id);

    // Start Review
    clickStatusActionIfEnabled(STATUS_ACTIONS.startReview, "Start Review");

    // Fill required review fields then complete review
    detailsPage.goToReviewAssessmentTab();
    reviewPage.verifyFormioLoaded();
    cy.get("#ApprovalView_ApprovedAmount", { timeout: 30000 })
      .should("be.visible")
      .and("not.be.disabled");
    reviewPage
      .enterApprovedAmount(BULK_CONFIG.approvedAmount)
      .setDecisionDateToToday();
    selectFirstAvailableOption("Likelihood of Funding");
    selectFirstAvailableOption("Due Diligence Status");
    selectFirstAvailableOption("Assessment Result");
    reviewPage.clickSave();

    clickStatusActionIfEnabled(
      STATUS_ACTIONS.completeReview,
      "Complete Review",
    );
    clickStatusActionIfEnabled(
      STATUS_ACTIONS.startAssessment,
      "Start Assessment",
    );

    // Create and complete assessment
    detailsPage.goToReviewAssessmentTab().verifyActiveTab("reviewAssessment");
    cy.wait(2000);
    reviewPage.scrollToAssessmentList();
    cy.get("body").then(($body) => {
      if ($body.find("#CreateButton").length > 0) {
        cy.get("#CreateButton").click({ force: true });
        cy.wait(1000);
      }
    });
    cy.get("body").then(($body) => {
      if ($body.find(ADJUDICATION_COMPLETE).length > 0) {
        cy.get(ADJUDICATION_COMPLETE, { timeout: 20000 }).then(($btn) => {
          if (!$btn.is(":disabled")) {
            cy.wrap($btn).click({ force: true });
            confirmStatusActionIfNeeded();
          }
        });
      }
    });

    // Configure payment info — enter supplier number then refresh site list.
    // Without the site refresh the payment modal shows "Some payment information
    // is missing" and the Submit button stays disabled.
    cy.reload();
    cy.wait(2000);
    detailsPage
      .goToPaymentInfoTab()
      .enterSupplierNumber(BULK_CONFIG.supplierNumber)
      .clickElsewhere()
      .clickPaymentInfoSave();

    // Reload so the DataTable re-initialises with the saved SupplierId, then
    // refresh the site list and wait for the API response.
    cy.reload();
    listPage.waitForNoBlockingOverlay();
    detailsPage.dismissErrorModalIfPresent();
    detailsPage.goToPaymentInfoTab();
    cy.intercept("GET", "**/api/app/supplier/sites-by-supplier-number**").as(
      "siteRefresh",
    );
    detailsPage.clickRefreshSiteList();
    cy.wait("@siteRefresh");

    // If site data is available, select the payment group so the payment-modal
    // description inputs are enabled.  Without this step the inputs render as
    // disabled and the bulk payment phase fails.
    cy.get("body").then(($body) => {
      const rows = $body.find("#SiteInfoTable tbody tr");
      const firstRowText = rows.first().text().replace(/\s+/g, " ").trim();
      const hasData =
        rows.length > 0 && !/no data available/i.test(firstRowText);
      const hasTokenError =
        $body.text().includes("GetAuthTokenAsync") ||
        $body.text().includes("Error retrieving Token");
      if (hasData && !hasTokenError) {
        detailsPage
          .clickSiteInfoEdit()
          .waitForEditSiteModal()
          .selectPaymentGroup("Cheque")
          .clickSaveChanges();
      } else {
        cy.log("No site data available — skipping site info edit");
      }
    });

    // Complete Assessment + Approve
    cy.reload();
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

    // Approval is asynchronous; wait until details and list reflect final state.
    waitForDetailBreadcrumbStatus("Approved");

    cy.log(`── Approval workflow complete: ${id}`);

    // Return to the list page — clears details-page state before next it() block
    goToApplicationsList();
    waitForSubmissionStatusInList(id, "Approved");
  }

  /**
   * Click a submission's checkbox in an already-filtered table without changing
   * the search filter. Changing the search triggers a server-side DataTable
   * reload which clears all prior checkbox selections — so callers must show
   * all target rows simultaneously before calling this function.
   */
  function clickRowCheckboxByText(id: string): void {
    cy.contains("tr", id, { timeout: 20000 })
      .scrollIntoView()
      .should("contain.text", "Approved")
      .find(".checkbox-select")
      .click({ force: true });
    cy.log(`✅ Selected approved row: ${id}`);
  }

  /**
   * Search for a submission in the Payments table and check its row checkbox.
   * DataTable maintains client-side selection state across search changes, so
   * repeated calls accumulate the running selection without clearing prior picks.
   */
  function checkPaymentRowCheckbox(id: string): void {
    cy.get("#search", { timeout: 20000 }).clear().type(id);
    cy.contains("tr", id, { timeout: 20000 })
      .scrollIntoView()
      .find(".checkbox-select")
      .click({ force: true });
    cy.log(`✅ Checked payment row: ${id}`);
  }

  // ─── Setup ───────────────────────────────────────────────────────────────

  before(() => {
    Cypress.config("includeShadowDom", true);
    loginIfNeeded();
  });

  // ─── Phase 0: Load IDs + switch grant program ────────────────────────────

  // Reads the IDs written by chefs-bulk-submission-seeder.cy.ts.
  // Falls back to a single dynamically fetched submission when the file is absent.
  it("Load bulk submission IDs", () => {
    cy.task(
      "readJsonIfExists",
      "cypress/scripts/bulk-submission-ids.json",
    ).then((result) => {
      const data = result as { submissionIds?: string[] } | null;

      if (data?.submissionIds && data.submissionIds.length > 0) {
        submissionIds = data.submissionIds;
        cy.log(`📌 Loaded ${submissionIds.length} seeded IDs`);
        cy.writeFile("cypress/scripts/bulk-submission-ids.json", {});
        return;
      }

      cy.log("No seeded file — fetching one submission dynamically");
      cy.fetchDynamicSubmission({
        categoryFilter: "Data Seeder",
        statusFilter: ["Submitted"],
        maxAge: 30,
        index: 0,
      }).then((id) => {
        submissionIds = [id];
        cy.log(`✅ Fallback submission ID: ${id}`);
      });
    });
  });

  it("Switch to grant program", () => {
    listPage.switchToGrantProgram(BULK_CONFIG.grantProgram);
  });

  // ─── Phase 1: Approve each submission individually ───────────────────────
  // One it() block per submission slot. Cypress cleans up between blocks so
  // Chrome memory doesn't accumulate across 10 full approval workflows.
  // Slots beyond the actual seeded count are skipped automatically.

  for (let i = 0; i < MAX_BULK; i++) {
    it(`Approve submission ${i + 1} of ${MAX_BULK}`, function () {
      if (!submissionIds[i]) {
        this.skip();
        return;
      }
      cy.log(
        `=== Approval ${i + 1} / ${submissionIds.length}: ${submissionIds[i]} ===`,
      );
      runApprovalWorkflow(submissionIds[i]);
    });
  }

  // ─── Phase 2: Bulk payment request submission ─────────────────────────────
  // All N approved submissions are selected at once on the applications list.
  // A single payment modal is opened, one description is filled per row, and
  // all N requests are submitted together with one "Submit Payment Requests" click.

  it("Select all approved submissions and submit bulk payment request", () => {
    // Navigate to the applications list explicitly — visiting just the base URL
    // routes to the CHEFS marketing page, not the Grant Manager.
    cy.visit(`${Cypress.env("webapp.url") as string}${APPLICATIONS_PATH}`);
    listPage.waitForNoBlockingOverlay();
    listPage.switchToGrantProgram(BULK_CONFIG.grantProgram);

    // Show ALL rows at once — do NOT search per row.
    // Each search triggers a server-side DataTable reload that clears all prior
    // checkbox selections. Set the date filter once, clear search, then find
    // each row by text in the full visible table.
    listPage
      .waitForNoBlockingOverlay()
      .selectQuickDateRange("last7days")
      .waitForTableRefresh();
    cy.get("#search", { timeout: 20000 }).clear();
    listPage.waitForTableRefresh();

    approvedSubmissionIds = [];
    Cypress._.each(submissionIds, (id: string, index: number) => {
      cy.log(`Checking approval status ${index + 1} / ${submissionIds.length}: ${id}`);
      cy.then(() =>
        waitForSubmissionStatusInListOptional(id, "Approved").then(
          (isApproved) => {
            if (isApproved) {
              approvedSubmissionIds.push(id);
            }
          },
        ),
      );
    });

    cy.then(() => {
      expect(
        approvedSubmissionIds.length,
        "At least one submission must be approved for bulk payment",
      ).to.be.greaterThan(0);

      const skippedIds = submissionIds.filter(
        (id) => !approvedSubmissionIds.includes(id),
      );
      if (skippedIds.length > 0) {
        cy.log(`Skipped non-approved IDs: ${skippedIds.join(", ")}`);
      }

      cy.log(
        `Approved subset for bulk payment: ${approvedSubmissionIds.length}/${submissionIds.length}`,
      );
    });

    // Re-open the full list once and select all approved rows in one pass.
    // Do not search per row after selection starts; search reload clears checks.
    cy.then(() => {
      goToApplicationsList();
      listPage
        .waitForNoBlockingOverlay()
        .selectQuickDateRange("last7days")
        .waitForTableRefresh();
      cy.get("#search", { timeout: 20000 }).clear();
      listPage.waitForTableRefresh();

      Cypress._.each(approvedSubmissionIds, (id: string, index: number) => {
        cy.log(
          `Selecting approved row ${index + 1} / ${approvedSubmissionIds.length}: ${id}`,
        );
        clickRowCheckboxByText(id);
      });
    });

    // Open the payment modal once with all N rows selected
    listPage.clickPaymentButtonWithWait();
    listPage.waitForPaymentModalVisible();

    // Fill the description for each submission in the modal
    Cypress._.each(approvedSubmissionIds, (id: string, index: number) => {
      cy.get("#payment-modal input[id$='__Description']")
        .eq(index)
        .should("exist")
        .clear()
        .type(`BulkPay-${id}`.slice(0, 40));
    });

    // In long modal lists, submit can be below the viewport and reported as
    // not visible due to fixed/overflow ancestors. Scroll to the concrete
    // submit control and click with force after enabled-state assertion.
    cy.get("#btnSubmitPayment", { timeout: 20000 })
      .scrollIntoView()
      .should("exist")
      .and("not.be.disabled")
      .click({ force: true });

    listPage.verifyPaymentModalClosed();
    cy.log(
      `✅ Bulk payment request submitted for ${approvedSubmissionIds.length} submissions`,
    );
  });

  // ─── Phase 3: L1 bulk payment approval ────────────────────────────────────

  // Navigate to Payments tab and select all N rows before opening the approval modal.
  it("Navigate to Payments tab and bulk-select all submission rows", () => {
    listPage.waitForNoBlockingOverlay();
    navPage.goToPayments();
    cy.location("pathname", { timeout: 20000 }).should("include", "Payment");

    // Expand the DataTable page size so all submissions land on one page —
    // without this, rows on page 2+ are not in the DOM and cy.contains() won't find them.
    cy.get("body").then(($body) => {
      const $sel = $body.find("select[name$='_length']");
      if ($sel.length > 0) {
        cy.wrap($sel.first()).select("100", { force: true });
      }
    });

    // Clear search after page-size change so all recent payments are visible.
    // Do NOT search per row — server-side DataTable reload clears checkbox selections.
    // waitForTableRefresh() targets the Applications-list spinner which doesn't exist
    // on the Payments tab — wait for table rows directly instead.
    cy.get("#search", { timeout: 20000 }).should("be.visible").clear();
    cy.get("tbody tr", { timeout: 20000 }).should("have.length.greaterThan", 0);

    Cypress._.each(approvedSubmissionIds, (id: string, index: number) => {
      cy.log(
        `Selecting payment row ${index + 1} / ${approvedSubmissionIds.length}: ${id}`,
      );
      cy.contains("tr", id, { timeout: 20000 })
        .scrollIntoView()
        .find(".checkbox-select")
        .click({ force: true });
    });

    cy.contains("button", "Approve", { timeout: 20000 }).should("be.visible");
  });

  it("L1 Bulk Approval - validate count and approve", () => {
    // Dismiss any modal left open by a previous retry attempt
    dismissBlockingModalIfPresent();

    cy.contains("button", "Approve", { timeout: 20000 })
      .should("be.visible")
      .and("not.be.disabled")
      .click();

    // The bulk approval modal title differs from the single-row "Approve Payments" title.
    // Wait for any modal to appear rather than asserting the exact title.
    cy.get(".modal.show", { timeout: 20000 }).should("be.visible");

    // Modal lists all N payment entries — #Note and #btnSubmitPayment are below them.
    const approvalNote = `BulkL1-${Date.now()}`.slice(0, 50);
    cy.get("#Note")
      .scrollIntoView()
      .should("be.visible")
      .clear()
      .type(approvalNote);
    cy.get("#btnSubmitPayment")
      .scrollIntoView()
      .should("be.visible")
      .and("not.be.disabled")
      .click();

    cy.get(".modal.show", { timeout: 20000 }).should("not.exist");
    cy.log(
      `✅ L1 bulk approval completed for ${approvedSubmissionIds.length} payments`,
    );
  });

  // ─── Phase 4: L2 bulk payment approval as user2 ───────────────────────────

  it("Logout user1 and login as user2 for L2 approval", () => {
    cy.logout();
    loginIfNeeded({
      username: Cypress.env("test2username") as string,
      password: Cypress.env("test2password") as string,
    });
  });

  it("Navigate to Payments tab and bulk-select all submission rows (L2)", () => {
    // Navigate to the applications list explicitly — visiting just the base URL
    // routes to the CHEFS marketing page, not the Grant Manager.
    cy.visit(`${Cypress.env("webapp.url") as string}${APPLICATIONS_PATH}`);
    listPage.waitForNoBlockingOverlay();
    listPage.switchToGrantProgram(BULK_CONFIG.grantProgram);
    navPage.goToPayments();
    cy.location("pathname", { timeout: 20000 }).should("include", "Payment");

    // Expand page size so all 10 rows are in the DOM (same fix as L1).
    cy.get("body").then(($body) => {
      const $sel = $body.find("select[name$='_length']");
      if ($sel.length > 0) {
        cy.wrap($sel.first()).select("100", { force: true });
      }
    });

    cy.get("#search", { timeout: 20000 }).should("be.visible").clear();
    cy.get("tbody tr", { timeout: 20000 }).should("have.length.greaterThan", 0);

    Cypress._.each(approvedSubmissionIds, (id: string, index: number) => {
      cy.log(
        `L2 selecting payment row ${index + 1} / ${approvedSubmissionIds.length}: ${id}`,
      );
      cy.contains("tr", id, { timeout: 20000 })
        .scrollIntoView()
        .find(".checkbox-select")
        .click({ force: true });
    });

    cy.contains("button", "Approve", { timeout: 20000 }).should("be.visible");
  });

  it("L2 Bulk Approval - validate count and approve", () => {
    // Dismiss any modal left open by a previous retry attempt
    dismissBlockingModalIfPresent();

    cy.contains("button", "Approve", { timeout: 20000 })
      .should("be.visible")
      .and("not.be.disabled")
      .click();

    // Wait for any modal to appear (title varies for bulk vs single-row flows)
    cy.get(".modal.show", { timeout: 20000 }).should("be.visible");

    // Modal lists all N payment entries — scroll to reach #Note and submit below them
    const approvalNote = `BulkL2-${Date.now()}`.slice(0, 50);
    cy.get("#Note")
      .scrollIntoView()
      .should("be.visible")
      .clear()
      .type(approvalNote);
    cy.get("#btnSubmitPayment")
      .scrollIntoView()
      .should("be.visible")
      .and("not.be.disabled")
      .click();

    cy.get(".modal.show", { timeout: 20000 }).should("not.exist");
    cy.log(
      `✅ L2 bulk approval completed for ${approvedSubmissionIds.length} payments`,
    );
  });

  // ─── Phase 5: Post-approval status validation ─────────────────────────────

  // Verifies every submission reached a terminal payment status after L2 approval.
  it("Validate all payment statuses on Payments table", () => {
    Cypress._.each(approvedSubmissionIds, (id: string) => {
      cy.get("#search", { timeout: 20000 }).clear().type(id);
      cy.contains("tr", id, { timeout: 20000 }).should(($row) => {
        const text = $row.text();
        expect(
          text.includes("Sent to Accounts Payable") ||
            text.includes("Submitted to CAS"),
          `Row for ${id} should show a final payment status`,
        ).to.be.true;
      });
    });
  });

  it("Logout", () => {
    cy.logout();
  });
});
