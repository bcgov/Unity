import { BasePage } from "./BasePage";

/**
 * ApplicationActionBarPage - Page Object for Application List Action Bar
 * Handles search, filters, and action buttons for the Applications list view
 */
export class ApplicationActionBarPage extends BasePage {
  private readonly selectors = {
    // Search and Filter Elements
    searchInput: "#search", // Main search input field
    submittedFromDate: "#submittedFromDate", // Start date filter for submissions
    submittedToDate: "#submittedToDate", // End date filter for submissions
    submittedFromDateLabel: 'label[for="SubmittedFromDate"]', // Label for from date field
    submittedToDateLabel: 'label[for="SubmittedToDate"]', // Label for to date field

    // Action Buttons (selection-dependent)
    btnOpen: "#externalLink", // Opens selected application(s)
    btnAssign: "#assignApplication", // Assigns selected application(s)
    btnApprove: "#approveApplications", // Approves selected application(s)
    btnTags: "#tagApplication", // Manages tags for selected application(s)
    btnPayment: "#applicationPaymentRequest", // Creates payment for selected application(s)
    btnInfo: "#applicationLink", // Shows info/summary for selected application(s)

    // Action Buttons (always visible)
    btnFilter: "#btn-toggle-filter", // Toggles filter panel visibility

    // Table Management Buttons
    btnExport: '.dt-buttons button:contains("Export")', // Exports table data
    btnSaveView: ".buttons-collection.grp-savedStates", // Saves current view configuration
    btnColumns: '.dt-buttons .dropdown-toggle:contains("Columns")', // Toggles column visibility

    // Container Elements
    actionBarContainer: ".action-bar.search-action-bar", // Main action bar wrapper
    searchWrapper: ".search-action-bar_search-wrapper", // Search and filter wrapper
    customButtonsGroup: "#app_custom_buttons.btn-group", // Custom action buttons group
    dynamicButtonContainer: "#dynamicButtonContainerId", // Dynamic buttons container

    // Table Elements (for selection)
    tableBody: "#GrantApplicationsTable tbody", // Table body
    tableRow: "#GrantApplicationsTable tbody tr", // Table row
    selectAllCheckbox: ".select-all-applications", // Select all checkbox
    rowCheckbox: ".checkbox-select.chkbox", // Individual row checkbox

    // Column Visibility Elements
    columnsDropdown: "ul.dropdown-menu.show", // Column dropdown menu
    columnToggleItem: "li.dt-button.buttons-columnVisibility", // Column toggle item
    activeColumnItem: "li.dt-button-active-a", // Active/visible column item
    tableHeader: "#GrantApplicationsTable thead tr", // Table header row
    tableHeaderColumn: "#GrantApplicationsTable thead th", // Table header column

    // Save View Dropdown Elements
    saveViewDropdown: "ul.dropdown-menu.show", // Save view dropdown menu
    saveAsViewOption: 'a.dropdown-item:contains("Save As View")', // Save As View option
    resetDefaultViewOption: 'a.dropdown-item:contains("Reset to Default View")', // Reset to Default View option
    deleteAllViewsOption: 'a.dropdown-item:contains("Delete All Views")', // Delete All Views option
    emptyStatesMessage: ".dtsr-emptyStates", // No saved views message

    // Filter Popover Elements
    filterPopover: ".popover-body", // Filter popover body
    showFilterCheckbox: "#showFilter", // Show Filter Row checkbox
    showFilterLabel: 'label[for="showFilter"]', // Show Filter Row label
    clearFilterButton: "#btnClearFilter", // Clear Filter button

    // Application Summary Side Pane Elements
    summaryOffcanvas: "#applicationAsssessmentSummary", // Summary side pane container
    closeSummaryButton: "#closeSummaryCanvas", // Close summary button
    summaryWidgetArea: "#summaryWidgetArea", // Summary widget area
    summaryTable: ".summary-table", // Summary table
    displayInputLabel: ".display-input-label", // Display input labels

    // Payment Request Modal Elements
    paymentModal: ".modal-content", // Payment modal container
    paymentModalTitle: ".modal-title", // Payment modal title
    paymentModalCloseButton:
      '.payment-modal-header .modal-title:contains("Request Payment") ~ .btn-close', // Payment modal close button
    applicationCountInput: "#ApplicationCount", // Number of payments input
    totalAmountInput: "#TotalAmount", // Total amount input
    invoiceNumberInput: 'input[id*="InvoiceNumber"]', // Invoice number input
    amountInput: 'input[id*="_Amount"]', // Amount input
    siteNameInput: 'input[id*="SiteName"]', // Site name input
    descriptionInput: 'input[id*="Description"]', // Description input
    submitPaymentButton: "#btnSubmitPayment", // Submit payment button
    cancelPaymentButton: 'button[onclick="closePaymentModal()"]', // Cancel payment button

    // Tags Modal Elements
    tagsModal: ".modal-content", // Tags modal container
    tagsModalTitle: '.modal-title:contains("Tags")', // Tags modal title
    tagsModalCloseButton: ".modal-header .btn-close", // Tags modal close button
    tagsInputControl: "#tags-input-control", // Tags input control
    tagsAddButton: ".tags-add-button", // Tags add button
    selectedTagsInput: "#SelectedTags", // Selected tags input
    selectedApplicationIdsInput: "#SelectedApplicationIds", // Selected application IDs
    actionTypeInput: "#ActionType", // Action type input
    assignTagsSaveButton: "#assignTagsModelSaveBtn", // Save tags button
    tagsCancelButton: '.modal-footer button[data-bs-dismiss="modal"]', // Cancel tags button

    // Approve Applications Modal Elements
    approveModal: ".modal-content", // Approve modal container
    approveModalTitle: '.modal-title:contains("Approve Applications")', // Approve modal title
    approveModalCloseButton: ".batch-approval-modal-header .btn-close", // Approve modal close button
    applicationsCountInput: "#ApplicationsCount", // Applications count input
    maxBatchCountInput: "#MaxBatchCount", // Max batch count input
    requestedAmountInput: 'input[id*="RequestedAmount"]', // Requested amount input
    recommendedAmountInput: 'input[id*="RecommendedAmount"]', // Recommended amount input
    approvedAmountInput: 'input[id*="ApprovedAmount"]', // Approved amount input
    decisionDateInput: 'input[id*="DecisionDate"]', // Decision date input
    directApprovalCheckbox: 'input[id*="isDirectApproval"]', // Direct approval checkbox
    submitBatchApprovalButton: "#btnSubmitBatchApproval", // Submit approval button
    cancelBatchApprovalButton: "#btnCancelBatchApproval", // Cancel approval button
  };

  constructor() {
    super();
  }

  // ============ Search Methods ============

  /**
   * Verify search bar exists and is visible
   */
  verifySearchBarExists(): void {
    this.getElement(this.selectors.searchInput)
      .should("exist")
      .and("be.visible")
      .and("have.attr", "placeholder", "Search");
  }

  /**
   * Verify search input has correct type and placeholder
   */
  verifySearchBarAttributes(): void {
    this.getElement(this.selectors.searchInput)
      .should("have.attr", "placeholder", "Search")
      .and("have.attr", "type", "search");
  }

  /**
   * Type text in search bar
   */
  searchFor(text: string): void {
    this.typeText(this.selectors.searchInput, text);
  }

  /**
   * Clear search bar
   */
  clearSearch(): void {
    this.getElement(this.selectors.searchInput).clear();
  }

  /**
   * Verify search bar has specific value
   */
  verifySearchValue(value: string): void {
    this.getElement(this.selectors.searchInput).should("have.value", value);
  }

  // ============ Date Filter Methods ============

  /**
   * Verify Submitted Date From filter exists
   */
  verifySubmittedFromDateExists(): void {
    this.getElement(this.selectors.submittedFromDate)
      .should("exist")
      .and("be.visible")
      .and("have.attr", "type", "date");
    this.getElement(this.selectors.submittedFromDateLabel).should("be.visible");
  }

  /**
   * Verify Submitted Date To filter exists
   */
  verifySubmittedToDateExists(): void {
    this.getElement(this.selectors.submittedToDate)
      .should("exist")
      .and("be.visible")
      .and("have.attr", "type", "date");
    this.getElement(this.selectors.submittedToDateLabel).should("be.visible");
  }

  /**
   * Verify submitted from date attributes
   */
  verifySubmittedFromDateAttributes(): void {
    this.getElement(this.selectors.submittedFromDate)
      .should("have.attr", "type", "date")
      .and("have.attr", "name", "SubmittedFromDate");
  }

  /**
   * Verify submitted to date attributes
   */
  verifySubmittedToDateAttributes(): void {
    this.getElement(this.selectors.submittedToDate)
      .should("have.attr", "type", "date")
      .and("have.attr", "name", "SubmittedToDate");
  }

  /**
   * Verify submitted from date value
   */
  verifySubmittedFromDateValue(expectedDate: string): void {
    this.getElement(this.selectors.submittedFromDate).should(
      "have.value",
      expectedDate,
    );
  }

  /**
   * Verify submitted to date value
   */
  verifySubmittedToDateValue(expectedDate: string): void {
    this.getElement(this.selectors.submittedToDate).should(
      "have.value",
      expectedDate,
    );
  }

  /**
   * Verify submitted from date label text
   */
  verifySubmittedFromDateLabel(): void {
    this.getElement(this.selectors.submittedFromDateLabel)
      .should("be.visible")
      .and("contain.text", "Submitted Date From");
  }

  /**
   * Verify submitted to date label text
   */
  verifySubmittedToDateLabel(): void {
    this.getElement(this.selectors.submittedToDateLabel)
      .should("be.visible")
      .and("contain.text", "Submitted Date To");
  }

  /**
   * Set date range filters
   */
  setDateRange(fromDate: string, toDate: string): void {
    this.getElement(this.selectors.submittedFromDate).clear().type(fromDate);
    this.getElement(this.selectors.submittedToDate).clear().type(toDate);
  }

  /**
   * Clear date filters
   */
  clearDateFilters(): void {
    this.getElement(this.selectors.submittedFromDate).clear();
    this.getElement(this.selectors.submittedToDate).clear();
  }

  // ============ Action Button Visibility Methods ============

  /**
   * Verify action buttons are NOT visible (no selection)
   */
  verifyActionButtonsHidden(): void {
    const environment = Cypress.env("environment");
    this.getElement(this.selectors.btnOpen).should("not.be.visible");
    this.getElement(this.selectors.btnAssign).should("not.be.visible");
    this.getElement(this.selectors.btnApprove).should("not.be.visible");
    this.getElement(this.selectors.btnTags).should("not.be.visible");
    if (environment !== "PROD") {
      this.getElement(this.selectors.btnPayment).should("not.be.visible");
    }
    this.getElement(this.selectors.btnInfo).should("not.be.visible");
  }

  /**
   * Verify all action buttons are visible (after selection)
   */
  verifyActionButtonsVisible(): void {
    const environment = Cypress.env("environment");
    this.getElement(this.selectors.btnOpen).should("be.visible");
    this.getElement(this.selectors.btnAssign).should("be.visible");
    this.getElement(this.selectors.btnApprove).should("be.visible");
    this.getElement(this.selectors.btnTags).should("be.visible");
    if (environment !== "PROD") {
      this.getElement(this.selectors.btnPayment).should("be.visible");
    }
    this.getElement(this.selectors.btnInfo).should("be.visible");
  }

  /**
   * Verify filter button is always visible
   */
  verifyFilterButtonVisible(): void {
    this.getElement(this.selectors.btnFilter)
      .should("exist")
      .and("be.visible")
      .and("contain.text", "Filter");
  }

  // ============ Action Button Attribute Verification ============

  /**
   * Verify Open button attributes
   */
  verifyOpenButtonAttributes(): void {
    this.getElement(this.selectors.btnOpen)
      .and("contain.text", "Open")
      .and("have.attr", "data-selector", "applications-table-actions");
  }

  /**
   * Verify Assign button attributes
   */
  verifyAssignButtonAttributes(): void {
    this.getElement(this.selectors.btnAssign)
      .should("be.visible")
      .and("contain.text", "Assign")
      .and("have.attr", "data-selector", "applications-table-actions");
  }

  /**
   * Verify Approve button attributes
   */
  verifyApproveButtonAttributes(): void {
    this.getElement(this.selectors.btnApprove)
      .should("be.visible")
      .and("contain.text", "Approve")
      .and("have.attr", "data-selector", "applications-table-actions");
  }

  /**
   * Verify Tags button attributes
   */
  verifyTagsButtonAttributes(): void {
    this.getElement(this.selectors.btnTags)
      .should("be.visible")
      .and("contain.text", "Tags")
      .and("have.attr", "data-selector", "applications-table-actions");
  }

  /**
   * Verify Payment button attributes
   */
  verifyPaymentButtonAttributes(): void {
    const environment = Cypress.env("environment");
    if (environment === "PROD") {
      cy.log(
        "Skipping payment button attribute verification in PROD environment",
      );
      return;
    }
    this.getElement(this.selectors.btnPayment)
      .should("be.visible")
      .and("contain.text", "Payment")
      .and("have.attr", "data-selector", "applications-table-actions");
  }

  /**
   * Verify Info button attributes
   */
  verifyInfoButtonAttributes(): void {
    this.getElement(this.selectors.btnInfo)
      .should("be.visible")
      .and("contain.text", "Info")
      .and("have.attr", "data-selector", "applications-table-actions");
  }

  // ============ Additional Menu Items ============

  /**
   * Verify Export button exists and has correct attributes
   */
  verifyExportButton(): void {
    this.getElement(this.selectors.btnExport)
      .should("exist")
      .and("be.visible")
      .and("have.attr", "aria-controls", "GrantApplicationsTable");
  }

  /**
   * Verify Save View button exists and has correct attributes
   */
  verifySaveViewButton(): void {
    this.getElement(this.selectors.btnSaveView)
      .should("exist")
      .and("be.visible")
      .and("contain.text", "Save View")
      .and("have.attr", "aria-controls", "GrantApplicationsTable");
  }

  /**
   * Verify Columns button exists and has correct attributes
   */
  verifyColumnsButton(): void {
    this.getElement(this.selectors.btnColumns)
      .should("exist")
      .and("be.visible")
      .and("have.attr", "aria-controls", "GrantApplicationsTable");
  }

  // ============ Container Structure Verification ============

  /**
   * Verify action bar container structure
   */
  verifyActionBarStructure(): void {
    this.getElement(this.selectors.actionBarContainer)
      .should("exist")
      .and("be.visible");
    this.getElement(this.selectors.searchWrapper)
      .should("exist")
      .and("be.visible");
    this.getElement(this.selectors.customButtonsGroup)
      .should("exist")
      .and("be.visible");
    this.getElement(this.selectors.dynamicButtonContainer)
      .should("exist")
      .and("be.visible");
  }

  /**
   * Verify all action buttons have correct type attribute
   */
  verifyButtonTypes(): void {
    const environment = Cypress.env("environment");
    const buttonIds = [
      this.selectors.btnOpen,
      this.selectors.btnAssign,
      this.selectors.btnApprove,
      this.selectors.btnTags,
      this.selectors.btnInfo,
      this.selectors.btnFilter,
    ];

    // Only add payment button if not in PROD environment
    if (environment !== "PROD") {
      buttonIds.push(this.selectors.btnPayment);
    }

    buttonIds.forEach((buttonId) => {
      this.getElement(buttonId).should("have.attr", "type", "button");
    });
  }

  // ============ Table Selection Methods ============

  /**
   * Select first application in the table
   */
  selectFirstApplication(): void {
    cy.get(this.selectors.tableRow)
      .first()
      .within(() => {
        cy.get(this.selectors.rowCheckbox).check({ force: true });
      });
  }

  /**
   * Unselect first application in the table
   */
  unselectFirstApplication(): void {
    cy.get(this.selectors.tableRow)
      .first()
      .within(() => {
        cy.get(this.selectors.rowCheckbox).uncheck({ force: true });
      });
  }

  /**
   * Search for an application by ID and select it
   * @param applicationId - The application ID to search for and select
   */
  selectSearchedApplicationById(applicationId: string): void {
    this.searchFor(applicationId);
    cy.wait(500); // Wait for search results to update
    cy.get(this.selectors.tableRow)
      .first()
      .within(() => {
        cy.get(this.selectors.rowCheckbox).check({ force: true });
      });
  }
  /**
   * Clear all selections
   */
  clearAllSelections(): void {
    cy.get("body").then(($body) => {
      if (
        $body.find(`${this.selectors.selectAllCheckbox}:checked`).length > 0
      ) {
        this.getElement(this.selectors.selectAllCheckbox).uncheck({
          force: true,
        });
      }
    });
  }

  /**
   * Wait for UI update after selection change
   */
  waitForUIUpdate(ms: number = 500): void {
    cy.wait(ms);
  }

  // ============ Save View Dropdown Methods ============

  /**
   * Click Save View button to open/close dropdown
   */
  clickSaveViewButton(): void {
    this.getElement(this.selectors.btnSaveView).click({ force: true });
    cy.wait(300); // Wait for dropdown animation
  }

  /**
   * Verify Save View dropdown is visible
   */
  verifySaveViewDropdownVisible(): void {
    this.getElement(this.selectors.saveViewDropdown).should("be.visible");
  }

  /**
   * Verify Save View dropdown is not visible
   */
  verifySaveViewDropdownHidden(): void {
    cy.get(this.selectors.saveViewDropdown).should("not.exist");
  }

  /**
   * Verify all Save View dropdown options exist
   */
  verifySaveViewDropdownOptions(): void {
    // Verify Save As View option
    cy.get(this.selectors.saveAsViewOption)
      .should("be.visible")
      .and("contain.text", "Save As View");

    // Verify Reset to Default View option
    cy.get(this.selectors.resetDefaultViewOption)
      .should("be.visible")
      .and("contain.text", "Reset to Default View");

    // Verify Delete All Views option
    cy.get(this.selectors.deleteAllViewsOption)
      .should("be.visible")
      .and("contain.text", "Delete All Views");

    // Verify empty states message
    cy.get(this.selectors.emptyStatesMessage)
      .should("be.visible")
      .and("contain.text", "No saved views");
  }

  /**
   * Close Save View dropdown by clicking button again
   */
  closeSaveViewDropdown(): void {
    cy.get("body").click(0, 0); // Click at top-left of body
    cy.wait(300); // Wait for dropdown to close
  }

  // ============ Filter Popover Methods ============

  /**
   * Click Filter button to open/close popover
   */
  clickFilterButton(): void {
    this.getElement(this.selectors.btnFilter).click({ force: true });
    cy.wait(300); // Wait for popover animation
  }

  /**
   * Verify Filter popover is visible
   */
  verifyFilterPopoverVisible(): void {
    this.getElement(this.selectors.filterPopover).should("be.visible");
  }

  /**
   * Verify Filter popover is not visible
   */
  verifyFilterPopoverHidden(): void {
    cy.get(this.selectors.filterPopover).should("not.exist");
  }

  /**
   * Verify all Filter popover elements exist
   */
  verifyFilterPopoverElements(): void {
    // Verify Show Filter checkbox
    cy.get(this.selectors.showFilterCheckbox)
      .should("be.visible")
      .and("have.attr", "type", "checkbox")
      .and("have.attr", "id", "showFilter");

    // Verify Show Filter label
    cy.get(this.selectors.showFilterLabel)
      .should("be.visible")
      .and("contain.text", "Show Filter Row");

    // Verify Clear Filter button
    cy.get(this.selectors.clearFilterButton)
      .should("be.visible")
      .and("contain.text", "CLEAR FILTER")
      .and("have.attr", "type", "button");
  }

  /**
   * Close Filter popover by clicking button again
   */
  closeFilterPopover(): void {
    this.clickFilterButton();
  }

  // ============ Column Visibility Methods ============

  /**
   * Click the Columns button to open/close dropdown
   */
  clickColumnsButton(): void {
    this.getElement(this.selectors.btnColumns).click({ force: true });
    cy.wait(300); // Wait for dropdown animation
  }

  /**
   * Close the dropdown by clicking elsewhere on the page
   */
  closeColumnsDropdown(): void {
    // Click on the table or action bar area to close dropdown
    cy.get("body").click(0, 0); // Click at top-left of body
    cy.wait(300); // Wait for dropdown to close
  }

  /**
   * Get currently selected column names
   * @returns Array of column names that are currently visible
   */
  getSelectedColumns(): Cypress.Chainable<string[]> {
    return cy
      .get(this.selectors.activeColumnItem)
      .find("a.dropdown-item")
      .then(($items) => {
        const columnNames: string[] = [];
        $items.each((_, item) => {
          columnNames.push(item.innerText.trim());
        });
        return columnNames;
      });
  }

  /**
   * Get all available column names from dropdown
   * @returns Array of all column names in the dropdown
   */
  getAllColumnNames(): Cypress.Chainable<string[]> {
    return cy
      .get(`${this.selectors.columnToggleItem} a.dropdown-item`)
      .then(($items) => {
        const columnNames: string[] = [];
        $items.each((_, item) => {
          columnNames.push(item.innerText.trim());
        });
        return columnNames;
      });
  }

  /**
   * Select all columns in the dropdown
   */
  selectAllColumns(): void {
    cy.get(this.selectors.columnToggleItem).each(($el) => {
      if (!$el.hasClass("dt-button-active-a")) {
        cy.wrap($el).find("a.dropdown-item").click({ force: true });
        cy.wait(100); // Small wait between clicks
      }
    });
  }

  /**
   * Deselect all columns in the dropdown
   */
  deselectAllColumns(): void {
    cy.get(this.selectors.activeColumnItem).each(($el) => {
      cy.wrap($el).find("a.dropdown-item").click({ force: true });
      cy.wait(100); // Small wait between clicks
    });
  }

  /**
   * Restore columns to a specific state
   * @param columnNames Array of column names to make visible
   */
  restoreColumns(columnNames: string[]): void {
    // First, deselect all columns
    cy.get(this.selectors.activeColumnItem).each(($el) => {
      const columnName = $el.find("a.dropdown-item").text().trim();
      if (!columnNames.includes(columnName)) {
        cy.wrap($el).find("a.dropdown-item").click({ force: true });
        cy.wait(100);
      }
    });

    // Then, select the columns that should be visible
    cy.get(this.selectors.columnToggleItem).each(($el) => {
      const columnName = $el.find("a.dropdown-item").text().trim();
      if (
        columnNames.includes(columnName) &&
        !$el.hasClass("dt-button-active-a")
      ) {
        cy.wrap($el).find("a.dropdown-item").click({ force: true });
        cy.wait(100);
      }
    });
  }

  /**
   * Verify table has specific columns visible
   * @param expectedColumns Array of column names that should be visible in the table
   */
  verifyTableColumns(expectedColumns: string[]): void {
    cy.get(this.selectors.tableHeaderColumn).then(($headers) => {
      const visibleColumns: string[] = [];
      $headers.each((_, header) => {
        const columnTitle = Cypress.$(header)
          .find(".dt-column-title")
          .text()
          .trim();
        if (columnTitle && columnTitle !== "") {
          visibleColumns.push(columnTitle);
        }
      });

      // Verify each expected column is present
      expectedColumns.forEach((expectedCol) => {
        expect(visibleColumns).to.include(expectedCol);
      });
    });
  }

  /**
   * Get visible column count from table header
   * @returns Number of visible columns in the table
   */
  getVisibleColumnCount(): Cypress.Chainable<number> {
    return cy.get(this.selectors.tableHeaderColumn).its("length");
  }

  /**
   * Verify dropdown is visible
   */
  verifyColumnsDropdownVisible(): void {
    this.getElement(this.selectors.columnsDropdown).should("be.visible");
  }

  /**
   * Verify dropdown is not visible
   */
  verifyColumnsDropdownHidden(): void {
    cy.get(this.selectors.columnsDropdown).should("not.exist");
  }

  // ============ Application Summary Side Pane Methods ============

  /**
   * Click Info button to open application summary side pane
   */
  clickInfoButton(): void {
    this.getElement(this.selectors.btnInfo).click();
    cy.wait(500); // Wait for side pane animation
  }

  /**
   * Verify application summary side pane is open and visible
   */
  verifySummarySidePaneVisible(): void {
    this.getElement(this.selectors.summaryOffcanvas)
      .should("be.visible")
      .and("have.class", "show");
  }

  /**
   * Verify all labels are present in the summary side pane
   */
  verifySummaryLabels(): void {
    // Wait for the summary panel to load
    cy.wait(2000);

    // Unity Application Id section labels
    const unityAppLabels = [
      "Owner",
      "Assignees",
      "Category",
      "Submission Date",
      "Registered Organization Name",
      "Registered Organization Number",
      "Economic Region",
      "Regional District",
      "Community",
      "Requested Amount",
      "Total Project Budget",
      "Sector",
    ];

    // Assessment Summary section labels
    const assessmentLabels = [
      "Status",
      "Likelihood Of Funding",
      "Assessment Start Date",
      "Decision Date",
      "Total Score",
      "Assessment Result",
      "Recommended Amount",
      "Approved Amount",
    ];

    // Verify Unity Application Id section header (if it exists)
    cy.get("body").then(($body) => {
      if ($body.find('h6:contains("Unity Application Id")').length > 0) {
        cy.contains("h6", "Unity Application Id", {
          timeout: 20000,
          includeShadowDom: true,
        })
          .should("exist")
          .scrollIntoView()
          .should("be.visible");

        // Verify all Unity Application Id labels
        unityAppLabels.forEach((label) => {
          cy.contains(this.selectors.displayInputLabel, label)
            .scrollIntoView()
            .should("be.visible")
            .and("have.attr", "for", this.convertLabelToId(label));
        });
      } else {
        cy.log(
          "Unity Application Id section not found - skipping verification",
        );
        // Still verify the labels are present even without the header
        unityAppLabels.forEach((label) => {
          cy.contains(this.selectors.displayInputLabel, label)
            .scrollIntoView()
            .should("be.visible")
            .and("have.attr", "for", this.convertLabelToId(label));
        });
      }
    });

    // Scroll to Assessment Summary section to make it visible
    cy.contains("h6", "Assessment Summary").scrollIntoView();
    cy.wait(300); // Wait for scroll animation

    // Verify Assessment Summary section header
    cy.contains("h6", "Assessment Summary").should("be.visible");

    // Verify all Assessment Summary labels
    assessmentLabels.forEach((label) => {
      cy.contains(this.selectors.displayInputLabel, label)
        .scrollIntoView()
        .should("be.visible")
        .and("have.attr", "for", this.convertLabelToId(label));
    });
  }

  /**
   * Convert label text to expected 'for' attribute ID
   * @param label The label text
   * @returns The expected ID value
   */
  private convertLabelToId(label: string): string {
    // Remove spaces and convert to expected ID format
    const idMap: { [key: string]: string } = {
      Owner: "Owner",
      Assignees: "Assignees",
      Category: "Category",
      "Submission Date": "SubmissionDate",
      "Registered Organization Name": "OrganizationName",
      "Registered Organization Number": "OrganizationNumber",
      "Economic Region": "EconomicRegion",
      "Regional District": "RegionalDistrict",
      Community: "Community",
      "Requested Amount": "RequestedAmount",
      "Total Project Budget": "ProjectBudget",
      Sector: "Sector",
      Status: "Status",
      "Likelihood Of Funding": "LikelihoodOfFunding",
      "Assessment Start Date": "AssessmentStartDate",
      "Decision Date": "FinalDecisionDate",
      "Total Score": "TotalScore",
      "Assessment Result": "AssessmentResult",
      "Recommended Amount": "RecommendedAmount",
      "Approved Amount": "ApprovedAmount",
    };

    return idMap[label] || label.replace(/\s+/g, "");
  }

  /**
   * Close the application summary side pane
   */
  closeSummarySidePane(): void {
    this.getElement(this.selectors.closeSummaryButton).click();
    cy.wait(500); // Wait for side pane to close
  }

  /**
   * Verify application summary side pane is closed
   */
  verifySummarySidePaneClosed(): void {
    cy.get(this.selectors.summaryOffcanvas).should("not.have.class", "show");
  }

  // ============ Payment Request Modal Methods ============

  /**
   * Click Payment button to open payment request modal
   */
  clickPaymentButton(): void {
    const environment = Cypress.env("environment");
    if (environment === "PROD") {
      cy.log("Skipping payment button click in PROD environment");
      return;
    }
    this.getElement(this.selectors.btnPayment).click();
    cy.wait(500); // Wait for modal animation
  }

  /**
   * Verify payment request modal is visible
   */
  verifyPaymentModalVisible(): void {
    const environment = Cypress.env("environment");
    if (environment === "PROD") {
      cy.log("Skipping payment modal verification in PROD environment");
      return;
    }
    cy.contains(this.selectors.paymentModalTitle, "Request Payment").should(
      "be.visible",
    );
  }

  /**
   * Verify all payment modal elements are present
   */
  verifyPaymentModalElements(): void {
    const environment = Cypress.env("environment");
    if (environment === "PROD") {
      cy.log(
        "Skipping payment modal elements verification in PROD environment",
      );
      return;
    }
    // Verify modal title
    cy.contains(this.selectors.paymentModalTitle, "Request Payment").should(
      "be.visible",
    );
    //
    //     // Verify batch information fields
    //     cy.get(this.selectors.batchNumberInput)
    //       .should("be.visible")
    //       .and("be.disabled");

    cy.get(this.selectors.applicationCountInput)
      .should("be.visible")
      .and("have.attr", "id", "ApplicationCount")
      .and("be.disabled");

    cy.get(this.selectors.totalAmountInput)
      .should("be.visible")
      .and("have.attr", "id", "TotalAmount")
      .and("be.disabled");

    // Verify payment form fields
    cy.get(this.selectors.invoiceNumberInput)
      .should("be.visible")
      .and("be.disabled");

    cy.get(this.selectors.amountInput).should("be.visible").and("be.disabled");

    cy.get(this.selectors.siteNameInput)
      .should("be.visible")
      .and("be.disabled");

    cy.get(this.selectors.descriptionInput)
      .should("be.visible")
      .and("be.disabled");

    // Verify buttons
    cy.get(this.selectors.submitPaymentButton)
      .should("be.visible")
      .and("contain.text", "Submit Payment Requests");

    cy.get(this.selectors.cancelPaymentButton)
      .should("be.visible")
      .and("contain.text", "Cancel");
  }

  /**
   * Close payment request modal
   */
  closePaymentModal(): void {
    const environment = Cypress.env("environment");
    if (environment === "PROD") {
      cy.log("Skipping payment modal close in PROD environment");
      return;
    }
    cy.get(this.selectors.cancelPaymentButton).click();
    cy.wait(500); // Wait for modal to close
  }

  /**
   * Verify payment modal is closed
   */
  verifyPaymentModalClosed(): void {
    const environment = Cypress.env("environment");
    if (environment === "PROD") {
      cy.log("Skipping payment modal closed verification in PROD environment");
      return;
    }
    cy.contains(this.selectors.paymentModalTitle, "Request Payment").should(
      "not.exist",
    );
  }

  // ============ Tags Modal Methods ============

  /**
   * Click Tags button to open tags modal
   */
  clickTagsButton(): void {
    this.getElement(this.selectors.btnTags).click();
    cy.wait(500); // Wait for modal animation
  }

  /**
   * Verify tags modal is visible
   */
  verifyTagsModalVisible(): void {
    cy.get(this.selectors.tagsModalTitle).should("be.visible");
  }

  /**
   * Verify all tags modal elements are present
   */
  verifyTagsModalElements(): void {
    // Verify modal title
    cy.get(this.selectors.tagsModalTitle).should("be.visible");

    // Verify tags input control
    cy.get(this.selectors.tagsInputControl)
      .should("be.visible")
      .and("have.attr", "placeholder", "Add a tag...");

    // Verify tags add button
    cy.get(this.selectors.tagsAddButton)
      .should("be.visible")
      .and("contain.text", "+");

    // Verify hidden inputs
    cy.get(this.selectors.selectedTagsInput).should("exist");

    cy.get(this.selectors.selectedApplicationIdsInput)
      .should("exist")
      .and("have.attr", "id", "SelectedApplicationIds");

    cy.get(this.selectors.actionTypeInput)
      .should("exist")
      .and("have.attr", "id", "ActionType");

    // Verify buttons
    cy.get(this.selectors.assignTagsSaveButton)
      .should("be.visible")
      .and("contain.text", "Save");

    cy.get(this.selectors.tagsCancelButton)
      .should("be.visible")
      .and("contain.text", "Cancel");
  }

  /**
   * Close tags modal
   */
  closeTagsModal(): void {
    cy.get(this.selectors.tagsCancelButton).click();
    cy.wait(500); // Wait for modal to close
  }

  /**
   * Verify tags modal is closed
   */
  verifyTagsModalClosed(): void {
    cy.get(this.selectors.tagsModalTitle).should("not.exist");
  }

  // ============ Approve Applications Modal Methods ============

  /**
   * Click Approve button to open approve applications modal
   */
  clickApproveButton(): void {
    this.getElement(this.selectors.btnApprove).click();
    cy.wait(500); // Wait for modal animation
  }

  /**
   * Verify approve applications modal is visible
   */
  verifyApproveModalVisible(): void {
    cy.get(this.selectors.approveModalTitle).should("be.visible");
  }

  /**
   * Verify all approve modal elements are present
   */
  verifyApproveModalElements(): void {
    // Verify modal title
    cy.get(this.selectors.approveModalTitle).should("be.visible");

    // Verify batch count inputs
    cy.get(this.selectors.applicationsCountInput)
      .should("exist")
      .and("have.attr", "id", "ApplicationsCount");

    cy.get(this.selectors.maxBatchCountInput)
      .should("exist")
      .and("have.attr", "id", "MaxBatchCount");

    // Verify amount fields
    cy.get(this.selectors.requestedAmountInput).should("be.visible");

    cy.get(this.selectors.recommendedAmountInput).should("be.visible");

    cy.get(this.selectors.approvedAmountInput).should("be.visible");

    // Verify decision date input
    cy.get(this.selectors.decisionDateInput)
      .should("be.visible")
      .and("have.attr", "type", "date");

    // Verify direct approval checkbox
    cy.get(this.selectors.directApprovalCheckbox).should("be.visible");

    // Verify buttons
    cy.get(this.selectors.submitBatchApprovalButton)
      .should("be.visible")
      .and("contain.text", "Approve");

    cy.get(this.selectors.cancelBatchApprovalButton)
      .should("be.visible")
      .and("contain.text", "Cancel");
  }

  /**
   * Close approve applications modal
   */
  closeApproveModal(): void {
    cy.get(this.selectors.cancelBatchApprovalButton).click();
    cy.wait(500); // Wait for modal to close
  }

  /**
   * Verify approve modal is closed
   */
  verifyApproveModalClosed(): void {
    cy.get(this.selectors.approveModalTitle).should("not.exist");
  }
}
