/**
 * TestDataHelper - Utility class for managing test data
 */

export class TestDataHelper {
  /**
   * Get submission detail by key for current environment
   */
  static getSubmissionDetail(key: string): Cypress.Chainable<string> {
    return cy.getSubmissionDetail(key);
  }

  /**
   * Get CHEFS detail by key for current environment
   */
  static getChefsDetail(key: string): Cypress.Chainable<string> {
    return cy.getChefsDetail(key);
  }

  /**
   * Get Metabase detail by key for current environment
   */
  static getMetabaseDetail(key: string): Cypress.Chainable<string> {
    return cy.getMetabaseDetail(key);
  }

  /**
   * Get current environment name
   */
  static getCurrentEnvironment(): string {
    return Cypress.env("environment");
  }

  /**
   * Get web app URL
   */
  static getWebAppUrl(): string {
    return Cypress.env("webapp.url");
  }

  /**
   * Get test user credentials
   */
  static getTestUser(userNumber: 1 | 2 = 1): {
    username: string;
    password: string;
  } {
    return {
      username: Cypress.env(`test${userNumber}username`),
      password: Cypress.env(`test${userNumber}password`),
    };
  }

  /**
   * Generate random string
   */
  static generateRandomString(length: number = 10): string {
    return Math.random()
      .toString(36)
      .substring(2, length + 2);
  }

  /**
   * Generate random email
   */
  static generateRandomEmail(): string {
    return `test_${this.generateRandomString()}@example.com`;
  }

  /**
   * Format currency
   */
  static formatCurrency(amount: number): string {
    return `$${amount.toLocaleString("en-US", {
      minimumFractionDigits: 2,
      maximumFractionDigits: 2,
    })}`;
  }

  /**
   * Parse currency string to number
   */
  static parseCurrency(currencyString: string): number {
    return parseFloat(currencyString.replace(/[$,]/g, ""));
  }

  /**
   * Wait with custom timeout
   */
  static wait(ms: number): void {
    cy.wait(ms);
  }

  /**
   * Retry action with custom attempts
   * Note: This is a simplified retry that doesn't catch errors
   * For proper retry logic, use Cypress's built-in retry assertions
   */
  static retry<T>(
    action: () => Cypress.Chainable<T>,
    maxAttempts: number = 3,
    delayMs: number = 1000,
  ): Cypress.Chainable<T> {
    cy.log(`Executing action with up to ${maxAttempts} attempts`);
    return action();
  }
}
