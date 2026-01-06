describe('Grant Manager Login and Top Navigation', () => {

    it('Verify Login', () => {

        // Always start from the base URL
        cy.visit(Cypress.env('webapp.url'))

        cy.get('body').then(($body) => {

            // Already authenticated
            if ($body.find('button:contains("VIEW APPLICATIONS")').length > 0) {
                cy.contains('VIEW APPLICATIONS').click()
                return
            }

            // Not authenticated
            if ($body.find('button:contains("LOGIN")').length > 0) {
                cy.contains('LOGIN').click()

                cy.get('body').then(($loginBody) => {

                    // IDIR chooser may or may not appear
                    if ($loginBody.find(':contains("IDIR")').length > 0) {
                        cy.contains('IDIR').click()
                    }

                    cy.get('body').then(($authBody) => {
                        if ($authBody.find('#user').length > 0) {
                            cy.get('#user').type(Cypress.env('test1username'))
                            cy.get('#password').type(Cypress.env('test1password'))
                            cy.contains('Continue').click()
                        }
                    })
                })

                return
            }

            throw new Error('Unable to determine authentication state')
        })

        cy.location('pathname', { timeout: 30000 })
            .should('include', '/GrantApplications')
    })

    it('Verify navigation options in the top banner', () => {

        // 3.) Verify Default Grant Program tenant is selected.
        cy.get('.unity-user-initials').should('exist').click()
        cy.get('#user-dropdown .btn-dropdown span')
            .should('contain', 'Default Grants Program')

        // 4.) Ensure all expected headings are present.

        // 5.) Applications
        cy.contains('Applications').should('exist').click()

        // 6.) Roles
        cy.contains('Roles').should('exist').click()

        // 7.) Users
        cy.contains('Users').should('exist').click()

        // 8.) Intakes
        cy.contains('Intakes').should('exist').click()

        // 9.) Forms
        cy.contains('Forms').should('exist').click()

        // 10.) Dashboard
        cy.contains('Dashboard').should('exist').click()

        // 11.) Payments
        cy.contains('Payments').should('exist').click()

        // Return to top
        cy.visit(Cypress.env('webapp.url'))
    })

    it('Verify Logout', () => {
        cy.logout()
    })
})
