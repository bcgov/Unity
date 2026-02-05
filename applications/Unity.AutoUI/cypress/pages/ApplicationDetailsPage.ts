import { BasePage } from "./BasePage";

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
    contactFullName: "input#ContactInfo_Name",
    contactTitle: "input#ContactInfo_Title",
    contactEmail: "input#ContactInfo_Email",
    contactBusinessPhone: "input#ContactInfo_Phone",
    contactCellPhone: "input#ContactInfo_Phone2",
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
      | "paymentInfo",
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
    // Wait for page to settle after tab navigation
    cy.wait(3000);

    headers.forEach((header) => {
      cy.contains(header, { timeout: 20000, includeShadowDom: true })
        .should("exist")
        .scrollIntoView({ duration: 1000 })
        .wait(500)
        .should("be.visible");
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
      | "onHold",
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
      | "onHold",
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
