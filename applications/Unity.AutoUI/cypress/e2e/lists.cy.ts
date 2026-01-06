describe('Grant Manager Login and List Navigation', () => {

    function switchToDefaultGrantsProgramIfAvailable() {
        cy.get('body').then(($body) => {
            const hasUserInitials = $body.find('.unity-user-initials').length > 0

            if (!hasUserInitials) {
                cy.log('Skipping tenant switch: no user initials menu found')
                return
            }

            cy.get('.unity-user-initials').click()

            cy.get('body').then(($body2) => {
                const switchLink = $body2.find('#user-dropdown a.dropdown-item').filter((_, el) => {
                    return (el.textContent || '').trim() === 'Switch Grant Programs'
                })

                if (switchLink.length === 0) {
                    cy.log('Skipping tenant switch: "Switch Grant Programs" not present')
                    cy.get('body').click(0, 0)
                    return
                }

                cy.wrap(switchLink.first()).click()
                cy.url({ timeout: 20000 }).should('include', '/GrantPrograms')

                cy.get('#search-grant-programs', { timeout: 20000 })
                    .should('be.visible')
                    .clear()
                    .type('Default Grants Program')

                cy.get('#UserGrantProgramsTable', { timeout: 20000 })
                    .should('be.visible')
                    .within(() => {
                        cy.contains('tbody tr', 'Default Grants Program', { timeout: 20000 })
                            .should('exist')
                            .within(() => {
                                cy.contains('button', 'Select')
                                    .should('be.enabled')
                                    .click()
                            })
                    })

                cy.location('pathname', { timeout: 20000 }).should((p) => {
                    expect(
                        p.indexOf('/GrantApplications') >= 0 ||
                        p.indexOf('/auth/') >= 0
                    ).to.eq(true)
                })
            })
        })
    }

    function setDashboardIntakeToTestIfAvailable() {
        const btnSel = 'button[data-id="dashboardIntakeId"]'
        const listboxSel = '#bs-select-1[role="listbox"]'
        const searchSel = 'input[type="search"][aria-controls="bs-select-1"]'

        cy.get('body').then(($body) => {
            if ($body.find(btnSel).length === 0) {
                cy.log('Skipping intake switch: selector not found')
                return
            }

            cy.get(btnSel).first().click({ force: true })
            cy.get(listboxSel, { timeout: 20000 }).should('exist')

            cy.get('body').then(($b2) => {
                if ($b2.find(searchSel).length > 0) {
                    cy.get(searchSel).clear().type('Test')
                }
            })

            cy.get(listboxSel).within(() => {
                cy.get('a.dropdown-item[role="option"]').then(($opts) => {
                    const match = $opts.filter((_, el) => {
                        const text = el.querySelector('span.text')?.textContent || ''
                        return text.trim() === 'Test'
                    })

                    if (match.length === 0) {
                        cy.log('Skipping intake switch: Test not found')
                        return
                    }

                    const el = match.get(0)
                    const selected =
                        el.getAttribute('aria-selected') === 'true' ||
                        el.classList.contains('selected')

                    if (!selected) {
                        cy.wrap(el).scrollIntoView().click({ force: true })
                    }
                })
            })

            cy.get('body').click(0, 0)
        })
    }

    it('Verify Login', () => {

        cy.visit(Cypress.env('webapp.url'))

        cy.get('body').then(($body) => {

            if ($body.find('button:contains("VIEW APPLICATIONS")').length > 0) {
                cy.contains('VIEW APPLICATIONS').click()
                return
            }

            if ($body.find('button:contains("LOGIN")').length > 0) {
                cy.contains('LOGIN').click()

                cy.get('body').then(($loginBody) => {
                    if ($loginBody.find(':contains("IDIR")').length > 0) {
                        cy.contains('IDIR').click()
                    }

                    cy.get('body').then(($authBody) => {
                        if ($authBody.find('#user').length > 0) {
                            cy.get('#user').type(Cypress.env('test1username'))
                            cy.get('#password').type(Cypress.env('test1password'))
                            cy.contains('Continue').click()
                        } else {
                            cy.log('Already authenticated')
                        }
                    })
                })

                return
            }

            throw new Error('Unable to determine authentication state')
        })

        cy.location('pathname', { timeout: 30000 }).should('include', '/GrantApplications')
    })

    it('Switch to Default Grants Program if available', () => {
        switchToDefaultGrantsProgramIfAvailable()
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

        cy.get('.unity-user-initials').should('exist').click()
        cy.get('#user-dropdown .btn-dropdown span')
            .should('contain', 'Default Grants Program')

        cy.contains("Applications").click()
        cy.get('tbody tr').should('have.length.at.least', 1)

        cy.contains("Roles").click()
        cy.get('tbody tr').should('have.length.at.least', 1)

        cy.contains("Users").click()
        cy.get('tbody tr').should('have.length.at.least', 1)

        cy.contains("Intakes").click()
        cy.get('tbody tr').should('have.length.at.least', 1)

        cy.contains("Forms").click()
        cy.get('tbody tr').should('have.length.at.least', 1)

        cy.contains("Dashboard").click()
        setDashboardIntakeToTestIfAvailable()

        cy.get('#applicationStatusChart text')
            .first()
            .invoke('text')
            .then(n => expect(parseInt(n, 10)).to.be.gt(0))

        cy.visit(Cypress.env('webapp.url'))
    })

    it('Verify Logout', () => {
        cy.logout()
    })
})
