/// <reference types="cypress" />

import { BasePage } from "./BasePage";

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

  // Confirm action modal selectors (SweetAlert2)
  private readonly confirmModal = {
    modal: ".swal2-popup",
    confirmButton: "button.swal2-confirm",
    cancelButton: "button.swal2-cancel",
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
  goToSubmissionTab(): this {
    this.clickElement(this.tabs.submission);
    return this;
  }

  /**
   * Navigate to Review & Assessment tab
   */
  goToReviewAssessmentTab(): this {
    this.dismissErrorModalIfPresent();
    this.clickElement(this.tabs.reviewAssessment);
    return this;
  }

  /**
   * Navigate to Project Info tab
   */
  goToProjectInfoTab(): this {
    this.clickElement(this.tabs.projectInfo);
    return this;
  }

  /**
   * Navigate to Applicant Info tab
   */
  goToApplicantInfoTab(): this {
    this.clickElement(this.tabs.applicantInfo);
    return this;
  }

  /**
   * Navigate to Funding Agreement tab
   */
  goToFundingAgreementTab(): this {
    this.clickElement(this.tabs.fundingAgreement);
    return this;
  }

  /**
   * Navigate to Payment Info tab
   */
  goToPaymentInfoTab(): this {
    this.clickElement(this.tabs.paymentInfo);
    return this;
  }

  /**
   * Verify all tabs are visible
   */
  verifyAllTabsVisible(): this {
    cy.get(this.tabs.submission).should("be.visible");
    cy.get(this.tabs.reviewAssessment).should("be.visible");
    cy.get(this.tabs.projectInfo).should("be.visible");
    cy.get(this.tabs.applicantInfo).should("be.visible");
    cy.get(this.tabs.fundingAgreement).should("be.visible");
    cy.get(this.tabs.paymentInfo).should("be.visible");
    return this;
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
  ): this {
    const tabSelectors: Record<string, string> = {
      submission: this.tabs.submission,
      reviewAssessment: this.tabs.reviewAssessment,
      projectInfo: this.tabs.projectInfo,
      applicantInfo: this.tabs.applicantInfo,
      fundingAgreement: this.tabs.fundingAgreement,
      paymentInfo: this.tabs.paymentInfo,
    };
    cy.get(tabSelectors[tabName]).should("have.class", "active");
    return this;
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

  // ============ Payment Info Methods ============

  /**
   * Enter Supplier Number
   */
  enterSupplierNumber(supplierNumber: string): this {
    cy.get("#SupplierNumber", { timeout: 20000 })
      .clear({ force: true })
      .type(supplierNumber, { force: true })
      .trigger("change")
      .blur();
    return this;
  }

  /**
   * Click elsewhere to trigger save button enable
   */
  clickElsewhere(): this {
    cy.get("body").click(0, 0);
    return this;
  }

  /**
   * Click Payment Info Save button
   */
  clickPaymentInfoSave(): this {
    cy.get("#nav-payment-info", { timeout: 20000 })
      .contains("button", "Save")
      .click({ force: true });
    // Wait briefly for save to process
    cy.wait(1000);
    return this;
  }

  /**
   * Verify Site Info table is populated
   */
  verifySiteInfoTablePopulated(): this {
    cy.get("#SiteInfoTable tbody tr", { timeout: 20000 })
      .should("have.length.at.least", 1);
    return this;
  }

  /**
   * Verify Site Info table has data in specific columns
   */
  verifySiteInfoTableHasData(): this {
    cy.get("#SiteInfoTable tbody tr", { timeout: 20000 }).first().within(() => {
      cy.get("td").eq(0).should("not.be.empty"); // Site #
      cy.get("td").eq(1).should("not.be.empty"); // Pay Group
      cy.get("td").eq(2).should("not.be.empty"); // Mailing Address
    });
    return this;
  }

  /**
   * Click Edit button in Site Info table
   */
  clickSiteInfoEdit(): this {
    cy.get("#SiteInfoTable tbody tr", { timeout: 20000 })
      .first()
      .find("button, a")
      .filter(':contains("Edit"), [title="Edit"], .edit-btn, .btn-edit')
      .first()
      .click({ force: true });
    return this;
  }

  /**
   * Wait for Edit Site modal to appear
   */
  waitForEditSiteModal(): this {
    cy.get(".modal-content", { timeout: 20000 })
      .contains(".modal-title", "Edit Site")
      .should("be.visible");
    return this;
  }

  /**
   * Select Payment Group in Edit Site modal
   */
  selectPaymentGroup(paymentGroup: "EFT" | "Cheque"): this {
    cy.get("#Site_PaymentGroup", { timeout: 20000 })
      .select(paymentGroup, { force: true });
    return this;
  }

  /**
   * Click Save Changes in Edit Site modal
   */
  clickSaveChanges(): this {
    cy.get(".modal-footer", { timeout: 20000 })
      .contains("button", "SAVE CHANGES")
      .click({ force: true });
    cy.wait(2000); // Wait for save to process
    cy.get("body").type("{esc}");
    cy.get(".modal.show, .modal.fade.show", { timeout: 20000 }).should("not.exist");
    cy.get(".modal-backdrop", { timeout: 20000 }).should("not.exist");
    return this;
  }

  /**
   * Click Cancel in Edit Site modal
   */
  clickModalCancel(): this {
    cy.get(".modal-footer", { timeout: 20000 })
      .contains("button", "CANCEL")
      .click({ force: true });
    return this;
  }

  // ============ Status Actions Dropdown Methods ============

  /**
   * Open the Status Actions dropdown
   */
  openStatusActionsDropdown(): void {
    cy.get(this.statusActions.dropdownMenu).then(($menu) => {
      if (!$menu.is(":visible")) {
        cy.get(this.statusActions.dropdownToggle, { timeout: 20000 })
          .should("exist")
          .scrollIntoView()
          .click({ force: true });
      }
    });
    cy.get(this.statusActions.dropdownMenu, { timeout: 10000 }).should("be.visible");
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
   * Click Approve action.
   * If "Complete Assessment" is enabled in the dropdown, click it first,
   * then reopen the dropdown before clicking Approve.
   */
  clickApprove(): this {
    this.openStatusActionsDropdown();
    cy.get(this.statusActions.completeAssessment).then(($btn) => {
      if (!$btn.is(":disabled")) {
        cy.wrap($btn).click({ force: true });
        cy.get("body").then(($body) => {
          if ($body.find(this.confirmModal.modal).filter(":visible").length > 0) {
            cy.get(this.confirmModal.modal)
              .find(this.confirmModal.confirmButton)
              .click({ force: true });
          }
        });
        // Wait for page to stabilize after status transition
        cy.get(this.statusActions.dropdownToggle, { timeout: 20000 }).should("be.visible");
        cy.wait(2000);
      }
    });
    // Always reopen dropdown fresh before clicking Approve (dropdown may have closed)
    this.openStatusActionsDropdown();
    cy.get(this.statusActions.approve, { timeout: 10000 })
      .should("exist")
      .click({ force: true });
    return this;
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

  // ============ Confirm Modal Methods ============

  /**
   * Wait for confirm action modal to appear (SweetAlert2)
   */
  waitForConfirmModal(): this {
    cy.get(this.confirmModal.modal, { timeout: 20000 }).should("be.visible");
    return this;
  }

  /**
   * Click Confirm button in the modal (SweetAlert2)
   */
  clickConfirm(): this {
    cy.get(this.confirmModal.modal, { timeout: 20000 })
      .find(this.confirmModal.confirmButton)
      .should("be.visible")
      .click({ force: true });
    return this;
  }

  /**
   * Click Cancel button in the modal (SweetAlert2)
   */
  clickCancel(): this {
    cy.get(this.confirmModal.modal, { timeout: 20000 })
      .find(this.confirmModal.cancelButton)
      .should("be.visible")
      .click({ force: true });
    return this;
  }

  /**
   * Dismiss any error modal if present (SweetAlert2)
   * Uses failOnStatusCode: false to not fail if no modal exists
   */
  dismissErrorModalIfPresent(): this {
    cy.get("body").then(($body) => {
      // Only dismiss if it is specifically an error modal (swal2-error icon)
      if ($body.find(".swal2-container .swal2-icon.swal2-error").length > 0) {
        cy.get(".swal2-container")
          .find(".swal2-confirm")
          .first()
          .click({ force: true });
        cy.wait(500);
      }
    });
    return this;
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
