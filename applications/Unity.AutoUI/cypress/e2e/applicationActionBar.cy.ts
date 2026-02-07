/**
 * applicationActionBar.cy.ts
 * Tests for validating the Application List Action Bar components
 * Validates search bar, date filters, and all menu items
 */

import { loginIfNeeded } from "cypress/support/auth";
import {
  LoginPageInstance,
  ApplicationActionBarPageInstance,
} from "../utilities";

const loginPage = LoginPageInstance();
const actionBarPage = ApplicationActionBarPageInstance();

describe("Application List Action Bar Validation", () => {
  // Helper function to get PST date
  const getPSTDate = (monthsOffset = 0): string => {
    const now = new Date();
    const pstDate = new Date(
      now.toLocaleString("en-US", { timeZone: "America/Los_Angeles" }),
    );

    if (monthsOffset !== 0) {
      pstDate.setMonth(pstDate.getMonth() + monthsOffset);
    }

    const year = pstDate.getFullYear();
    const month = String(pstDate.getMonth() + 1).padStart(2, "0");
    const day = String(pstDate.getDate()).padStart(2, "0");

    return `${year}-${month}-${day}`;
  };

  before(() => {
    loginIfNeeded();

    // Navigate to Applications page
    cy.location("pathname", { timeout: 30000 }).should(
      "include",
      "/GrantApplications",
    );
    cy.get("#GrantApplicationsTable", { timeout: 10000 }).should("be.visible");
  });

  // ==================== TESTS WITHOUT APPLICATION SELECTION ====================
  describe("Tests Without Application Selection", () => {
    before(() => {
      actionBarPage.clearAllSelections();
      actionBarPage.waitForUIUpdate();
    });

    describe("Search Bar Validation", () => {
      it("Should verify search bar exists, attributes and is visible", () => {
        actionBarPage.verifySearchBarExists();
        actionBarPage.verifySearchBarAttributes();
      });

      it("Should verify search bar is functional", () => {
        const testSearch = "Test";
        actionBarPage.searchFor(testSearch);
        actionBarPage.verifySearchValue(testSearch);
        actionBarPage.clearSearch();
      });
    });

    describe("Date Filter Validation", () => {
      it("Should verify Submitted Date From filter exists", () => {
        actionBarPage.verifySubmittedFromDateExists();
      });

      it("Should verify Submitted Date From has correct attributes", () => {
        actionBarPage.verifySubmittedFromDateAttributes();
      });

      it("Should verify Submitted Date From is set to 6 months prior (PST)", () => {
        const expectedDate = getPSTDate(-6);
        actionBarPage.verifySubmittedFromDateValue(expectedDate);
      });

      it("Should verify Submitted Date From label is correct", () => {
        actionBarPage.verifySubmittedFromDateLabel();
      });

      it("Should verify Submitted Date To filter exists", () => {
        actionBarPage.verifySubmittedToDateExists();
      });

      it("Should verify Submitted Date To has correct attributes", () => {
        actionBarPage.verifySubmittedToDateAttributes();
      });

      it("Should verify Submitted Date To is set to today (PST)", () => {
        const expectedDate = getPSTDate(0);
        actionBarPage.verifySubmittedToDateValue(expectedDate);
      });

      it("Should verify Submitted Date To label is correct", () => {
        actionBarPage.verifySubmittedToDateLabel();
      });

      it("Should verify date filters are functional", () => {
        const fromDate = "2024-01-01";
        const toDate = "2024-12-31";

        actionBarPage.setDateRange(fromDate, toDate);
        actionBarPage.verifySubmittedFromDateValue(fromDate);
        actionBarPage.verifySubmittedToDateValue(toDate);
        actionBarPage.clearDateFilters();
      });
    });

    describe("Action Button State - No Selection", () => {
      it("Should verify action buttons state", () => {
        actionBarPage.verifyActionButtonsHidden();
        actionBarPage.verifyFilterButtonVisible();
        actionBarPage.verifyActionButtonsHidden();
      });
    });

    describe("Additional Menu Items Validation", () => {
      it("Should verify additional button exists", () => {
        actionBarPage.verifyExportButton();
        actionBarPage.verifySaveViewButton();
        actionBarPage.verifyColumnsButton();
      });
    });

    describe("Save View Dropdown Validation", () => {
      it("Should open Save View dropdown and verify all options", () => {
        actionBarPage.clickSaveViewButton();
        actionBarPage.verifySaveViewDropdownVisible();
        actionBarPage.verifySaveViewDropdownOptions();
        actionBarPage.closeSaveViewDropdown();
        actionBarPage.verifySaveViewDropdownHidden();
      });
    });

    describe("Filter Popover Validation", () => {
      it("Should open Filter popover and verify all elements", () => {
        actionBarPage.clickFilterButton();
        actionBarPage.verifyFilterPopoverVisible();
        actionBarPage.verifyFilterPopoverElements();
      });

      it("Should close Filter popover when clicking button again", () => {
        actionBarPage.closeFilterPopover();
        actionBarPage.verifyFilterPopoverHidden();
      });
    });

    // ==================== TESTS WITH APPLICATION SELECTION ====================
    describe("Tests With Application Selection", () => {
      beforeEach(() => {
        // Refresh page and ensure application is selected before each test
        actionBarPage.refreshPage();
        cy.wait(2000); // Wait for page to fully load
        actionBarPage.clearAllSelections();
        actionBarPage.waitForUIUpdate();
        actionBarPage.selectFirstApplication();
        actionBarPage.waitForUIUpdate();
      });

      describe("Action Button State - With Selection", () => {
        it("Should verify action buttons become visible after selecting application", () => {
          actionBarPage.verifyActionButtonsVisible();
          actionBarPage.verifyOpenButtonAttributes();
          actionBarPage.verifyAssignButtonAttributes();
          actionBarPage.verifyApproveButtonAttributes();
          actionBarPage.verifyTagsButtonAttributes();
          actionBarPage.verifyPaymentButtonAttributes();
          actionBarPage.verifyInfoButtonAttributes();
          actionBarPage.verifyButtonTypes();
        });
      });

      describe("Application Summary Side Pane Validation", () => {
        it("Should open application summary side pane via Info button", () => {
          actionBarPage.clickInfoButton();
          actionBarPage.verifySummarySidePaneVisible();
          actionBarPage.verifySummaryLabels();
          actionBarPage.closeSummarySidePane();
          actionBarPage.verifySummarySidePaneClosed();
        });
      });

      (Cypress.env("environment") === "PROD" ? describe.skip : describe)(
        "Payment Request Modal Validation",
        () => {
          it("Should open payment request modal via Payment button", () => {
            actionBarPage.clickPaymentButton();
            actionBarPage.verifyPaymentModalVisible();
            actionBarPage.verifyPaymentModalElements();
            actionBarPage.closePaymentModal();
            actionBarPage.verifyPaymentModalClosed();
          });
        },
      );

      describe("Tags Modal Validation", () => {
        it("Should open tags modal via Tags button", () => {
          actionBarPage.clickTagsButton();
          actionBarPage.verifyTagsModalVisible();
          actionBarPage.verifyTagsModalElements();
          actionBarPage.closeTagsModal();
          actionBarPage.verifyTagsModalClosed();
        });
      });

      describe("Approve Applications Modal Validation", () => {
        it("Should open approve applications modal via Approve button", () => {
          actionBarPage.clickApproveButton();
          actionBarPage.verifyApproveModalVisible();
          actionBarPage.verifyApproveModalElements();
          actionBarPage.closeApproveModal();
          actionBarPage.verifyApproveModalClosed();
        });
      });
    });
    describe("Column Visibility Management", () => {
      let originalColumns: string[] = [];

      it("Should open Columns dropdown and capture current state", () => {
        actionBarPage.clickColumnsButton();
        actionBarPage.verifyColumnsDropdownVisible();

        actionBarPage.getSelectedColumns().then((columns) => {
          originalColumns = columns;
          cy.log(`Original columns (${columns.length}): ${columns.join(", ")}`);
          expect(columns.length).to.be.greaterThan(0);
        });
      });

      it("Should select all columns and verify table update", () => {
        // actionBarPage.clickColumnsButton();
        actionBarPage.selectAllColumns();

        actionBarPage.closeColumnsDropdown();
        actionBarPage.verifyColumnsDropdownHidden();

        actionBarPage.clickColumnsButton();
        actionBarPage.getAllColumnNames().then((allColumns) => {
          actionBarPage.closeColumnsDropdown();

          actionBarPage.verifyTableColumns(allColumns);
          cy.log(`All columns selected (${allColumns.length})`);
        });
      });

      it("Should deselect all columns and verify table update", () => {
        actionBarPage.clickColumnsButton();
        actionBarPage.verifyColumnsDropdownVisible();

        actionBarPage.deselectAllColumns();

        actionBarPage.closeColumnsDropdown();
        actionBarPage.verifyColumnsDropdownHidden();

        actionBarPage.getVisibleColumnCount().then((count) => {
          cy.log(`Visible columns after deselecting all: ${count}`);
          expect(count).to.be.lessThan(5);
        });
      });
    });

    it("Should verify action bar container structure", () => {
      actionBarPage.verifyActionBarStructure();
    });
  });
});
