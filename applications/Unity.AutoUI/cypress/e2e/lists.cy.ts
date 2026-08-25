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
        // The INTAKES filter is a Select2 (bootstrap-5 theme) multi-select, not
        // bootstrap-select — the toggle is the [role="combobox"] wrapping the
        // rendered-choices <ul id="select2-dashboardIntakeId-container">.
        const renderedSel = '#select2-dashboardIntakeId-container'
        const dropdownSel = '.select2-dropdown'
        const optionSel = `${dropdownSel} li.select2-results__option`

        cy.get(renderedSel, { timeout: 30000 })
            .should('be.visible')
            .closest('[role="combobox"]')
            .as('intakeCombobox')
            .click({ force: true })

        cy.get(dropdownSel, { timeout: 30000 }).should('be.visible')

        cy.get(renderedSel)
            .parent()
            .find('textarea.select2-search__field', { timeout: 30000 })
            .should('be.visible')
            .clear()
            .type('Test')

        cy.contains(optionSel, /^Test$/, { timeout: 30000 })
            .then(($opt) => {
                const selected = $opt.attr('aria-selected') === 'true'

                if (!selected) {
                    cy.wrap($opt).click({ force: true })
                }
            })

        cy.get('select#dashboardIntakeId option:selected').should(($opts) => {
            const texts = Array.from($opts, (opt) => (opt.textContent || '').trim())
            expect(texts).to.include('Test')
        })

        cy.get('@intakeCombobox').click({ force: true })
        cy.get('@intakeCombobox').should('have.attr', 'aria-expanded', 'false')
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
                expect(Number.parseInt($el.text(), 10)).to.be.gt(0)
            })

        cy.visit(Cypress.env('webapp.url'))
    })

    it('Verify Logout', () => {
        loginPage.quickLogout()
    })
})
