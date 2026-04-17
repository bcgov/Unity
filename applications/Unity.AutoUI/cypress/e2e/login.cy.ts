import { LoginPageInstance, NavigationPageInstance } from "../utilities";

describe('Grant Manager Login and Logout', () => {
    const loginPage = LoginPageInstance()
    const navPage = NavigationPageInstance()

    it('Verify Default Grant Program tenant is selected.', () => {
        loginPage.login()
        loginPage.verifyOnGrantApplications()

        // Verify Default Grant Program tenant is selected
        navPage.verifyCurrentTenant('Default Grants Program')

        // Logout (terminal action)
        loginPage.quickLogout()
    })
})
