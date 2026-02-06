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

  it("Verify Login", () => {
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
            } else {
              cy.log("Already authenticated");
            }
          });
        });

        return;
      }

      throw new Error("Unable to determine authentication state");
    });

    cy.location("pathname", { timeout: 30000 }).should(
      "include",
      "/GrantApplications",
    );
  });

  it("Switch to Default Grants Program if available", () => {
    navigationPage.switchToTenantIfAvailable("Default Grants Program");
  });

  it("Handle IDIR if required", () => {
    cy.get("body").then(($body) => {
      if ($body.find("#social-idir").length > 0) {
        cy.get("#social-idir").click();
      }
    });

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
