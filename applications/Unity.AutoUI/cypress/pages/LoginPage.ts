import { BasePage } from "./BasePage";

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
    this.visit();
    this.clickByText(this.selectors.loginButton);
    this.wait(1000);
    this.clickByText(this.selectors.idirButton);
    this.wait(1000);

    // Check if already logged in
    cy.get("body").then(($body) => {
      if ($body.find(this.selectors.usernameField).length) {
        const user = username || Cypress.env("test1username");
        const pass = password || Cypress.env("test1password");

        this.typeText(this.selectors.usernameField, user);
        this.typeText(this.selectors.passwordField, pass);
        this.clickByText(this.selectors.continueButton);
      } else {
        cy.log("Already logged in");
      }
    });
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
    cy.login();
  }

  /**
   * Quick logout using custom command (backward compatibility)
   */
  quickLogout(): void {
    cy.logout();
  }
}
