import { LoginPageInstance, NavigationPageInstance } from "../utilities";

describe('Grant Manager Login and Top Navigation', () => {
    const loginPage = LoginPageInstance()
    const navPage = NavigationPageInstance()

    it('Verify Login', () => {
        loginPage.login()
        loginPage.verifyOnGrantApplications()
    })

    it('Verify navigation options in the top banner', () => {

        // 3.) Verify Default Grant Program tenant is selected.
        navPage.verifyCurrentTenant('Default Grants Program')

        // 4.) Ensure all expected headings are present.
        navPage.verifyAllNavItemsExist()

        // 5.) Applications
        navPage.goToApplications()

        // 6.) Roles
        navPage.goToRoles()

        // 7.) Users
        navPage.goToUsers()

        // 8.) Intakes
        navPage.goToIntakes()

        // 9.) Forms
        navPage.goToForms()

        // 10.) Dashboard
        navPage.goToDashboard()

        // 11.) Payments
        navPage.goToPayments()

        // Return to top
        cy.visit(Cypress.env('webapp.url'))
    })

    it('Verify Logout', () => {
        loginPage.quickLogout()
    })
})
