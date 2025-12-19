describe('Send an email', () => {
    const TEST_EMAIL_TO = 'grantmanagementsupport@gov.bc.ca'
    const TEST_EMAIL_CC = 'UnitySupport@gov.bc.ca'
    const TEST_EMAIL_BCC = 'UNITYSUP@Victoria1.gov.bc.ca'
    const TEMPLATE_NAME = 'Test Case 1'

    const now = new Date()
    const timestamp =
        now.getFullYear() +
        '-' +
        String(now.getMonth() + 1).padStart(2, '0') +
        '-' +
        String(now.getDate()).padStart(2, '0') +
        ' ' +
        String(now.getHours()).padStart(2, '0') +
        ':' +
        String(now.getMinutes()).padStart(2, '0') +
        ':' +
        String(now.getSeconds()).padStart(2, '0')

    const TEST_EMAIL_SUBJECT = `Smoke Test Email ${timestamp}`

    //it('Verify Login', () => {
    //    cy.login()
    //})

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

    it('Open an application from the list', () => {
        cy.url().should('include', '/GrantApplications')

        cy.get('#GrantApplicationsTable tbody a[href^="/GrantApplications/Details?ApplicationId="]', { timeout: 20000 })
            .should('have.length.greaterThan', 0)

        cy.get('#GrantApplicationsTable tbody a[href^="/GrantApplications/Details?ApplicationId="]')
            .first()
            .click()

        cy.url().should('include', '/GrantApplications/Details')
    })

    it('Open Emails tab', () => {
        cy.get('#emails-tab', { timeout: 20000 })
            .should('exist')
            .should('be.visible')
            .click()

        cy.contains('Emails', { timeout: 20000 }).should('exist')
        cy.contains('Email History', { timeout: 20000 }).should('exist')
    })

    it('Open New Email form', () => {
        cy.get('#btn-new-email', { timeout: 20000 })
            .should('exist')
            .should('be.visible')
            .click()

        cy.contains('Email To', { timeout: 20000 }).should('exist')
    })

    it('Select Email Template', () => {
        cy.intercept('GET', '/api/app/template/*/template-by-id').as('loadTemplate')

        cy.get('#template', { timeout: 20000 })
            .should('exist')
            .should('be.visible')
            .select(TEMPLATE_NAME)

        cy.wait('@loadTemplate', { timeout: 20000 })

        cy.get('#template')
            .find('option:selected')
            .should('have.text', TEMPLATE_NAME)
    })

    it('Set Email To address', () => {
        cy.get('#EmailTo', { timeout: 20000 })
            .should('exist')
            .should('be.visible')
            .clear()
            .type(TEST_EMAIL_TO)

        cy.get('#EmailTo').should('have.value', TEST_EMAIL_TO)
    })

    it('Set Email CC address', () => {
        cy.get('#EmailCC', { timeout: 20000 })
            .should('exist')
            .should('be.visible')
            .clear()
            .type(TEST_EMAIL_CC)

        cy.get('#EmailCC').should('have.value', TEST_EMAIL_CC)
    })

    it('Set Email BCC address', () => {
        cy.get('#EmailBCC', { timeout: 20000 })
            .should('exist')
            .should('be.visible')
            .clear()
            .type(TEST_EMAIL_BCC)

        cy.get('#EmailBCC').should('have.value', TEST_EMAIL_BCC)
    })

    it('Set Email Subject', () => {
        cy.get('#EmailSubject', { timeout: 20000 })
            .should('exist')
            .should('be.visible')
            .clear()
            .type(TEST_EMAIL_SUBJECT)

        cy.get('#EmailSubject').should('have.value', TEST_EMAIL_SUBJECT)
    })

    it('Save the email', () => {
        cy.get('#btn-save', { timeout: 20000 })
            .should('exist')
            .should('be.visible')
            .click()

        cy.get('#btn-new-email', { timeout: 20000 }).should('be.visible')
    })

    it('Select saved email from Email History', () => {
        cy.contains('td.data-table-header', TEST_EMAIL_SUBJECT, { timeout: 20000 })
            .should('exist')
            .click()

        cy.get('#EmailTo', { timeout: 20000 }).should('be.visible')
        cy.get('#EmailCC').should('be.visible')
        cy.get('#EmailBCC').should('be.visible')
        cy.get('#EmailSubject').should('be.visible')

        cy.get('#btn-send').should('be.visible')
        cy.get('#btn-save').should('be.visible')
    })

    it('Send the email', () => {
        cy.get('#btn-send', { timeout: 20000 })
            .should('exist')
            .should('be.visible')
            .click()
    })

    it('Confirm send email in modal', () => {
        cy.get('#modal-content', { timeout: 20000 })
            .should('exist')
            .should('be.visible')

        cy.contains('Are you sure you want to send this email?', { timeout: 20000 })
            .should('exist')

        cy.get('#btn-confirm-send', { timeout: 20000 })
            .should('exist')
            .should('be.visible')
            .click()
    })

    it('Verify Logout', () => {
        cy.logout()
    })
})
