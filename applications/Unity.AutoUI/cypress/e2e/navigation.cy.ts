import { PageFactory } from "../utilities/PageFactory";

describe("Grant Manager Login and Top Navigation", () => {
  const loginPage = PageFactory.getLoginPage();
  const navigationPage = PageFactory.getNavigationPage();

  it("Verify Login", () => {
    // Always start from the base URL
    loginPage.visit();

    cy.get("body").then(($body) => {
      // Already authenticated
      if ($body.find('button:contains("VIEW APPLICATIONS")').length > 0) {
        cy.contains("VIEW APPLICATIONS").click();
        return;
      }

      // Not authenticated
      if ($body.find('button:contains("LOGIN")').length > 0) {
        cy.contains("LOGIN").click();

        cy.get("body").then(($loginBody) => {
          // IDIR chooser may or may not appear
          if ($loginBody.find(':contains("IDIR")').length > 0) {
            cy.contains("IDIR").click();
          }

          cy.get("body").then(($authBody) => {
            if ($authBody.find("#user").length > 0) {
              cy.get("#user").type(Cypress.env("test1username"));
              cy.get("#password").type(Cypress.env("test1password"));
              cy.contains("Continue").click();
            }
          });
        });

        return;
      }

      throw new Error("Unable to determine authentication state");
    });

    cy.location("pathname", { timeout: 30000 }).should(
      "include",
      "/GrantApplications"
    );
  });

  it("Verify navigation options in the top banner", () => {
    // Verify Default Grant Program tenant is selected
    navigationPage.clickUserMenu();
    navigationPage.verifyCurrentTenant("Default Grants Program");

    // Verify all expected navigation items exist
    navigationPage.verifyNavItemExists("Applications");
    navigationPage.verifyNavItemExists("Roles");
    navigationPage.verifyNavItemExists("Users");
    navigationPage.verifyNavItemExists("Intakes");
    navigationPage.verifyNavItemExists("Forms");
    navigationPage.verifyNavItemExists("Dashboard");
    navigationPage.verifyNavItemExists("Payments");

    // Click each navigation item
    navigationPage.goToApplications();
    navigationPage.goToRoles();
    navigationPage.goToUsers();
    navigationPage.goToIntakes();
    navigationPage.goToForms();
    navigationPage.goToDashboard();
    navigationPage.goToPayments();

    // Return to top
    loginPage.visit();
  });

  it("Verify Logout", () => {
    loginPage.quickLogout();
  });
});
