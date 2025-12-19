describe('Grant Manager Login and List Navigation', () => {

    function switchToDefaultGrantsProgramIfAvailable() {
        cy.get('body').then(($body) => {
            // If we are already on GrantPrograms (or can navigate there), try. Otherwise skip quietly.
            // Key point: never .should() an optional element.
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
                    cy.log('Skipping tenant switch: "Switch Grant Programs" not present for this user/session')
                    // Close dropdown so it does not block clicks later
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
                    expect(p.indexOf('/GrantApplications') >= 0 || p.indexOf('/auth/') >= 0).to.eq(true)
                })
            })
        })
    }

    function setDashboardIntakeToTestIfAvailable() {
        // Robust intake switcher for Dashboard (TEST / UAT / PROD compatible)
        // The INTAKES bootstrap-select can be multi-select, so the button label may stay "INTAKES".
        // We verify selection via option state (aria-selected / selected class), not button text.

        const btnSel = 'button[data-id="dashboardIntakeId"]'
        const listboxSel = '#bs-select-1[role="listbox"]'
        const searchSel = 'input[type="search"][aria-controls="bs-select-1"][aria-label="Search"]'

        cy.get('body').then(($body) => {
            if ($body.find(btnSel).length === 0) {
                cy.log('Skipping intake switch: dashboard intake selector not found')
                return
            }

            cy.get(btnSel).first().click({ force: true })

            cy.get(listboxSel, { timeout: 20000 }).should('exist')

            // Search is optional across environments
            cy.get('body').then(($b2) => {
                if ($b2.find(searchSel).length > 0) {
                    cy.get(searchSel).should('be.visible').clear().type('Test')
                }
            })

            cy.get(listboxSel).within(() => {
                cy.get('a.dropdown-item[role="option"]').then(($opts) => {
                    const match = $opts.filter((_, el) => {
                        const textSpan = el.querySelector('span.text')
                        const label = (textSpan ? textSpan.textContent : el.textContent) || ''
                        return label.trim() === 'Test'
                    })

                    if (match.length === 0) {
                        cy.log('Skipping intake switch: "Test" option not found')
                        return
                    }

                    const domEl = match.get(0) as HTMLElement

                    const ariaSelected =
                        (domEl.getAttribute('aria-selected') || '').trim() === 'true'

                    const classSelected =
                        domEl.classList.contains('selected') ||
                        (domEl.closest('li')?.classList.contains('selected') ?? false)

                    if (!ariaSelected && !classSelected) {
                        cy.wrap(domEl).scrollIntoView().click({ force: true })
                    }
                })
            })

            // Verify selection state inside the listbox
            cy.get(listboxSel).within(() => {
                cy.get('a.dropdown-item[role="option"]').then(($opts2) => {
                    const match2 = $opts2.filter((_, el) => {
                        const textSpan = el.querySelector('span.text')
                        const label = (textSpan ? textSpan.textContent : el.textContent) || ''
                        return label.trim() === 'Test'
                    })

                    expect(match2.length).to.be.greaterThan(0)

                    const domEl2 = match2.get(0) as HTMLElement

                    const ariaSelected2 =
                        (domEl2.getAttribute('aria-selected') || '').trim() === 'true'

                    const classSelected2 =
                        domEl2.classList.contains('selected') ||
                        (domEl2.closest('li')?.classList.contains('selected') ?? false)

                    expect(ariaSelected2 || classSelected2).to.eq(true)
                })
            })

            // Close dropdown so it does not block chart interaction
            cy.get('body').click(0, 0)
        })
    }

    it('Login', () => {
        cy.login()
    })

    it('Switch to Default Grants Program if available', () => {
        switchToDefaultGrantsProgramIfAvailable()
    })

    it('Handle IDIR if required', () => {
        cy.get('body').then(($body) => {
            if ($body.find('#social-idir').length > 0) {
                cy.get('#social-idir').should('be.visible').click()
            }
        })

        cy.location('pathname', { timeout: 30000 }).should('include', '/GrantApplications')
    })

    // 12.) Ensure all of the lists are populated.
    it('Verify Applications, Roles, Users, Intakes, Forms, Dashboard lists are populated', () => {
        // Verify Default Grant Program tenant is selected.
        cy.get('.unity-user-initials').should('exist').click()
        cy.get('#user-dropdown .btn-dropdown span').should('contain', 'Default Grants Program')
        // 13.) Applications
        cy.contains("Applications").click()
        cy.wait(1000)
        cy.get('tbody tr').should('have.length.at.least', 1) //the applications list should have at least one row. i.e. it shouldn't be blank.	
        // 14.) Roles
        cy.contains("Roles").click()
        cy.wait(1000)
        cy.get('tbody tr').should('have.length.at.least', 1) //the Roles list should have at least one row. i.e. it shouldn't be blank.
        // 15.) Users
        cy.contains("Users").click()
        cy.wait(1000)
        cy.get('tbody tr').should('have.length.at.least', 1) //the Users list should have at least one row. i.e. it shouldn't be blank.
        // 16.) Intakes
        cy.contains("Intakes").click()
        cy.wait(1000)
        cy.get('tbody tr').should('have.length.at.least', 1) //the Intakes list should have at least one row. i.e. it shouldn't be blank.
        // 17.) Forms
        cy.contains("Forms").click()
        cy.wait(1000)
        cy.get('tbody tr').should('have.length.at.least', 1) //the Forms list should have at least one row. i.e. it shouldn't be blank.
        // 18.) Dashboard
        cy.contains("Dashboard").click()
        cy.wait(1000)

        //Switch Intake to "Test"
        setDashboardIntakeToTestIfAvailable()
        cy.wait(1000) // New: give charts a moment to refresh after changing intake

        cy.get('#applicationStatusChart > div > svg > g > text:nth-child(1)')
            .invoke('text')
            .then((text) => {
                const number = parseInt(text, 10)
                expect(number).to.be.gt(0)
            })
        cy.get('#economicRegionChart > div > svg > g > text:nth-child(1)')
            .invoke('text')
            .then((text) => {
                const number = parseInt(text, 10)
                expect(number).to.be.gt(0)
            })
        cy.get('#applicationAssigneeChart > div > svg > g > text:nth-child(1)')
            .invoke('text')
            .then((text) => {
                const number = parseInt(text, 10)
                expect(number).to.be.gt(0)
            })
        cy.get('#subsectorRequestedAmountChart > div > svg > g > text:nth-child(1)')
            .invoke('text') //gets the text of the element, which should be a string representation of a dollar amount
            .then((text) => {
                const amount = parseFloat(text.replace('$', '')) //the dollar sign is removed from the text and the result is converted to a number
                expect(amount).to.be.gt(0)
            }) //the amount is expected to be greater than zero
        // Return to top
        cy.visit(Cypress.env('webapp.url'))
    })
    it('Verify Logout', () => {
        cy.logout()
    })
})
