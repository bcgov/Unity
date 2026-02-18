/// <reference types="cypress" />

import { ApplicationsPage } from "./ListPages";

/**
 * ApplicationsListPage - Extended Page Object for the Grant Applications List page
 * Extends ApplicationsPage with additional functionality for:
 * - Date filters
 * - Columns menu operations
 * - Payment modal handling
 * - Table horizontal scrolling and column visibility
 */
export class ApplicationsListPage extends ApplicationsPage {
  private readonly STANDARD_TIMEOUT = 20000;
  private readonly BUTTON_TIMEOUT = 60000;

  // Date filter selectors
  private readonly dateFilters = {
    submittedFromDate: "input#submittedFromDate",
    submittedToDate: "input#submittedToDate",
    spinner: 'div.spinner-grow[role="status"]',
  };

  // Extended action bar selectors (beyond ApplicationsPage)
  private readonly extendedActionBar = {
    customButtons: "#app_custom_buttons",
    dynamicButtonContainer: "#dynamicButtonContainerId",
    exportButton: "#dynamicButtonContainerId .dt-buttons button span",
    saveViewButton: "button.grp-savedStates",
  };

  // Table scrolling selectors
  private readonly scrollTable = {
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

  // ============ Extended Table Methods ============

  /**
   * Verify table has rows (using scroll body selector)
   */
  verifyTableHasData(): this {
    cy.get(this.scrollTable.tableRows, { timeout: this.STANDARD_TIMEOUT }).should(
      "have.length.greaterThan",
      1
    );
    return this;
  }

  /**
   * Select a row by index (clicks on a non-link cell)
   */
  selectRowByIndex(rowIndex: number, withCtrl = false): this {
    cy.get(this.scrollTable.tableRows, { timeout: this.STANDARD_TIMEOUT })
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
    cy.get(this.scrollTable.scrollBody, { timeout: this.STANDARD_TIMEOUT })
      .should("exist")
      .scrollTo(x, 0, { duration: 0, ensureScrollable: false });
    return this;
  }

  /**
   * Get visible header titles from the table
   */
  getVisibleHeaderTitles(): Cypress.Chainable<string[]> {
    return cy
      .get(this.scrollTable.columnTitles, { timeout: this.STANDARD_TIMEOUT })
      .then(($els: JQuery<HTMLElement>) => {
        const titles: string[] = Cypress.$($els)
          .toArray()
          .map((el: HTMLElement) => (el.textContent || "").replace(/\s+/g, " ").trim())
          .filter((t: string) => t.length > 0);
        return titles;
      });
  }

  /**
   * Assert that visible headers include expected columns (case-insensitive)
   */
  assertVisibleHeadersInclude(expected: string[]): this {
    this.getVisibleHeaderTitles().then((titles: string[]) => {
      const titlesLower = titles.map((t: string) => t.toLowerCase());
      expected.forEach((e: string) => {
        expect(
          titlesLower,
          `visible headers should include "${e}"`
        ).to.include(e.toLowerCase());
      });
    });
    return this;
  }

  // ============ Extended Action Bar Methods ============

  /**
   * Scroll to and verify action bar exists
   */
  verifyActionBarExists(): this {
    cy.get(this.extendedActionBar.customButtons, { timeout: this.STANDARD_TIMEOUT })
      .should("exist")
      .scrollIntoView();
    return this;
  }

  /**
   * Click the Payment button (extended with visibility checks)
   */
  clickPaymentButtonWithWait(): this {
    cy.get("#applicationPaymentRequest", { timeout: this.BUTTON_TIMEOUT })
      .should("be.visible")
      .and("not.be.disabled")
      .click({ force: true });
    return this;
  }

  /**
   * Verify Export button is visible
   */
  verifyExportButtonVisible(): this {
    cy.contains(this.extendedActionBar.exportButton, "Export", {
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
    cy.get(this.extendedActionBar.dynamicButtonContainer, {
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

    // Click backdrop if present (check existence first to avoid timeout)
    cy.get("body").then(($body: JQuery<HTMLBodyElement>) => {
      if ($body.find(this.paymentModal.backdrop).length > 0) {
        cy.get(this.paymentModal.backdrop).click("topLeft", { force: true });
      }
    });

    // Try Cancel button if available (check existence first to avoid timeout)
    cy.get("body").then(($body: JQuery<HTMLBodyElement>) => {
      const $cancelBtn = $body.find(this.paymentModal.cancelButton).filter(
        (_: number, el: HTMLElement) => (el.textContent || "").includes("Cancel")
      );
      if ($cancelBtn.length > 0) {
        cy.wrap($cancelBtn.first()).scrollIntoView().click({ force: true });
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
    cy.get(this.scrollTable.columnTitles, { timeout: this.STANDARD_TIMEOUT }).should(
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
   * Click a column item in the Columns menu (case-insensitive)
   */
  clickColumnsItem(label: string): this {
    cy.contains(this.columnsMenu.dropdownItem, label, {
      timeout: this.STANDARD_TIMEOUT,
      matchCase: false,
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
   * Note: Consider using NavigationPage.switchToTenantIfAvailable() for consistency
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
