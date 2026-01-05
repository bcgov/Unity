import { PageFactory } from "../utilities/PageFactory";

describe("Grant Manager Login and Top Navigation", () => {
  it("Verify Login", () => {
    const loginPage = PageFactory.getLoginPage();
    loginPage.login();
  });

  it("Verify navigation options in the top banner", () => {
    const navPage = PageFactory.getNavigationPage();

    // Verify Default Grant Program tenant is selected.
    navPage.clickUserMenu();
    navPage.verifyCurrentTenant("Default Grants Program");

    // Ensure all expected headings are present.
    // Applications
    navPage.goToApplications();
    cy.wait(1000);

    // Roles
    navPage.goToRoles();

    // Users
    navPage.goToUsers();

    // Intakes
    navPage.goToIntakes();

    // Forms
    navPage.goToForms();

    // Dashboard
    navPage.goToDashboard();

    // Payments
    navPage.goToPayments();

    // Return to top
    cy.visit(Cypress.env("webapp.url"));
  });

  it("Verify Logout", () => {
    const loginPage = PageFactory.getLoginPage();
    loginPage.logout();
  });
});
