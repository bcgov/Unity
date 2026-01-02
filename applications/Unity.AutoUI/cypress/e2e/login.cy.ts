import { PageFactory } from "../utilities/PageFactory";

describe("Grant Manager Login and Logout", () => {
  it("Verify Default Grant Program tenant is selected.", () => {
    const loginPage = PageFactory.getLoginPage();
    const navPage = PageFactory.getNavigationPage();

    // Login
    loginPage.login();

    // Verify user menu is visible
    navPage.clickUserMenu();

    // Logout
    loginPage.logout();
  });
});
