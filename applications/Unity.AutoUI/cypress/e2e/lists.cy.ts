import { loginIfNeeded } from "../support/auth";
import { PageFactory } from "../utilities/PageFactory";

describe("Grant Manager Login and List Navigation", () => {
  const loginPage = PageFactory.getLoginPage();
  const navigationPage = PageFactory.getNavigationPage();
  const dashboardPage = PageFactory.getDashboardPage();
  const applicationsPage = PageFactory.getApplicationsPage();
  const rolesPage = PageFactory.getRolesPage();
  const usersPage = PageFactory.getUsersPage();
  const intakesPage = PageFactory.getIntakesPage();
  const formsPage = PageFactory.getFormsPage();

  before(() => {
    loginIfNeeded();
    navigationPage.switchToTenantIfAvailable("Default Grants Program");
    // loginIfNeeded() already handles Keycloak IDIR selection - no need for redundant check
    cy.location("pathname", { timeout: 30000 }).should(
      "include",
      "/GrantApplications",
    );
  });

  it("Verify Applications, Roles, Users, Intakes, Forms, Dashboard lists are populated", () => {
    // Verify Default Grant Program tenant is selected
    navigationPage.clickUserMenu();
    navigationPage.verifyCurrentTenant("Default Grants Program");

    // Verify Applications
    navigationPage.goToApplications();
    applicationsPage.verifyListHasData();

    // Verify Roles
    navigationPage.goToRoles();
    rolesPage.verifyListHasData();

    // Verify Users
    navigationPage.goToUsers();
    usersPage.verifyListHasData();

    // Verify Intakes
    navigationPage.goToIntakes();
    intakesPage.verifyListHasData();

    // Verify Forms
    navigationPage.goToForms();
    formsPage.verifyListHasData();

    // Verify Dashboard
    navigationPage.goToDashboard();
    dashboardPage.setIntakeIfAvailable("Test");

    cy.get("#applicationStatusChart text")
      .first()
      .invoke("text")
      .then((n) => expect(parseInt(n, 10)).to.be.gt(0));

    cy.visit(Cypress.env("webapp.url"));
  });

  it("Verify Logout", () => {
    loginPage.quickLogout();
  });
});
