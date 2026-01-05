import { PageFactory } from '../utilities/PageFactory'

describe('Grant Manager Login and List Navigation', () => {

    it('Login', () => {
        const loginPage = PageFactory.getLoginPage()
        loginPage.login()
    })

    it('Switch to Default Grants Program if available', () => {
        const navPage = PageFactory.getNavigationPage()
        navPage.switchToTenantIfAvailable('Default Grants Program')
    })

    it('Handle IDIR if required', () => {
        cy.get('body').then(($body) => {
            if ($body.find('#social-idir').length > 0) {
                cy.get('#social-idir').should('be.visible').click()
            }
        })

        cy.location('pathname', { timeout: 30000 }).should('include', '/GrantApplications')
    })

    // Verify all lists are populated
    it('Verify Applications, Roles, Users, Intakes, Forms, Dashboard lists are populated', () => {
        const navPage = PageFactory.getNavigationPage()
        const applicationsPage = PageFactory.getApplicationsPage()
        const rolesPage = PageFactory.getRolesPage()
        const usersPage = PageFactory.getUsersPage()
        const intakesPage = PageFactory.getIntakesPage()
        const formsPage = PageFactory.getFormsPage()
        const dashboardPage = PageFactory.getDashboardPage()
        
        // Verify Default Grant Program tenant is selected
        navPage.clickUserMenu()
        navPage.verifyCurrentTenant('Default Grants Program')
        
        // Applications
        navPage.goToApplications()
        cy.wait(1000)
        applicationsPage.verifyListHasData()
        
        // Roles
        navPage.goToRoles()
        cy.wait(1000)
        rolesPage.verifyListHasData()
        
        // Users
        navPage.goToUsers()
        cy.wait(1000)
        usersPage.verifyListHasData()
        
        // Intakes
        navPage.goToIntakes()
        cy.wait(1000)
        intakesPage.verifyListHasData()
        
        // Forms
        navPage.goToForms()
        cy.wait(1000)
        formsPage.verifyListHasData()
        
        // Dashboard
        navPage.goToDashboard()
        cy.wait(1000)

        // Switch Intake to "Test" and verify charts have data
        dashboardPage.setIntakeIfAvailable('Test')
        cy.wait(1000) // Give charts a moment to refresh after changing intake
        
        // Verify all charts have data
        dashboardPage.verifyAllChartsHaveData()
        
        // Return to top
        cy.visit(Cypress.env('webapp.url'))
    })
    
    it('Verify Logout', () => {
        const loginPage = PageFactory.getLoginPage()
        loginPage.logout()
    })
})
