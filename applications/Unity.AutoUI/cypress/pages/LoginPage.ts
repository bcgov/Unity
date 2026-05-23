import { BasePage } from "./BasePage";
import { loginIfNeeded } from "../support/auth";

/**
 * LoginPage - Page Object for Login/Authentication functionality
 */
export class LoginPage extends BasePage {
  // Selectors
  private readonly selectors = {
    loginButton: "LOGIN",
    idirButton: "IDIR",
    usernameField: "#user",
    passwordField: "#password",
    continueButton: "Continue",
    userInitials: ".unity-user-initials",
    userDropdown: "#user-dropdown .btn-dropdown span",
    logoutLink: "Logout",
  };

  constructor() {
    super(Cypress.env("webapp.url"));
  }

  /**
   * Perform login with credentials
   */
  login(username?: string, password?: string): void {
    loginIfNeeded({
      username,
      password,
      timeout: 30000,
      baseUrl: Cypress.env("webapp.url"),
    });
  }

  /**
   * Verify we are on the authenticated applications page
   */
  verifyOnGrantApplications(): void {
    cy.location("pathname", { timeout: 30000 }).should(
      "include",
      "/GrantApplications",
    );
  }

  /**
   * Perform logout
   */
  logout(): void {
    this.visit();
    cy.request("GET", Cypress.env("webapp.url") + "Account/Logout");
    this.wait(1000);
    this.visit();
  }

  /**
   * Verify user is logged in
   */
  verifyLoggedIn(): void {
    this.getElement(this.selectors.userInitials).should("exist");
  }

  /**
   * Click on user initials dropdown
   */
  clickUserDropdown(): void {
    this.clickElement(this.selectors.userInitials);
  }

  /**
   * Verify tenant is selected
   */
  verifyTenant(tenantName: string): void {
    this.clickUserDropdown();
    this.verifyContainsText(this.selectors.userDropdown, tenantName);
  }

  /**
   * Quick login using custom command (backward compatibility)
   */
  quickLogin(): void {
    this.login();
  }

  /**
   * Quick logout using custom command (backward compatibility)
   */
  quickLogout(): void {
    cy.logout();
  }
}
