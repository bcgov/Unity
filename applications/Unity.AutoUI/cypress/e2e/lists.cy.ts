import {
    ApplicationsPageInstance,
    LoginPageInstance,
    NavigationPageInstance,
} from '../utilities'

describe('Grant Manager Login and List Navigation', () => {
    const loginPage = LoginPageInstance()
    const navPage = NavigationPageInstance()
    const appsPage = ApplicationsPageInstance()

    function setDashboardIntakeToTestIfAvailable() {
        const btnSel = 'button[data-id="dashboardIntakeId"]'
        const listboxSel = '#bs-select-1[role="listbox"]'
        const searchSel = 'input[type="search"][aria-controls="bs-select-1"]'

        cy.get(btnSel, { timeout: 30000 })
            .should('be.visible')
            .first()
            .click({ force: true })

        cy.get(listboxSel, { timeout: 30000 }).should('be.visible')

        cy.get(searchSel, { timeout: 30000 })
            .should('be.visible')
            .clear()
            .type('Test')

        cy.contains(`${listboxSel} a.dropdown-item[role="option"] span.text`, /^Test$/, { timeout: 30000 })
            .closest('a.dropdown-item')
            .then(($opt) => {
                const selected =
                    $opt.attr('aria-selected') === 'true' ||
                    $opt.hasClass('selected')

                if (!selected) {
                    cy.wrap($opt).scrollIntoView().click({ force: true })
                }
            })

        cy.get('select#dashboardIntakeId option:selected').should(($opts) => {
            const texts = Array.from($opts, (opt) => (opt.textContent || '').trim())
            expect(texts).to.include('Test')
        })

        cy.get(btnSel).first().click({ force: true })
        cy.get(btnSel).first().should('have.attr', 'aria-expanded', 'false')
    }

    it('Verify Login', () => {
        loginPage.login()
        loginPage.verifyOnGrantApplications()
    })

    it('Switch to Default Grants Program if available', () => {
        navPage.switchToDefaultGrantsProgramIfAvailable()
    })

    it('Handle IDIR if required', () => {
        cy.get('body').then(($body) => {
            if ($body.find('#social-idir').length > 0) {
                cy.get('#social-idir').click()
            }
        })

        cy.location('pathname', { timeout: 30000 }).should('include', '/GrantApplications')
    })

    it('Verify Applications, Roles, Users, Intakes, Forms, Dashboard lists are populated', () => {

        navPage.verifyCurrentTenant('Default Grants Program')

        navPage.goToApplications()
        appsPage.verifyListHasData()

        navPage.goToRoles()
        cy.get('tbody tr').should('have.length.at.least', 1)

        navPage.goToUsers()
        cy.get('tbody tr').should('have.length.at.least', 1)

        navPage.goToIntakes()
        cy.get('tbody tr').should('have.length.at.least', 1)

        navPage.goToForms()
        cy.get('tbody tr').should('have.length.at.least', 1)

        navPage.goToDashboard()
        cy.location('pathname', { timeout: 30000 }).should('include', '/Dashboard')
        setDashboardIntakeToTestIfAvailable()

        cy.get('#applicationStatusChart text', { timeout: 30000 })
            .first()
            .should(($el) => {
                expect(parseInt($el.text(), 10)).to.be.gt(0)
            })

        cy.visit(Cypress.env('webapp.url'))
    })

    it('Verify Logout', () => {
        loginPage.quickLogout()
    })
})
