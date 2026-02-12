/// <reference types="cypress" />

import { loginIfNeeded } from "../support/auth";
import { ApplicationsListPage } from "../pages/ApplicationDetailsPage";

describe("Unity Login and check data from CHEFS", () => {
  const page = new ApplicationsListPage();

  // Column visibility test data - organized by scroll position
  const COLUMN_VISIBILITY_DATA = {
    scrollPosition0: [
      "Applicant Name",
      "Category",
      "Submission #",
      "Submission Date",
      "Status",
      "Sub-Status",
      "Community",
      "Requested Amount",
      "Approved Amount",
      "Project Name",
      "Applicant Id",
    ],
    scrollPosition1500: [
      "Tags",
      "Assignee",
      "SubSector",
      "Economic Region",
      "Regional District",
      "Registered Organization Number",
      "Org Book Status",
    ],
    scrollPosition3000: [
      "Project Start Date",
      "Project End Date",
      "Projected Funding Total",
      "Total Paid Amount $",
      "Project Electoral District",
      "Applicant Electoral District",
    ],
    scrollPosition4500: [
      "Forestry or Non-Forestry",
      "Forestry Focus",
      "Acquisition",
      "City",
      "Community Population",
      "Likelihood of Funding",
      "Total Score",
    ],
    scrollPosition6000: [
      "Assessment Result",
      "Recommended Amount",
      "Due Date",
      "Owner",
      "Decision Date",
      "Project Summary",
      "Organization Type",
      "Business Number",
    ],
    scrollPosition7500: [
      "Due Diligence Status",
      "Decline Rationale",
      "Contact Full Name",
      "Contact Title",
      "Contact Email",
      "Contact Business Phone",
      "Contact Cell Phone",
    ],
    scrollPosition9000: [
      "Signing Authority Full Name",
      "Signing Authority Title",
      "Signing Authority Email",
      "Signing Authority Business Phone",
      "Signing Authority Cell Phone",
      "Place",
      "Risk Ranking",
      "Notes",
      "Red-Stop",
      "Indigenous",
      "FYE Day",
      "FYE Month",
      "Payout",
      "Unity Application Id",
    ],
  };

  // Columns to toggle during the test - organized for maintainability
  const COLUMNS_TO_TOGGLE = {
    singleToggle: [
      "% of Total Project Budget",
      "Acquisition",
      "Applicant Electoral District",
      "Assessment Result",
      "Business Number",
      "City",
      "Community Population",
      "Contact Business Phone",
      "Contact Cell Phone",
      "Contact Email",
      "Contact Full Name",
      "Contact Title",
      "Decision Date",
      "Decline Rationale",
      "Due Date",
      "Due Diligence Status",
      "Economic Region",
      "Forestry Focus",
      "Forestry or Non-Forestry",
      "FYE Day",
      "FYE Month",
      "Indigenous",
      "Likelihood of Funding",
      "Non-Registered Organization Name",
      "Notes",
      "Org Book Status",
      "Organization Type",
      "Other Sector/Sub/Industry Description",
      "Owner",
      "Payout",
      "Place",
      "Project Electoral District",
      "Project End Date",
      "Project Start Date",
      "Project Summary",
      "Projected Funding Total",
      "Recommended Amount",
      "Red-Stop",
      "Regional District",
      "Registered Organization Name",
      "Registered Organization Number",
      "Risk Ranking",
      "Sector",
      "Signing Authority Business Phone",
      "Signing Authority Cell Phone",
      "Signing Authority Email",
      "Signing Authority Full Name",
      "Signing Authority Title",
      "SubSector",
      "Total Paid Amount $",
      "Total Project Budget",
      "Total Score",
      "Unity Application Id",
    ],
    doubleToggle: [
      "Applicant Id",
      "Applicant Name",
      "Approved Amount",
      "Assignee",
      "Category",
      "Community",
      "Project Name",
      "Requested Amount",
      "Status",
      "Sub-Status",
      "Submission #",
      "Submission Date",
      "Tags",
    ],
  };

  // TEST renders the Submission tab inside an open shadow root (Form.io).
  // Enabling this makes cy.get / cy.contains pierce shadow DOM consistently across envs.
  before(() => {
    Cypress.config("includeShadowDom", true);
    loginIfNeeded({ timeout: 20000 });
  });

  it("Switch to Default Grants Program if available", () => {
    page.switchToGrantProgram("Default Grants Program");
  });

  it("Tests the existence and functionality of the Submitted Date From and Submitted Date To filters", () => {
    // Set date filters and verify table refresh
    page
      .setSubmittedFromDate("2022-01-01")
      .waitForTableRefresh()
      .setSubmittedToDate(page.getTodayIsoLocal())
      .waitForTableRefresh();
  });

  //  With no rows selected verify the visibility of Filter, Export, Save View, and Columns.
  it("Verify the action buttons are visible with no rows selected", () => {
    // Placeholder for future implementation
  });

  //  With one row selected verify the visibility of Filter, Export, Save View, and Columns.
  it("Verify the action buttons are visible with one row selected", () => {
    // Placeholder for future implementation
  });

  it("Clicks Payment and force-closes the modal", () => {
    // Ensure table has data and select two rows
    page
      .verifyTableHasData()
      .selectMultipleRows([0, 1])
      .verifyActionBarExists()
      .clickPaymentButton()
      .waitForPaymentModalVisible()
      .closePaymentModal()
      .verifyPaymentModalClosed();

    // Verify right-side buttons are still usable
    page
      .verifyDynamicButtonContainerExists()
      .verifyExportButtonVisible()
      .verifySaveViewButtonVisible()
      .verifyColumnsButtonVisible();
  });

  // Walk the Columns menu and toggle each column on, verifying the column is visible.
  it("Verify all columns in the menu are visible when and toggled on.", () => {
    // Reset to default view and open columns menu
    page.closeOpenDropdowns().resetToDefaultView().openColumnsMenu();

    // Toggle all single-toggle columns
    page.toggleColumns(COLUMNS_TO_TOGGLE.singleToggle);

    // Toggle all double-toggle columns (toggle twice to ensure visibility)
    COLUMNS_TO_TOGGLE.doubleToggle.forEach((column) => {
      page.clickColumnsItem(column).clickColumnsItem(column);
    });

    // Close the columns menu
    page.closeColumnsMenu();

    // Verify columns by scrolling through the table
    page
      .scrollTableHorizontally(0)
      .assertVisibleHeadersInclude(COLUMN_VISIBILITY_DATA.scrollPosition0);

    page
      .scrollTableHorizontally(1500)
      .assertVisibleHeadersInclude(COLUMN_VISIBILITY_DATA.scrollPosition1500);

    page
      .scrollTableHorizontally(3000)
      .assertVisibleHeadersInclude(COLUMN_VISIBILITY_DATA.scrollPosition3000);

    page
      .scrollTableHorizontally(4500)
      .assertVisibleHeadersInclude(COLUMN_VISIBILITY_DATA.scrollPosition4500);

    page
      .scrollTableHorizontally(6000)
      .assertVisibleHeadersInclude(COLUMN_VISIBILITY_DATA.scrollPosition6000);

    page
      .scrollTableHorizontally(7500)
      .assertVisibleHeadersInclude(COLUMN_VISIBILITY_DATA.scrollPosition7500);

    page
      .scrollTableHorizontally(9000)
      .assertVisibleHeadersInclude(COLUMN_VISIBILITY_DATA.scrollPosition9000);
  });

  it("Verify Logout", () => {
    cy.logout();
  });
});
