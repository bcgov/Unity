/**
 * BasePage - Abstract base class for all Pages
 */
export abstract class BasePage {
  protected url: string;

  constructor(url: string = "") {
    this.url = url;
  }

  /**
   * Navigate to the page
   */
  visit(): void {
    if (this.url) {
      cy.visit(this.url);
    }
  }

  /**
   * Get element by selector with optional timeout
   */
  protected getElement(selector: string, timeout?: number) {
    return timeout ? cy.get(selector, { timeout }) : cy.get(selector);
  }

  /**
   * Click element by selector
   */
  protected clickElement(selector: string): void {
    this.getElement(selector).should("exist").click();
  }

  /**
   * Type text into an input field
   */
  protected typeText(selector: string, text: string): void {
    this.getElement(selector).should("exist").clear().type(text);
  }

  /**
   * Verify element contains text
   */
  protected verifyContainsText(selector: string, text: string): void {
    this.getElement(selector).should("contain.text", text);
  }

  /**
   * Verify element has specific text
   */
  protected verifyText(selector: string, text: string): void {
    this.getElement(selector).should("have.text", text);
  }

  /**
   * Verify input field has value
   */
  protected verifyInputValue(selector: string, value: string): void {
    this.getElement(selector).should("have.value", value);
  }

  /**
   * Wait for a specific duration
   */
  protected wait(ms: number): void {
    cy.wait(ms);
  }

  /**
   * Select dropdown option by value
   */
  protected selectDropdown(selector: string, value: string): void {
    this.getElement(selector).select(value);
  }

  /**
   * Verify dropdown has selected value
   */
  protected verifyDropdownValue(selector: string, value: string): void {
    this.getElement(selector).should("have.value", value);
  }

  /**
   * Check if element exists
   */
  protected elementExists(selector: string): Cypress.Chainable<boolean> {
    return cy.get("body").then(($body) => {
      return $body.find(selector).length > 0;
    });
  }

  /**
   * Click element by text content
   */
  protected clickByText(text: string): void {
    cy.contains(text).should("exist").click();
  }

  /**
   * Verify element is visible
   */
  protected verifyVisible(selector: string): void {
    this.getElement(selector).should("be.visible");
  }

  /**
   * Get table row count
   */
  protected getTableRowCount(): Cypress.Chainable<number> {
    return cy.get("tbody tr").its("length");
  }

  /**
   * Verify table has minimum rows
   */
  protected verifyTableHasMinRows(minRows: number): void {
    cy.get("tbody tr").should("have.length.at.least", minRows);
  }

  /**
   * Find element within parent
   */
  protected findWithin(parentSelector: string, childSelector: string) {
    return this.getElement(parentSelector).find(childSelector);
  }

  /**
   * Get element by label text
   */
  protected getByLabel(labelText: string, labelSelector: string = "label") {
    return cy.contains(labelSelector, labelText);
  }

  /**
   * Verify label and next element value
   */
  protected verifyLabelValue(labelFor: string, expectedText: string): void {
    cy.get(`label[for="${labelFor}"]`)
      .next(".display-input")
      .should("include.text", expectedText);
  }

  /**
   * Clear browser cache and storage
   */
  protected clearBrowserData(): void {
    cy.clearCookies();
    cy.clearLocalStorage();
  }
}
