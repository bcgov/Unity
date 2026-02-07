import { loginIfNeeded } from "../support/auth";
import { PageFactory } from "../utilities/PageFactory";

describe("Grant Manager Login and Top Navigation", () => {
  const loginPage = PageFactory.getLoginPage();
  const navigationPage = PageFactory.getNavigationPage();

  before(() => {
    loginIfNeeded();
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
