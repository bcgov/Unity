import { BasePage } from "./BasePage";

/**
 * ListPage - Generic Page Object for list-based pages (Applications, Roles, Users, etc.)
 */
export class ListPage extends BasePage {
  protected pageName: string;

  constructor(pageName: string) {
    super();
    this.pageName = pageName;
  }

  /**
   * Verify list has at least one row
   */
  verifyListHasData(): void {
    this.verifyTableHasMinRows(1);
  }

  /**
   * Verify list is populated with minimum rows
   */
  verifyListPopulated(minRows: number = 1): void {
    this.verifyTableHasMinRows(minRows);
  }

  /**
   * Get row count
   */
  getRowCount(): Cypress.Chainable<number> {
    return this.getTableRowCount();
  }

  /**
   * Search in list
   */
  searchFor(searchText: string): void {
    this.typeText("#search", searchText);
  }

  /**
   * Clear search
   */
  clearSearch(): void {
    this.getElement("#search").should("exist").clear();
  }

  /**
   * Click on a row by text content
   */
  clickRowByText(text: string): void {
    cy.contains("tr", text).click();
  }

  /**
   * Select checkbox for row containing text
   */
  selectRowCheckbox(text: string): void {
    cy.contains("tr", text).find(".checkbox-select").click();
  }
  /**
   * Verify row exists with text
   */
  verifyRowExists(text: string): void {
    cy.contains("tr", text).should("exist");
  }
}

/**
 * ApplicationsPage - Specific page object for Applications list (GrantApplicationsTable)
 */
export class ApplicationsPage extends ListPage {
  private readonly selectors = {
    applicationLink: "#applicationLink",
    externalLink: "#externalLink",
    closeSummaryCanvas: "#closeSummaryCanvas",
    // GrantApplicationsTable selectors
    table: "#GrantApplicationsTable",
    selectAllCheckbox: ".select-all-applications",
    rowCheckbox: ".checkbox-select.chkbox",
    tableBody: "#GrantApplicationsTable tbody",
    tableRow: "#GrantApplicationsTable tbody tr",
    submissionLink: "#GrantApplicationsTable tbody tr td a",
    // Action buttons
    btnOpen: "#externalLink",
    btnAssign: "#assignApplication",
    btnApprove: "#approveApplications",
    btnTags: "#tagApplication",
    btnPayment: "#applicationPaymentRequest",
    btnInfo: "#applicationLink",
    btnFilter: "#btn-toggle-filter",
  };

  // Column indices for GrantApplicationsTable
  private readonly columns = {
    checkbox: 0,
    applicantName: 1,
    submissionNumber: 2,
    category: 3,
    submissionDate: 4,
    projectName: 5,
    assignee: 9,
    status: 10,
    requestedAmount: 11,
    approvedAmount: 12,
    community: 15,
    subStatus: 31,
    tags: 32,
    applicantId: 63,
  };

  constructor() {
    super("Applications");
  }

  /**
   * Open application summary panel
   */
  openApplicationSummary(): void {
    this.clickElement(this.selectors.applicationLink);
  }

  /**
   * Close summary panel
   */
  closeSummary(): void {
    this.clickElement(this.selectors.closeSummaryCanvas);
  }

  /**
   * Open application details
   */
  openApplicationDetails(): void {
    this.clickElement(this.selectors.externalLink);
  }

  /**
   * Search and select application by confirmation ID
   */
  searchAndSelectByConfirmationId(confirmationId: string): void {
    this.clearSearch();
    this.searchFor(confirmationId);
    this.selectRowCheckbox(confirmationId);
  }

  // ============ Action Button Methods ============

  /**
   * Click Open button to open selected application(s)
   */
  clickOpenButton(): void {
    this.clickElement(this.selectors.btnOpen);
  }

  /**
   * Click Assign button to assign selected application(s)
   */
  clickAssignButton(): void {
    this.clickElement(this.selectors.btnAssign);
  }

  /**
   * Click Approve button to approve selected application(s)
   */
  clickApproveButton(): void {
    this.clickElement(this.selectors.btnApprove);
  }

  /**
   * Click Tags button to manage tags for selected application(s)
   */
  clickTagsButton(): void {
    this.clickElement(this.selectors.btnTags);
  }

  /**
   * Click Payment button to process payment for selected application(s)
   */
  clickPaymentButton(): void {
    this.clickElement(this.selectors.btnPayment);
  }

  /**
   * Click Info button to view application summary/info panel
   */
  clickInfoButton(): void {
    this.clickElement(this.selectors.btnInfo);
  }

  /**
   * Click Filter button to toggle filter panel
   */
  clickFilterButton(): void {
    this.clickElement(this.selectors.btnFilter);
  }

  /**
   * Verify action button is enabled
   */
  verifyActionButtonEnabled(
    buttonName:
      | "open"
      | "assign"
      | "approve"
      | "tags"
      | "payment"
      | "info"
      | "filter"
  ): void {
    const buttonSelectors: Record<string, string> = {
      open: this.selectors.btnOpen,
      assign: this.selectors.btnAssign,
      approve: this.selectors.btnApprove,
      tags: this.selectors.btnTags,
      payment: this.selectors.btnPayment,
      info: this.selectors.btnInfo,
      filter: this.selectors.btnFilter,
    };
    cy.get(buttonSelectors[buttonName]).should("not.be.disabled");
  }

  /**
   * Verify action button is disabled
   */
  verifyActionButtonDisabled(
    buttonName:
      | "open"
      | "assign"
      | "approve"
      | "tags"
      | "payment"
      | "info"
      | "filter"
  ): void {
    const buttonSelectors: Record<string, string> = {
      open: this.selectors.btnOpen,
      assign: this.selectors.btnAssign,
      approve: this.selectors.btnApprove,
      tags: this.selectors.btnTags,
      payment: this.selectors.btnPayment,
      info: this.selectors.btnInfo,
      filter: this.selectors.btnFilter,
    };
    cy.get(buttonSelectors[buttonName]).should("be.disabled");
  }

  /**
   * Verify all action buttons are visible
   */
  verifyActionButtonsVisible(): void {
    cy.get(this.selectors.btnOpen).should("be.visible");
    cy.get(this.selectors.btnAssign).should("be.visible");
    cy.get(this.selectors.btnApprove).should("be.visible");
    cy.get(this.selectors.btnTags).should("be.visible");
    cy.get(this.selectors.btnPayment).should("be.visible");
    cy.get(this.selectors.btnInfo).should("be.visible");
    cy.get(this.selectors.btnFilter).should("be.visible");
  }

  // ============ Enhanced GrantApplicationsTable Methods ============

  /**
   * Select all applications using the header checkbox
   */
  selectAllApplications(): void {
    this.clickElement(this.selectors.selectAllCheckbox);
  }

  /**
   * Select application by submission number
   */
  selectApplicationBySubmissionNumber(submissionNumber: string): void {
    cy.contains(this.selectors.tableRow, submissionNumber)
      .find(this.selectors.rowCheckbox)
      .check();
  }

  /**
   * Click application details link by submission number
   */
  openApplicationBySubmissionNumber(submissionNumber: string): void {
    cy.contains(this.selectors.submissionLink, submissionNumber).click();
  }

  /**
   * Get application data by submission number
   */
  getApplicationBySubmissionNumber(
    submissionNumber: string
  ): Cypress.Chainable<any> {
    return cy
      .contains(this.selectors.tableRow, submissionNumber)
      .then(($row) => {
        return {
          applicantName: $row
            .find(`td:nth-child(${this.columns.applicantName + 1})`)
            .text()
            .trim(),
          submissionNumber: $row
            .find(`td:nth-child(${this.columns.submissionNumber + 1})`)
            .text()
            .trim(),
          category: $row
            .find(`td:nth-child(${this.columns.category + 1})`)
            .text()
            .trim(),
          submissionDate: $row
            .find(`td:nth-child(${this.columns.submissionDate + 1})`)
            .text()
            .trim(),
          projectName: $row
            .find(`td:nth-child(${this.columns.projectName + 1})`)
            .text()
            .trim(),
          assignee: $row
            .find(`td:nth-child(${this.columns.assignee + 1})`)
            .text()
            .trim(),
          status: $row
            .find(`td:nth-child(${this.columns.status + 1})`)
            .text()
            .trim(),
          requestedAmount: $row
            .find(`td:nth-child(${this.columns.requestedAmount + 1})`)
            .text()
            .trim(),
          approvedAmount: $row
            .find(`td:nth-child(${this.columns.approvedAmount + 1})`)
            .text()
            .trim(),
          community: $row
            .find(`td:nth-child(${this.columns.community + 1})`)
            .text()
            .trim(),
          subStatus: $row
            .find(`td:nth-child(${this.columns.subStatus + 1})`)
            .text()
            .trim(),
          tags: $row
            .find(`td:nth-child(${this.columns.tags + 1})`)
            .text()
            .trim(),
          applicantId: $row
            .find(`td:nth-child(${this.columns.applicantId + 1})`)
            .text()
            .trim(),
        };
      });
  }

  /**
   * Sort table by column name
   */
  sortByColumn(columnName: string): void {
    const columnSelectors: Record<string, string> = {
      applicantName: '[data-dt-column="1"] .dt-column-order',
      submissionNumber: '[data-dt-column="2"] .dt-column-order',
      category: '[data-dt-column="3"] .dt-column-order',
      submissionDate: '[data-dt-column="4"] .dt-column-order',
      projectName: '[data-dt-column="5"] .dt-column-order',
      assignee: '[data-dt-column="9"] .dt-column-order',
      status: '[data-dt-column="10"] .dt-column-order',
      requestedAmount: '[data-dt-column="11"] .dt-column-order',
      approvedAmount: '[data-dt-column="12"] .dt-column-order',
      community: '[data-dt-column="15"] .dt-column-order',
      subStatus: '[data-dt-column="31"] .dt-column-order',
      tags: '[data-dt-column="32"] .dt-column-order',
      applicantId: '[data-dt-column="63"] .dt-column-order',
    };

    const selector = columnSelectors[columnName];
    if (selector) {
      this.clickElement(selector);
    }
  }

  /**
   * Filter applications by status
   */
  verifyApplicationStatus(
    submissionNumber: string,
    expectedStatus: string
  ): void {
    cy.contains(this.selectors.tableRow, submissionNumber)
      .find(`td:nth-child(${this.columns.status + 1})`)
      .should("contain.text", expectedStatus);
  }

  /**
   * Get all applications with specific status
   */
  getApplicationsByStatus(status: string): Cypress.Chainable<any[]> {
    return cy.get(this.selectors.tableRow).then(($rows) => {
      return $rows
        .map((_index, row) => {
          const $row = Cypress.$(row);
          const rowStatus = $row
            .find(`td:nth-child(${this.columns.status + 1})`)
            .text()
            .trim();
          if (rowStatus === status) {
            return {
              applicantName: $row
                .find(`td:nth-child(${this.columns.applicantName + 1})`)
                .text()
                .trim(),
              submissionNumber: $row
                .find(`td:nth-child(${this.columns.submissionNumber + 1})`)
                .text()
                .trim(),
              status: rowStatus,
            };
          }
          return null;
        })
        .get()
        .filter((app) => app !== null);
    });
  }

  /**
   * Verify requested amount for application
   */
  verifyRequestedAmount(
    submissionNumber: string,
    expectedAmount: string
  ): void {
    cy.contains(this.selectors.tableRow, submissionNumber)
      .find(`td:nth-child(${this.columns.requestedAmount + 1})`)
      .should("contain.text", expectedAmount);
  }

  /**
   * Verify approved amount for application
   */
  verifyApprovedAmount(submissionNumber: string, expectedAmount: string): void {
    cy.contains(this.selectors.tableRow, submissionNumber)
      .find(`td:nth-child(${this.columns.approvedAmount + 1})`)
      .should("contain.text", expectedAmount);
  }

  /**
   * Get count of applications by status
   */
  getCountByStatus(status: string): Cypress.Chainable<number> {
    return cy
      .get(this.selectors.tableRow)
      .filter((index, row) => {
        return (
          Cypress.$(row)
            .find(`td:nth-child(${this.columns.status + 1})`)
            .text()
            .trim() === status
        );
      })
      .its("length");
  }

  /**
   * Select multiple applications by submission numbers
   */
  selectMultipleApplications(submissionNumbers: string[]): void {
    submissionNumbers.forEach((submissionNumber) => {
      this.selectApplicationBySubmissionNumber(submissionNumber);
    });
  }

  /**
   * Verify assignee for application
   */
  verifyAssignee(submissionNumber: string, expectedAssignee: string): void {
    cy.contains(this.selectors.tableRow, submissionNumber)
      .find(`td:nth-child(${this.columns.assignee + 1})`)
      .should("contain.text", expectedAssignee);
  }

  /**
   * Verify tags for application
   */
  verifyTags(submissionNumber: string, expectedTags: string): void {
    cy.contains(this.selectors.tableRow, submissionNumber)
      .find(`td:nth-child(${this.columns.tags + 1})`)
      .should("contain.text", expectedTags);
  }

  /**
   * Verify sub-status for application
   */
  verifySubStatus(submissionNumber: string, expectedSubStatus: string): void {
    cy.contains(this.selectors.tableRow, submissionNumber)
      .find(`td:nth-child(${this.columns.subStatus + 1})`)
      .should("contain.text", expectedSubStatus);
  }

  /**
   * Get first N applications from the table
   */
  getFirstNApplications(count: number): Cypress.Chainable<any[]> {
    return cy.get(this.selectors.tableRow).then(($rows) => {
      return $rows
        .slice(0, count)
        .map((_index, row) => {
          const $row = Cypress.$(row);
          return {
            applicantName: $row
              .find(`td:nth-child(${this.columns.applicantName + 1})`)
              .text()
              .trim(),
            submissionNumber: $row
              .find(`td:nth-child(${this.columns.submissionNumber + 1})`)
              .text()
              .trim(),
            status: $row
              .find(`td:nth-child(${this.columns.status + 1})`)
              .text()
              .trim(),
          };
        })
        .get();
    });
  }

  /**
   * Verify table has applications with specific category
   */
  verifyApplicationsExistWithCategory(category: string): void {
    cy.get(this.selectors.tableRow)
      .find(`td:nth-child(${this.columns.category + 1})`)
      .contains(category)
      .should("exist");
  }

  /**
   * Get total requested amount from visible applications
   */
  getTotalRequestedAmount(): Cypress.Chainable<number> {
    let total = 0;
    return cy
      .get(this.selectors.tableRow)
      .each(($row) => {
        const amountText = $row
          .find(`td:nth-child(${this.columns.requestedAmount + 1})`)
          .text()
          .trim();
        const amount = parseFloat(amountText.replace(/[$,]/g, ""));
        if (!isNaN(amount)) {
          total += amount;
        }
      })
      .then(() => total);
  }

  /**
   * Verify application exists by applicant name
   */
  verifyApplicationExistsByApplicantName(applicantName: string): void {
    cy.get(this.selectors.tableRow)
      .find(`td:nth-child(${this.columns.applicantName + 1})`)
      .contains(applicantName)
      .should("exist");
  }

  /**
   * Get application link URL by submission number
   */
  getApplicationLinkUrl(submissionNumber: string): Cypress.Chainable<string> {
    return cy
      .contains(this.selectors.submissionLink, submissionNumber)
      .invoke("attr", "href")
      .then((href) => href || "");
  }
}

/**
 * RolesPage - Specific page object for Roles list
 */
export class RolesPage extends ListPage {
  constructor() {
    super("Roles");
  }
}

/**
 * UsersPage - Specific page object for Users list
 */
export class UsersPage extends ListPage {
  constructor() {
    super("Users");
  }
}

/**
 * IntakesPage - Specific page object for Intakes list
 */
export class IntakesPage extends ListPage {
  constructor() {
    super("Intakes");
  }
}

/**
 * FormsPage - Specific page object for Forms list
 */
export class FormsPage extends ListPage {
  constructor() {
    super("Forms");
  }
}

/**
 * PaymentsPage - Specific page object for Payments list
 */
export class PaymentsPage extends ListPage {
  constructor() {
    super("Payments");
  }
}
