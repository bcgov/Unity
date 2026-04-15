/// <reference types="cypress" />

import { BasePage } from "./BasePage";

/**
 * ReviewAssessmentPage - Page Object for the Review & Assessment tab
 * Handles form fields inside shadow DOM (Form.io)
 */
export class ReviewAssessmentPage extends BasePage {
  private readonly STANDARD_TIMEOUT = 20000;

  // Tab container selectors
  private readonly containers = {
    detailsTabContent: "#detailsTabContent",
    submissionTab: "#nav-summery",
    reviewAssessmentTab: "#nav-review-and-assessment",
    formioContainer: "#formio",
  };

  // Section header selectors (card headers)
  private readonly sections = {
    introduction: 'h4.card-title:contains("1. INTRODUCTION")',
    eligibility: 'h4.card-title:contains("2. ELIGIBILITY")',
    applicantInfo: 'h4.card-title:contains("3. APPLICANT INFORMATION")',
    projectInfo: 'h4.card-title:contains("4. PROJECT INFORMATION")',
    projectTimelines: 'h4.card-title:contains("5. PROJECT TIMELINES")',
    projectBudget: 'h4.card-title:contains("6. PROJECT BUDGET")',
    attestation: 'h4.card-title:contains("7. ATTESTATION")',
  };

  // Organization Info panel selectors (using name attribute)
  private readonly organizationInfo = {
    applicantName: 'input[name="data[_ApplicantName]"]',
    registeredBusinessName: 'input[name="data[_dateExtractBusinessName]"]',
    registeredBusinessNumber: 'input[name="data[_registeredBusinessNumber]"]',
    businessName: 'input[name="data[_OrganizationName]"]',
    organizationType: 'select[name="data[_OrganizationType]"]',
    orgBookStatus: 'select[name="data[_OrgBookStatus]"]',
    riskRanking: 'select[name="data[_riskRanking]"]',
    sector: 'select[name="data[sector]"]',
    subsector: 'select[name="data[subsector]"]',
    otherSubsector: 'textarea[name="data[_OtherSubSector]"]',
  };

  // Contact Info panel selectors
  private readonly contactInfo = {
    contactName: 'input[name="data[_ContactName]"]',
    contactTitle: 'input[name="data[_ContactTitle]"]',
    contactEmail: 'input[name="data[_ContactEmail]"]',
    contactPhonePrimary: 'input[name="data[_ContactPhoneNumberPrimary]"]',
    contactPhoneSecondary: 'input[name="data[_ContactPhoneNumberSecondary]"]',
  };

  // Mailing Address panel selectors
  private readonly mailingAddress = {
    unit: 'input[name="data[_MailingAddressUnit]"]',
    street1: 'input[name="data[_MailingAddressStreet1]"]',
    street2: 'input[name="data[_MailingAddressStreet2]"]',
    city: 'input[name="data[_MailingAddressCity]"]',
    province: 'select[name="data[_MailingAddressProvince]"]',
    postalCode: 'input[name="data[_MailingAddressPostalCode]"]',
  };

  // Project Info selectors
  private readonly projectInfo = {
    projectName: 'input[name="data[_ProjectName]"]',
    projectDescription: 'textarea[name="data[_ProjectDescription]"]',
    economicRegion: 'select[name="data[_EconomicRegion]"]',
    regionalDistrict: 'select[name="data[_RegionalDistrict]"]',
    community: 'select[name="data[_Community]"]',
  };

  // Project Budget selectors
  private readonly projectBudget = {
    requestedAmount: 'input[name="data[_RequestedAmount]"]',
    totalProjectBudget: 'input[name="data[_TotalProjectBudget]"]',
  };

  // Assessment selectors
  private readonly assessment = {
    approvedAmount: "#ApprovalView_ApprovedAmount",
    decisionDate: "#ApprovalView_FinalDecisionDate",
    saveButton: 'button:contains("Save")',
    // Assessment List view buttons
    createAssessmentButton: "#CreateButton",
    completeAssessmentButton: "#CompleteButton",
    assessmentMainView: "#assessmentMainView",
  };

  constructor() {
    super();
  }

  // ============ Section Methods ============

  /**
   * Expand a section by clicking its header
   */
  expandSection(
    sectionName:
      | "introduction"
      | "eligibility"
      | "applicantInfo"
      | "projectInfo"
      | "projectTimelines"
      | "projectBudget"
      | "attestation",
  ): this {
    cy.contains("h4.card-title", this.getSectionTitle(sectionName), {
      timeout: this.STANDARD_TIMEOUT,
    })
      .should("be.visible")
      .click({ force: true });
    return this;
  }

  /**
   * Get the full section title text
   */
  private getSectionTitle(sectionName: string): string {
    const titles: Record<string, string> = {
      introduction: "1. INTRODUCTION",
      eligibility: "2. ELIGIBILITY",
      applicantInfo: "3. APPLICANT INFORMATION",
      projectInfo: "4. PROJECT INFORMATION",
      projectTimelines: "5. PROJECT TIMELINES",
      projectBudget: "6. PROJECT BUDGET",
      attestation: "7. ATTESTATION",
    };
    return titles[sectionName] || sectionName;
  }

  /**
   * Verify a section exists
   */
  verifySectionExists(
    sectionName:
      | "introduction"
      | "eligibility"
      | "applicantInfo"
      | "projectInfo"
      | "projectTimelines"
      | "projectBudget"
      | "attestation",
  ): this {
    cy.contains("h4.card-title", this.getSectionTitle(sectionName), {
      timeout: this.STANDARD_TIMEOUT,
    }).should("exist");
    return this;
  }

  // ============ Organization Info Methods ============

  /**
   * Get applicant name value
   */
  getApplicantName(): Cypress.Chainable<string> {
    return cy
      .get(this.organizationInfo.applicantName, {
        timeout: this.STANDARD_TIMEOUT,
      })
      .invoke("val")
      .then((val) => String(val));
  }

  /**
   * Verify applicant name
   */
  verifyApplicantName(expectedValue: string): this {
    cy.get(this.organizationInfo.applicantName, {
      timeout: this.STANDARD_TIMEOUT,
    }).should("have.value", expectedValue);
    return this;
  }

  /**
   * Get registered business name
   */
  getRegisteredBusinessName(): Cypress.Chainable<string> {
    return cy
      .get(this.organizationInfo.registeredBusinessName, {
        timeout: this.STANDARD_TIMEOUT,
      })
      .invoke("val")
      .then((val) => String(val));
  }

  /**
   * Verify registered business name
   */
  verifyRegisteredBusinessName(expectedValue: string): this {
    cy.get(this.organizationInfo.registeredBusinessName, {
      timeout: this.STANDARD_TIMEOUT,
    }).should("have.value", expectedValue);
    return this;
  }

  /**
   * Get registered business number
   */
  getRegisteredBusinessNumber(): Cypress.Chainable<string> {
    return cy
      .get(this.organizationInfo.registeredBusinessNumber, {
        timeout: this.STANDARD_TIMEOUT,
      })
      .invoke("val")
      .then((val) => String(val));
  }

  /**
   * Verify registered business number
   */
  verifyRegisteredBusinessNumber(expectedValue: string): this {
    cy.get(this.organizationInfo.registeredBusinessNumber, {
      timeout: this.STANDARD_TIMEOUT,
    }).should("have.value", expectedValue);
    return this;
  }

  // ============ Contact Info Methods ============

  /**
   * Verify contact name
   */
  verifyContactName(expectedValue: string): this {
    cy.get(this.contactInfo.contactName, {
      timeout: this.STANDARD_TIMEOUT,
    }).should("have.value", expectedValue);
    return this;
  }

  /**
   * Verify contact title
   */
  verifyContactTitle(expectedValue: string): this {
    cy.get(this.contactInfo.contactTitle, {
      timeout: this.STANDARD_TIMEOUT,
    }).should("have.value", expectedValue);
    return this;
  }

  /**
   * Verify contact email
   */
  verifyContactEmail(expectedValue: string): this {
    cy.get(this.contactInfo.contactEmail, {
      timeout: this.STANDARD_TIMEOUT,
    }).should("have.value", expectedValue);
    return this;
  }

  /**
   * Verify contact phone primary
   */
  verifyContactPhonePrimary(expectedValue: string): this {
    cy.get(this.contactInfo.contactPhonePrimary, {
      timeout: this.STANDARD_TIMEOUT,
    }).should("have.value", expectedValue);
    return this;
  }

  /**
   * Verify contact phone secondary
   */
  verifyContactPhoneSecondary(expectedValue: string): this {
    cy.get(this.contactInfo.contactPhoneSecondary, {
      timeout: this.STANDARD_TIMEOUT,
    }).should("have.value", expectedValue);
    return this;
  }

  // ============ Mailing Address Methods ============

  /**
   * Verify mailing address city
   */
  verifyMailingCity(expectedValue: string): this {
    cy.get(this.mailingAddress.city, { timeout: this.STANDARD_TIMEOUT }).should(
      "have.value",
      expectedValue,
    );
    return this;
  }

  /**
   * Verify mailing address street 1
   */
  verifyMailingStreet1(expectedValue: string): this {
    cy.get(this.mailingAddress.street1, {
      timeout: this.STANDARD_TIMEOUT,
    }).should("have.value", expectedValue);
    return this;
  }

  /**
   * Verify mailing address postal code
   */
  verifyMailingPostalCode(expectedValue: string): this {
    cy.get(this.mailingAddress.postalCode, {
      timeout: this.STANDARD_TIMEOUT,
    }).should("have.value", expectedValue);
    return this;
  }

  // ============ Panel Methods ============

  /**
   * Expand Organization Info panel
   */
  expandOrganizationInfoPanel(): this {
    cy.contains(".card-header", "Organization Info", {
      timeout: this.STANDARD_TIMEOUT,
    })
      .should("be.visible")
      .click({ force: true });
    return this;
  }

  /**
   * Expand Contact Info panel
   */
  expandContactInfoPanel(): this {
    cy.contains(".card-header", "Contact Info", {
      timeout: this.STANDARD_TIMEOUT,
    })
      .should("be.visible")
      .click({ force: true });
    return this;
  }

  /**
   * Expand Mailing Address panel
   */
  expandMailingAddressPanel(): this {
    cy.contains(".card-header", "Mailing Address", {
      timeout: this.STANDARD_TIMEOUT,
    })
      .should("be.visible")
      .click({ force: true });
    return this;
  }

  // ============ Verification Methods ============

  /**
   * Verify all main sections are present
   */
  verifyAllSectionsPresent(): this {
    this.verifySectionExists("introduction");
    this.verifySectionExists("eligibility");
    this.verifySectionExists("applicantInfo");
    this.verifySectionExists("projectInfo");
    this.verifySectionExists("projectTimelines");
    this.verifySectionExists("projectBudget");
    this.verifySectionExists("attestation");
    return this;
  }

  /**
   * Verify formio container is loaded
   */
  verifyFormioLoaded(): this {
    cy.get("body", { timeout: this.STANDARD_TIMEOUT }).should(($body) => {
      const formioVisible =
        $body.find(`${this.containers.formioContainer}:visible`).length > 0;
      const approvalVisible =
        $body.find(`${this.assessment.approvedAmount}:visible`).length > 0;
      const assessmentListVisible =
        $body.find(`${this.assessment.assessmentMainView}:visible`).length > 0;

      expect(
        formioVisible || approvalVisible || assessmentListVisible,
        "expected review and assessment content to be visible",
      ).to.eq(true);
    });
    return this;
  }

  /**
   * Get field value by name attribute
   */
  getFieldValue(fieldName: string): Cypress.Chainable<string> {
    return cy
      .get(`input[name="data[${fieldName}]"]`, {
        timeout: this.STANDARD_TIMEOUT,
      })
      .invoke("val")
      .then((val) => String(val));
  }

  /**
   * Verify field value by name attribute
   */
  verifyFieldValue(fieldName: string, expectedValue: string): this {
    cy.get(`input[name="data[${fieldName}]"]`, {
      timeout: this.STANDARD_TIMEOUT,
    }).should("have.value", expectedValue);
    return this;
  }

  /**
   * Verify select field has expected text (for Choices.js dropdowns)
   */
  verifySelectFieldText(fieldName: string, expectedText: string): this {
    cy.get(`select[name="data[${fieldName}]"]`, {
      timeout: this.STANDARD_TIMEOUT,
    })
      .parent()
      .find(".choices__item--selectable")
      .should("contain.text", expectedText);
    return this;
  }

  // ============ Assessment Methods ============

  /**
   * Enter approved amount
   */
  enterApprovedAmount(amount: string): this {
    cy.get(this.assessment.approvedAmount, { timeout: this.STANDARD_TIMEOUT })
      .scrollIntoView({ block: "center" })
      .should("exist")
      .and("not.be.disabled")
      .clear({ force: true })
      .type(amount, { force: true });
    return this;
  }

  /**
   * Set decision date to today (format: YYYY-MM-DD)
   */
  setDecisionDateToToday(): this {
    const now = new Date();
    const yyyy = now.getFullYear();
    const mm = String(now.getMonth() + 1).padStart(2, "0");
    const dd = String(now.getDate()).padStart(2, "0");
    const today = `${yyyy}-${mm}-${dd}`;
    cy.get(this.assessment.decisionDate, { timeout: this.STANDARD_TIMEOUT })
      .scrollIntoView({ block: "center" })
      .should("exist")
      .and("not.be.disabled")
      .clear({ force: true })
      .type(today, { force: true });
    return this;
  }

  /**
   * Set decision date to a specific date
   */
  setDecisionDate(date: string): this {
    cy.get(this.assessment.decisionDate, { timeout: this.STANDARD_TIMEOUT })
      .scrollIntoView({ block: "center" })
      .should("exist")
      .and("not.be.disabled")
      .clear({ force: true })
      .type(date, { force: true });
    return this;
  }

  /**
   * Click Save button
   */
  clickSave(): this {
    cy.contains("button", "Save", { timeout: this.STANDARD_TIMEOUT })
      .should("be.visible")
      .click({ force: true });
    return this;
  }

  /**
   * Scroll to Assessment List section
   */
  scrollToAssessmentList(): this {
    cy.get(this.assessment.assessmentMainView, {
      timeout: this.STANDARD_TIMEOUT,
    })
      .should("be.visible")
      .scrollIntoView();
    return this;
  }

  /**
   * Click Create Assessment button in Assessment List view
   */
  clickCreateAssessment(): this {
    cy.get(this.assessment.createAssessmentButton, {
      timeout: this.STANDARD_TIMEOUT,
    })
      .should("be.visible")
      .click({ force: true });
    return this;
  }

  /**
   * Click Complete Assessment button in Assessment List view
   */
  clickCompleteAssessment(): this {
    cy.get(this.assessment.completeAssessmentButton, {
      timeout: this.STANDARD_TIMEOUT,
    })
      .should("not.be.disabled")
      .click({ force: true });
    return this;
  }
}
