describe('Grant Manager Login and Logout', () => {

    it('Verify Default Grant Program tenant is selected.', () => {

        // Always start from the base URL
        cy.visit(Cypress.env('webapp.url'))

        // Determine authentication state from UI
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

        // Verify we landed in the authenticated app
        cy.location('pathname', { timeout: 30000 })
            .should('include', '/GrantApplications')

        // Verify Default Grant Program tenant is selected
        cy.get('.unity-user-initials').should('exist').click()
        cy.get('#user-dropdown .btn-dropdown span')
            .should('contain', 'Default Grants Program')

        // Logout (terminal action)
        cy.logout()
    })
})
