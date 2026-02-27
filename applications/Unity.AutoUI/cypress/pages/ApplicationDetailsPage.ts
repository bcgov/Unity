/// <reference types="cypress" />

import { BasePage } from "./BasePage";

/**
 * ApplicationsListPage - Page Object for the Grant Applications List page
 * Handles action bar, filters, table operations, columns menu, and modals
 */
export class ApplicationsListPage extends BasePage {
  private readonly STANDARD_TIMEOUT = 20000;
  private readonly BUTTON_TIMEOUT = 60000;

  // Date filter selectors
  private readonly dateFilters = {
    submittedFromDate: "input#submittedFromDate",
    submittedToDate: "input#submittedToDate",
    spinner: 'div.spinner-grow[role="status"]',
  };

  // Action bar selectors
  private readonly actionBar = {
    customButtons: "#app_custom_buttons",
    dynamicButtonContainer: "#dynamicButtonContainerId",
    paymentButton: "#applicationPaymentRequest",
    exportButton: "#dynamicButtonContainerId .dt-buttons button span",
    saveViewButton: "button.grp-savedStates",
    columnsButton: "span",
  };

  // Table selectors
  private readonly table = {
    scrollBody: ".dt-scroll-body",
    tableRows: ".dt-scroll-body tbody tr",
    scrollHead: ".dt-scroll-head",
    columnTitles: ".dt-scroll-head span.dt-column-title",
  };

  // Columns menu selectors
  private readonly columnsMenu = {
    dropdownItem: "a.dropdown-item",
    buttonBackground: "div.dt-button-background",
  };

  // Payment modal selectors
  private readonly paymentModal = {
    modal: "#payment-modal",
    backdrop: ".modal-backdrop",
    cancelButton: "#payment-modal .modal-footer button",
  };

  // Grant program selectors
  private readonly grantProgram = {
    userInitials: ".unity-user-initials",
    userDropdown: "#user-dropdown a.dropdown-item",
    searchInput: "#search-grant-programs",
    programsTable: "#UserGrantProgramsTable",
    programsTableRow: "#UserGrantProgramsTable tbody tr",
  };

  // Save view selectors
  private readonly saveView = {
    button: "button.grp-savedStates",
    resetOption: "a.dropdown-item",
  };

  constructor() {
    super();
  }

  // ============ Date Filter Methods ============

  /**
   * Set the Submitted From Date filter
   */
  setSubmittedFromDate(date: string): this {
    cy.get(this.dateFilters.submittedFromDate, { timeout: this.STANDARD_TIMEOUT })
      .click({ force: true })
      .clear({ force: true })
      .type(date, { force: true })
      .trigger("change", { force: true })
      .blur({ force: true })
      .should("have.value", date);
    return this;
  }

  /**
   * Set the Submitted To Date filter
   */
  setSubmittedToDate(date: string): this {
    cy.get(this.dateFilters.submittedToDate, { timeout: this.STANDARD_TIMEOUT })
      .click({ force: true })
      .clear({ force: true })
      .type(date, { force: true })
      .trigger("change", { force: true })
      .blur({ force: true })
      .should("have.value", date);
    return this;
  }

  /**
   * Wait for table refresh (spinner to be hidden)
   */
  waitForTableRefresh(): this {
    cy.get(this.dateFilters.spinner, { timeout: this.STANDARD_TIMEOUT }).then(
      ($s: JQuery<HTMLElement>) => {
        cy.wrap($s)
          .should("have.attr", "style")
          .and("contain", "display: none");
      }
    );
    return this;
  }

  /**
   * Get today's date in ISO local format (YYYY-MM-DD)
   */
  getTodayIsoLocal(): string {
    const d = new Date();
    const pad2 = (n: number) => String(n).padStart(2, "0");
    return `${d.getFullYear()}-${pad2(d.getMonth() + 1)}-${pad2(d.getDate())}`;
  }

  // ============ Table Methods ============

  /**
   * Verify table has rows
   */
  verifyTableHasData(): this {
    cy.get(this.table.tableRows, { timeout: this.STANDARD_TIMEOUT }).should(
      "have.length.greaterThan",
      1
    );
    return this;
  }

  /**
   * Select a row by index (clicks on a non-link cell)
   */
  selectRowByIndex(rowIndex: number, withCtrl = false): this {
    cy.get(this.table.tableRows, { timeout: this.STANDARD_TIMEOUT })
      .eq(rowIndex)
      .find("td")
      .not(":has(a)")
      .first()
      .click({ force: true, ctrlKey: withCtrl });
    return this;
  }

  /**
   * Select multiple rows by indices
   */
  selectMultipleRows(indices: number[]): this {
    indices.forEach((index, i) => {
      this.selectRowByIndex(index, i > 0);
    });
    return this;
  }

  /**
   * Scroll table horizontally to a specific position
   */
  scrollTableHorizontally(x: number): this {
    cy.get(this.table.scrollBody, { timeout: this.STANDARD_TIMEOUT })
      .should("exist")
      .scrollTo(x, 0, { duration: 0, ensureScrollable: false });
    return this;
  }

  /**
   * Get visible header titles from the table
   */
  getVisibleHeaderTitles(): Cypress.Chainable<string[]> {
    return cy
      .get(this.table.columnTitles, { timeout: this.STANDARD_TIMEOUT })
      .then(($els: JQuery<HTMLElement>) => {
        const titles: string[] = Cypress.$($els)
          .toArray()
          .map((el: HTMLElement) => (el.textContent || "").replace(/\s+/g, " ").trim())
          .filter((t: string) => t.length > 0);
        return titles;
      });
  }

  /**
   * Assert that visible headers include expected columns
   */
  assertVisibleHeadersInclude(expected: string[]): this {
    this.getVisibleHeaderTitles().then((titles: string[]) => {
      expected.forEach((e: string) => {
        expect(titles, `visible headers should include "${e}"`).to.include(e);
      });
    });
    return this;
  }

  // ============ Action Bar Methods ============

  /**
   * Scroll to and verify action bar exists
   */
  verifyActionBarExists(): this {
    cy.get(this.actionBar.customButtons, { timeout: this.STANDARD_TIMEOUT })
      .should("exist")
      .scrollIntoView();
    return this;
  }

  /**
   * Click the Payment button
   */
  clickPaymentButton(): this {
    cy.get(this.actionBar.paymentButton, { timeout: this.BUTTON_TIMEOUT })
      .should("be.visible")
      .and("not.be.disabled")
      .click({ force: true });
    return this;
  }

  /**
   * Verify Export button is visible
   */
  verifyExportButtonVisible(): this {
    cy.contains(this.actionBar.exportButton, "Export", {
      timeout: this.STANDARD_TIMEOUT,
    }).should("be.visible");
    return this;
  }

  /**
   * Verify Save View button is visible
   */
  verifySaveViewButtonVisible(): this {
    cy.contains(
      "#dynamicButtonContainerId button.grp-savedStates",
      "Save View",
      { timeout: this.STANDARD_TIMEOUT }
    ).should("be.visible");
    return this;
  }

  /**
   * Verify Columns button is visible
   */
  verifyColumnsButtonVisible(): this {
    cy.contains(
      "#dynamicButtonContainerId .dt-buttons button span",
      "Columns",
      { timeout: this.STANDARD_TIMEOUT }
    ).should("be.visible");
    return this;
  }

  /**
   * Verify dynamic button container exists
   */
  verifyDynamicButtonContainerExists(): this {
    cy.get(this.actionBar.dynamicButtonContainer, {
      timeout: this.STANDARD_TIMEOUT,
    })
      .should("exist")
      .scrollIntoView();
    return this;
  }

  // ============ Payment Modal Methods ============

  /**
   * Wait for payment modal to be visible
   */
  waitForPaymentModalVisible(): this {
    cy.get(this.paymentModal.modal, { timeout: this.STANDARD_TIMEOUT })
      .should("be.visible")
      .and("have.class", "show");
    return this;
  }

  /**
   * Close payment modal using multiple strategies
   */
  closePaymentModal(): this {
    // Attempt ESC key
    cy.get("body").type("{esc}", { force: true });

    // Click backdrop if present
    cy.get(this.paymentModal.backdrop, { timeout: this.STANDARD_TIMEOUT }).then(
      ($bd: JQuery<HTMLElement>) => {
        if ($bd.length) {
          cy.wrap($bd).click("topLeft", { force: true });
        }
      }
    );

    // Try Cancel button if available
    cy.contains(this.paymentModal.cancelButton, "Cancel", {
      timeout: this.STANDARD_TIMEOUT,
    }).then(($btn: JQuery<HTMLElement>) => {
      if ($btn && $btn.length > 0) {
        cy.wrap($btn).scrollIntoView().click({ force: true });
      } else {
        cy.log("Cancel button not present, proceeding to hard-close fallback");
      }
    });

    // Hard close fallback using jQuery
    cy.window().then((win: Cypress.AUTWindow) => {
      try {
        // eslint-disable-next-line @typescript-eslint/no-explicit-any
        const windowWithModal = win as any;
        if (typeof windowWithModal.closePaymentModal === "function") {
          windowWithModal.closePaymentModal();
        }
      } catch {
        /* ignore */
      }

      // eslint-disable-next-line @typescript-eslint/no-explicit-any
      const $ = (win as any).jQuery || (win as any).$;
      if ($) {
        try {
          $("#payment-modal")
            .removeClass("show")
            .attr("aria-hidden", "true")
            .css("display", "none");
          $(".modal-backdrop").remove();
          $("body").removeClass("modal-open").css("overflow", "");
        } catch {
          /* ignore */
        }
      }
    });
    return this;
  }

  /**
   * Verify payment modal is closed
   */
  verifyPaymentModalClosed(): this {
    cy.get(this.paymentModal.modal, { timeout: this.STANDARD_TIMEOUT }).should(
      ($m: JQuery<HTMLElement>) => {
        const isHidden = !$m.is(":visible") || !$m.hasClass("show");
        expect(isHidden, "payment-modal hidden or not shown").to.eq(true);
      }
    );
    cy.get(this.paymentModal.backdrop, { timeout: this.STANDARD_TIMEOUT }).should(
      "not.exist"
    );
    return this;
  }

  // ============ Columns Menu Methods ============

  /**
   * Close any open dropdowns or modals
   */
  closeOpenDropdowns(): this {
    cy.get("body").then(($body: JQuery<HTMLBodyElement>) => {
      if ($body.find(this.columnsMenu.buttonBackground).length > 0) {
        cy.get(this.columnsMenu.buttonBackground).click({ force: true });
      }
    });
    return this;
  }

  /**
   * Open Save View dropdown and reset to default
   */
  resetToDefaultView(): this {
    cy.get(this.saveView.button, { timeout: this.STANDARD_TIMEOUT })
      .should("be.visible")
      .and("contain.text", "Save View")
      .click();

    cy.contains(this.saveView.resetOption, "Reset to Default View", {
      timeout: this.STANDARD_TIMEOUT,
    })
      .should("exist")
      .click({ force: true });

    // Wait for table to rebuild
    cy.get(this.table.columnTitles, { timeout: this.STANDARD_TIMEOUT }).should(
      "have.length.gt",
      5
    );
    return this;
  }

  /**
   * Open the Columns menu
   */
  openColumnsMenu(): this {
    cy.contains("span", "Columns", { timeout: this.STANDARD_TIMEOUT })
      .should("be.visible")
      .click();

    // Wait for dropdown to be fully populated
    cy.get(this.columnsMenu.dropdownItem, { timeout: this.STANDARD_TIMEOUT }).should(
      "have.length.gt",
      50
    );
    return this;
  }

  /**
   * Click a column item in the Columns menu
   */
  clickColumnsItem(label: string): this {
    cy.contains(this.columnsMenu.dropdownItem, label, {
      timeout: this.STANDARD_TIMEOUT,
    })
      .should("exist")
      .scrollIntoView()
      .click({ force: true });
    return this;
  }

  /**
   * Toggle multiple columns (click each one)
   */
  toggleColumns(columns: string[]): this {
    columns.forEach((column) => {
      this.clickColumnsItem(column);
    });
    return this;
  }

  /**
   * Close the Columns menu
   */
  closeColumnsMenu(): this {
    cy.get(this.columnsMenu.buttonBackground, { timeout: this.STANDARD_TIMEOUT })
      .should("exist")
      .click({ force: true });

    cy.get(this.columnsMenu.buttonBackground, {
      timeout: this.STANDARD_TIMEOUT,
    }).should("not.exist");
    return this;
  }

  // ============ Grant Program Methods ============

  /**
   * Switch to a specific grant program if available
   */
  switchToGrantProgram(programName: string): this {
    cy.get("body").then(($body: JQuery<HTMLBodyElement>) => {
      const hasUserInitials =
        $body.find(this.grantProgram.userInitials).length > 0;

      if (!hasUserInitials) {
        cy.log("Skipping tenant switch: no user initials menu found");
        return;
      }

      cy.get(this.grantProgram.userInitials).click();

      cy.get("body").then(($body2: JQuery<HTMLBodyElement>) => {
        const switchLink = $body2
          .find(this.grantProgram.userDropdown)
          .filter((_: number, el: HTMLElement) => {
            return (el.textContent || "").trim() === "Switch Grant Programs";
          });

        if (switchLink.length === 0) {
          cy.log(
            'Skipping tenant switch: "Switch Grant Programs" not present for this user/session'
          );
          cy.get("body").click(0, 0);
          return;
        }

        cy.wrap(switchLink.first()).click();

        cy.url({ timeout: this.STANDARD_TIMEOUT }).should(
          "include",
          "/GrantPrograms"
        );

        cy.get(this.grantProgram.searchInput, { timeout: this.STANDARD_TIMEOUT })
          .should("be.visible")
          .clear()
          .type(programName);

        cy.contains(this.grantProgram.programsTableRow, programName, {
          timeout: this.STANDARD_TIMEOUT,
        })
          .should("exist")
          .within(() => {
            cy.contains("button", "Select").should("be.enabled").click();
          });

        cy.location("pathname", { timeout: this.STANDARD_TIMEOUT }).should(
          (p: string) => {
            expect(
              p.indexOf("/GrantApplications") >= 0 || p.indexOf("/auth/") >= 0
            ).to.eq(true);
          }
        );
      });
    });
    return this;
  }
}

/**
 * ApplicationDetailsPage - Page Object for the Application Details page
 * Handles tabs, status actions, and field verification
 */
export class ApplicationDetailsPage extends BasePage {
  // Tab selectors
  private readonly tabs = {
    submission: "#nav-summery-tab",
    reviewAssessment: "#nav-review-and-assessment-tab",
    projectInfo: "#nav-project-info-tab",
    applicantInfo: "#nav-organization-info-tab",
    fundingAgreement: "#nav-funding-agreement-info-tab",
    paymentInfo: "#nav-payment-info-tab",
  };

  // Status Actions dropdown selectors
  private readonly statusActions = {
    dropdown: "#ApplicationActionDropdown",
    dropdownToggle: "#ApplicationActionDropdown .dropdown-toggle",
    dropdownMenu: "#ApplicationActionDropdown .dropdown-menu",
    startReview: "#Application_StartReviewButton",
    completeReview: "#Application_CompleteReviewButton",
    startAssessment: "#Application_StartAssessmentButton",
    completeAssessment: "#Application_CompleteAssessmentButton",
    approve: "#Application_ApproveButton",
    deny: "#Application_DenyButton",
    close: "#Application_CloseButton",
    withdraw: "#Application_WithdrawButton",
    defer: "#Application_DeferButton",
    onHold: "#Application_OnHoldButton",
  };

  // Field selectors for Summary/Info Panel
  private readonly summaryFields = {
    category: "Category",
    organizationName: "OrganizationName",
    organizationNumber: "OrganizationNumber",
    economicRegion: "EconomicRegion",
    regionalDistrict: "RegionalDistrict",
    community: "Community",
    requestedAmount: "RequestedAmount",
    projectBudget: "ProjectBudget",
    sector: "Sector",
  };

  // Project Info tab field selectors
  private readonly projectInfoFields = {
    projectName: "#ProjectInfo_ProjectName",
    startDate: "#startDate",
    endDate: "#ProjectInfo_ProjectEndDate",
    requestedAmountPI: "#RequestedAmountInputPI",
    totalBudgetPI: "#TotalBudgetInputPI",
    acquisition: "#ProjectInfo_Acquisition",
    forestry: "#ProjectInfo_Forestry",
    forestryFocus: "#ProjectInfo_ForestryFocus",
    economicRegions: "#economicRegions",
    regionalDistricts: "#regionalDistricts",
    communities: "#communities",
    communityPopulation: "#ProjectInfo_CommunityPopulation",
    electoralDistrict: "#ProjectInfo_ElectoralDistrict",
    place: "#ProjectInfo_Place",
  };

  // Applicant Info tab field selectors
  private readonly applicantInfoFields = {
    fieldset: 'fieldset[name$="Applicant_Summary"]',
    orgName: "#ApplicantSummary_OrgName",
    orgNumber: "#ApplicantSummary_OrgNumber",
    contactFullName: "#ApplicantSummary_ContactFullName",
    contactTitle: "#ApplicantSummary_ContactTitle",
    contactEmail: "#ApplicantSummary_ContactEmail",
    contactBusinessPhone: "#ApplicantSummary_ContactBusinessPhone",
    contactCellPhone: "#ApplicantSummary_ContactCellPhone",
    physicalAddressStreet: "#ApplicantSummary_PhysicalAddressStreet",
    physicalAddressStreet2: "#ApplicantSummary_PhysicalAddressStreet2",
    physicalAddressUnit: "#ApplicantSummary_PhysicalAddressUnit",
    physicalAddressCity: "#ApplicantSummary_PhysicalAddressCity",
    physicalAddressProvince: "#ApplicantSummary_PhysicalAddressProvince",
    physicalAddressPostalCode: "#ApplicantSummary_PhysicalAddressPostalCode",
    mailingAddressStreet: "#ApplicantInfo_MailingAddressStreet",
    mailingAddressStreet2: "#ApplicantInfo_MailingAddressStreet2",
    mailingAddressUnit: "#ApplicantInfo_MailingAddressUnit",
    mailingAddressCity: "#ApplicantInfo_MailingAddressCity",
    mailingAddressProvince: "#ApplicantInfo_MailingAddressProvince",
    mailingAddressPostalCode: "#ApplicantInfo_MailingAddressPostalCode",
    signingAuthorityFullName: "#ApplicantInfo_SigningAuthorityFullName",
    signingAuthorityTitle: "#ApplicantInfo_SigningAuthorityTitle",
    signingAuthorityEmail: "#ApplicantInfo_SigningAuthorityEmail",
    signingAuthorityBusinessPhone:
      "#ApplicantInfo_SigningAuthorityBusinessPhone",
    signingAuthorityCellPhone: "#ApplicantInfo_SigningAuthorityCellPhone",
    sectorSubSectorIndustryDesc:
      "#ApplicantSummary_SectorSubSectorIndustryDesc",
    sector: "label.form-label",
    subSector: "label.form-label",
  };

  constructor() {
    super();
  }

  /**
   * Navigate to Submission tab
   */
  goToSubmissionTab(): void {
    this.clickElement(this.tabs.submission);
  }

  /**
   * Navigate to Review & Assessment tab
   */
  goToReviewAssessmentTab(): void {
    this.clickElement(this.tabs.reviewAssessment);
  }

  /**
   * Navigate to Project Info tab
   */
  goToProjectInfoTab(): void {
    this.clickElement(this.tabs.projectInfo);
  }

  /**
   * Navigate to Applicant Info tab
   */
  goToApplicantInfoTab(): void {
    this.clickElement(this.tabs.applicantInfo);
  }

  /**
   * Navigate to Funding Agreement tab
   */
  goToFundingAgreementTab(): void {
    this.clickElement(this.tabs.fundingAgreement);
  }

  /**
   * Navigate to Payment Info tab
   */
  goToPaymentInfoTab(): void {
    this.clickElement(this.tabs.paymentInfo);
  }

  /**
   * Verify all tabs are visible
   */
  verifyAllTabsVisible(): void {
    cy.get(this.tabs.submission).should("be.visible");
    cy.get(this.tabs.reviewAssessment).should("be.visible");
    cy.get(this.tabs.projectInfo).should("be.visible");
    cy.get(this.tabs.applicantInfo).should("be.visible");
    cy.get(this.tabs.fundingAgreement).should("be.visible");
    cy.get(this.tabs.paymentInfo).should("be.visible");
  }

  /**
   * Verify active tab
   */
  verifyActiveTab(
    tabName:
      | "submission"
      | "reviewAssessment"
      | "projectInfo"
      | "applicantInfo"
      | "fundingAgreement"
      | "paymentInfo"
  ): void {
    const tabSelectors: Record<string, string> = {
      submission: this.tabs.submission,
      reviewAssessment: this.tabs.reviewAssessment,
      projectInfo: this.tabs.projectInfo,
      applicantInfo: this.tabs.applicantInfo,
      fundingAgreement: this.tabs.fundingAgreement,
      paymentInfo: this.tabs.paymentInfo,
    };
    cy.get(tabSelectors[tabName]).should("have.class", "active");
  }

  /**
   * Verify field value in summary panel (using label/display-input pattern)
   */
  verifySummaryField(fieldName: string, expectedValue: string): void {
    this.verifyLabelValue(fieldName, expectedValue);
  }

  /**
   * Verify multiple summary fields
   */
  verifySummaryFields(fields: { [key: string]: string }): void {
    Object.entries(fields).forEach(([fieldName, expectedValue]) => {
      this.verifySummaryField(fieldName, expectedValue);
    });
  }

  /**
   * Verify Project Info field value
   */
  verifyProjectInfoField(selector: string, expectedValue: string): void {
    this.verifyInputValue(selector, expectedValue);
  }

  /**
   * Verify Applicant Info field value
   */
  verifyApplicantInfoField(selector: string, expectedValue: string): void {
    this.verifyInputValue(selector, expectedValue);
  }

  /**
   * Verify multiple applicant info fields at once
   */
  verifyApplicantInfoFields(fields: Array<[string, string]>): void {
    cy.get(this.applicantInfoFields.fieldset, { timeout: 10000 })
      .should("be.visible")
      .as("app");

    fields.forEach(([selector, expected]) => {
      cy.get("@app").find(selector).should("have.value", expected);
    });
  }

  /**
   * Verify textarea field value in Applicant Info
   */
  verifyApplicantInfoTextarea(selector: string, expectedValue: string): void {
    cy.get(this.applicantInfoFields.fieldset)
      .find(selector)
      .invoke("val")
      .should("equal", expectedValue);
  }

  /**
   * Select sector in Applicant Info
   */
  selectSector(sectorName: string): void {
    cy.contains(this.applicantInfoFields.sector, /^Sector$/, { timeout: 10000 })
      .siblings("select")
      .select(sectorName);
  }

  /**
   * Select sub-sector in Applicant Info
   */
  selectSubSector(subSectorName: string): void {
    cy.contains(this.applicantInfoFields.subSector, /^Sub-sector$/)
      .siblings("select")
      .select(subSectorName);
  }

  /**
   * Verify sub-sector options are available
   */
  verifySubSectorOptions(options: string[]): void {
    cy.contains(this.applicantInfoFields.subSector, /^Sub-sector$/)
      .siblings("select")
      .as("subSector");

    options.forEach((text) => {
      cy.get("@subSector").select(text).should("have.value", text);
    });
  }

  /**
   * Verify submission section headers exist
   */
  verifySubmissionHeaders(headers: string[]): void {
    headers.forEach((header) => {
      cy.contains("h4", header).should("exist").click();
    });
  }

  /**
   * Verify Payment Info requested amount
   */
  verifyPaymentInfoRequestedAmount(amount: string): void {
    this.verifyInputValue("#RequestedAmount", amount);
  }

  /**
   * Verify Review & Assessment requested amount
   */
  verifyReviewAssessmentRequestedAmount(amount: string): void {
    this.verifyInputValue("#RequestedAmountInputAR", amount);
  }

  /**
   * Verify Review & Assessment total budget
   */
  verifyReviewAssessmentTotalBudget(budget: string): void {
    this.verifyInputValue("#TotalBudgetInputAR", budget);
  }

  // ============ Status Actions Dropdown Methods ============

  /**
   * Open the Status Actions dropdown
   */
  openStatusActionsDropdown(): void {
    this.clickElement(this.statusActions.dropdownToggle);
    cy.get(this.statusActions.dropdownMenu).should("be.visible");
  }

  /**
   * Click Start Review action
   */
  clickStartReview(): void {
    this.openStatusActionsDropdown();
    this.clickElement(this.statusActions.startReview);
  }

  /**
   * Click Complete Review action
   */
  clickCompleteReview(): void {
    this.openStatusActionsDropdown();
    this.clickElement(this.statusActions.completeReview);
  }

  /**
   * Click Start Assessment action
   */
  clickStartAssessment(): void {
    this.openStatusActionsDropdown();
    this.clickElement(this.statusActions.startAssessment);
  }

  /**
   * Click Complete Assessment action
   */
  clickCompleteAssessment(): void {
    this.openStatusActionsDropdown();
    this.clickElement(this.statusActions.completeAssessment);
  }

  /**
   * Click Approve action
   */
  clickApprove(): void {
    this.openStatusActionsDropdown();
    this.clickElement(this.statusActions.approve);
  }

  /**
   * Click Decline action
   */
  clickDecline(): void {
    this.openStatusActionsDropdown();
    this.clickElement(this.statusActions.deny);
  }

  /**
   * Click Close action
   */
  clickClose(): void {
    this.openStatusActionsDropdown();
    this.clickElement(this.statusActions.close);
  }

  /**
   * Click Withdraw action
   */
  clickWithdraw(): void {
    this.openStatusActionsDropdown();
    this.clickElement(this.statusActions.withdraw);
  }

  /**
   * Click Defer action
   */
  clickDefer(): void {
    this.openStatusActionsDropdown();
    this.clickElement(this.statusActions.defer);
  }

  /**
   * Click On Hold action
   */
  clickOnHold(): void {
    this.openStatusActionsDropdown();
    this.clickElement(this.statusActions.onHold);
  }

  /**
   * Verify status action is enabled
   */
  verifyStatusActionEnabled(
    action:
      | "startReview"
      | "completeReview"
      | "startAssessment"
      | "completeAssessment"
      | "approve"
      | "decline"
      | "close"
      | "withdraw"
      | "defer"
      | "onHold"
  ): void {
    const actionSelectors: Record<string, string> = {
      startReview: this.statusActions.startReview,
      completeReview: this.statusActions.completeReview,
      startAssessment: this.statusActions.startAssessment,
      completeAssessment: this.statusActions.completeAssessment,
      approve: this.statusActions.approve,
      decline: this.statusActions.deny,
      close: this.statusActions.close,
      withdraw: this.statusActions.withdraw,
      defer: this.statusActions.defer,
      onHold: this.statusActions.onHold,
    };
    this.openStatusActionsDropdown();
    cy.get(actionSelectors[action]).should("not.be.disabled");
  }

  /**
   * Verify status action is disabled
   */
  verifyStatusActionDisabled(
    action:
      | "startReview"
      | "completeReview"
      | "startAssessment"
      | "completeAssessment"
      | "approve"
      | "decline"
      | "close"
      | "withdraw"
      | "defer"
      | "onHold"
  ): void {
    const actionSelectors: Record<string, string> = {
      startReview: this.statusActions.startReview,
      completeReview: this.statusActions.completeReview,
      startAssessment: this.statusActions.startAssessment,
      completeAssessment: this.statusActions.completeAssessment,
      approve: this.statusActions.approve,
      decline: this.statusActions.deny,
      close: this.statusActions.close,
      withdraw: this.statusActions.withdraw,
      defer: this.statusActions.defer,
      onHold: this.statusActions.onHold,
    };
    this.openStatusActionsDropdown();
    cy.get(actionSelectors[action]).should("be.disabled");
  }

  /**
   * Verify Status Actions dropdown is visible
   */
  verifyStatusActionsDropdownVisible(): void {
    cy.get(this.statusActions.dropdown).should("be.visible");
    cy.get(this.statusActions.dropdownToggle).should("be.visible");
  }

  /**
   * Get available (enabled) status actions
   */
  getAvailableStatusActions(): Cypress.Chainable<string[]> {
    const actions: string[] = [];
    this.openStatusActionsDropdown();
    return cy
      .get(this.statusActions.dropdownMenu)
      .find("button:not([disabled])")
      .each(($btn) => {
        actions.push($btn.text().trim());
      })
      .then(() => actions);
  }

  /**
   * Verify specific status actions are available
   */
  verifyStatusActionsAvailable(expectedActions: string[]): void {
    this.getAvailableStatusActions().then((availableActions) => {
      expectedActions.forEach((action) => {
        expect(availableActions).to.include(action);
      });
    });
  }
}
