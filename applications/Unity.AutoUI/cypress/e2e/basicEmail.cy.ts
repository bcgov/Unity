// cypress/e2e/basicEmail.cy.ts

describe('Send an email', () => {
    const TEST_EMAIL_TO = Cypress.env('TEST_EMAIL_TO') as string
    const TEST_EMAIL_CC = Cypress.env('TEST_EMAIL_CC') as string
    const TEST_EMAIL_BCC = Cypress.env('TEST_EMAIL_BCC') as string
    const TEMPLATE_NAME = 'Test Case 1'
    const STANDARD_TIMEOUT = 20000

    // Only suppress the noisy ResizeObserver error that Unity throws in TEST.
    // Everything else should still fail the test.
    Cypress.on('uncaught:exception', (err) => {
        const msg = err && err.message ? err.message : ''
        if (msg.indexOf('ResizeObserver loop limit exceeded') >= 0) {
            return false
        }
        return true
    })

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
                    cy.log('Skipping tenant switch: "Switch Grant Programs" not present for this user/session')
                    cy.get('body').click(0, 0)
                    return
                }

                cy.wrap(switchLink.first()).click()

                cy.url({ timeout: STANDARD_TIMEOUT }).should('include', '/GrantPrograms')

                cy.get('#search-grant-programs', { timeout: STANDARD_TIMEOUT })
                    .should('be.visible')
                    .clear()
                    .type('Default Grants Program')

                cy.get('#UserGrantProgramsTable', { timeout: STANDARD_TIMEOUT })
                    .should('be.visible')
                    .within(() => {
                        cy.contains('tbody tr', 'Default Grants Program', { timeout: STANDARD_TIMEOUT })
                            .should('exist')
                            .within(() => {
                                cy.contains('button', 'Select')
                                    .should('be.enabled')
                                    .click()
                            })
                    })

                cy.location('pathname', { timeout: STANDARD_TIMEOUT }).should((p) => {
                    expect(p.indexOf('/GrantApplications') >= 0 || p.indexOf('/auth/') >= 0).to.eq(true)
                })
            })
        })
    }

    function openSavedEmailFromHistoryBySubject(subject: string) {
        cy.get('body', { timeout: STANDARD_TIMEOUT }).then(($body) => {
            const historyTableById = $body.find('#EmailHistoryTable')
            if (historyTableById.length > 0) {
                cy.get('#EmailHistoryTable', { timeout: STANDARD_TIMEOUT })
                    .should('be.visible')
                    .within(() => {
                        cy.contains('td', subject, { timeout: STANDARD_TIMEOUT })
                            .should('exist')
                            .click()
                    })
                return
            }

            // Fallback: find the subject anywhere in a TD (scoped to avoid brittle class names)
            cy.contains('td', subject, { timeout: STANDARD_TIMEOUT })
                .should('exist')
                .click()
        })
    }

    function confirmSendDialogIfPresent() {
        // Wait until either a bootstrap modal is shown, or SweetAlert container appears, or confirm button exists.
        cy.get('body', { timeout: STANDARD_TIMEOUT }).should(($b) => {
            const hasBootstrapShownModal = $b.find('.modal.show').length > 0
            const hasSwal = $b.find('.swal2-container').length > 0
            const hasConfirmBtn = $b.find('#btn-confirm-send').length > 0
            expect(hasBootstrapShownModal || hasSwal || hasConfirmBtn).to.eq(true)
        })

        cy.get('body', { timeout: STANDARD_TIMEOUT }).then(($b) => {
            const hasSwal = $b.find('.swal2-container').length > 0
            if (hasSwal) {
                // SweetAlert2 style
                cy.get('.swal2-container', { timeout: STANDARD_TIMEOUT }).should('be.visible')
                cy.contains('.swal2-container', 'Are you sure', { timeout: STANDARD_TIMEOUT }).should('exist')

                // Typical confirm button class, with fallback to text match
                if ($b.find('.swal2-confirm').length > 0) {
                    cy.get('.swal2-confirm', { timeout: STANDARD_TIMEOUT }).should('be.visible').click()
                } else {
                    cy.contains('.swal2-container button', 'Yes', { timeout: STANDARD_TIMEOUT }).click()
                }
                return
            }

            const hasBootstrapShownModal = $b.find('.modal.show').length > 0
            if (hasBootstrapShownModal) {
                // Bootstrap modal: assert the shown modal, not the inner content div
                cy.get('.modal.show', { timeout: STANDARD_TIMEOUT })
                    .should('be.visible')
                    .within(() => {
                        cy.contains('Are you sure you want to send this email?', { timeout: STANDARD_TIMEOUT })
                            .should('exist')

                        // Prefer the known id if present, otherwise click a button with expected intent text
                        if (Cypress.$('#btn-confirm-send').length > 0) {
                            cy.get('#btn-confirm-send', { timeout: STANDARD_TIMEOUT })
                                .should('exist')
                                .should('be.visible')
                                .click()
                        } else {
                            cy.contains('button', 'Confirm', { timeout: STANDARD_TIMEOUT }).click()
                        }
                    })
                return
            }

            // Last resort: confirm button exists but modal might not be "visible" by Cypress standards
            cy.get('#btn-confirm-send', { timeout: STANDARD_TIMEOUT })
                .should('exist')
                .click({ force: true })
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

        cy.get('#GrantApplicationsTable tbody a[href^="/GrantApplications/Details?ApplicationId="]', { timeout: STANDARD_TIMEOUT })
            .should('have.length.greaterThan', 0)

        cy.get('#GrantApplicationsTable tbody a[href^="/GrantApplications/Details?ApplicationId="]')
            .first()
            .click()

        cy.url().should('include', '/GrantApplications/Details')
    })

    it('Open Emails tab', () => {
        cy.get('#emails-tab', { timeout: STANDARD_TIMEOUT })
            .should('exist')
            .should('be.visible')
            .click()

        cy.contains('Emails', { timeout: STANDARD_TIMEOUT }).should('exist')
        cy.contains('Email History', { timeout: STANDARD_TIMEOUT }).should('exist')
    })

    it('Open New Email form', () => {
        cy.get('#btn-new-email', { timeout: STANDARD_TIMEOUT })
            .should('exist')
            .should('be.visible')
            .click()

        cy.contains('Email To', { timeout: STANDARD_TIMEOUT }).should('exist')
    })

    it('Select Email Template', () => {
        cy.intercept('GET', '/api/app/template/*/template-by-id').as('loadTemplate')

        cy.get('#template', { timeout: STANDARD_TIMEOUT })
            .should('exist')
            .should('be.visible')
            .select(TEMPLATE_NAME)

        cy.wait('@loadTemplate', { timeout: STANDARD_TIMEOUT })

        cy.get('#template')
            .find('option:selected')
            .should('have.text', TEMPLATE_NAME)
    })

    it('Set Email To address', () => {
        cy.get('#EmailTo', { timeout: STANDARD_TIMEOUT })
            .should('exist')
            .should('be.visible')
            .clear()
            .type(TEST_EMAIL_TO)

        cy.get('#EmailTo').should('have.value', TEST_EMAIL_TO)
    })

    it('Set Email CC address', () => {
        cy.get('#EmailCC', { timeout: STANDARD_TIMEOUT })
            .should('exist')
            .should('be.visible')
            .clear()
            .type(TEST_EMAIL_CC)

        cy.get('#EmailCC').should('have.value', TEST_EMAIL_CC)
    })

    it('Set Email BCC address', () => {
        cy.get('#EmailBCC', { timeout: STANDARD_TIMEOUT })
            .should('exist')
            .should('be.visible')
            .clear()
            .type(TEST_EMAIL_BCC)

        cy.get('#EmailBCC').should('have.value', TEST_EMAIL_BCC)
    })

    it('Set Email Subject', () => {
        cy.get('#EmailSubject', { timeout: STANDARD_TIMEOUT })
            .should('exist')
            .should('be.visible')
            .clear()
            .type(TEST_EMAIL_SUBJECT)

        cy.get('#EmailSubject').should('have.value', TEST_EMAIL_SUBJECT)
    })

    it('Save the email', () => {
        cy.get('#btn-save', { timeout: STANDARD_TIMEOUT })
            .should('exist')
            .should('be.visible')
            .click()

        cy.get('#btn-new-email', { timeout: STANDARD_TIMEOUT }).should('be.visible')
    })

    it('Select saved email from Email History', () => {
        openSavedEmailFromHistoryBySubject(TEST_EMAIL_SUBJECT)

        cy.get('#EmailTo', { timeout: STANDARD_TIMEOUT }).should('be.visible')
        cy.get('#EmailCC').should('be.visible')
        cy.get('#EmailBCC').should('be.visible')
        cy.get('#EmailSubject').should('be.visible')

        cy.get('#btn-send', { timeout: STANDARD_TIMEOUT }).should('exist')
        cy.get('#btn-save', { timeout: STANDARD_TIMEOUT }).should('exist')
    })

    it('Send the email', () => {
        cy.get('#btn-send', { timeout: STANDARD_TIMEOUT })
            .should('exist')
            .should('be.visible')
            .should('not.be.disabled')
            .click()
    })

    it('Confirm send email in dialog', () => {
        confirmSendDialogIfPresent()
    })

    it('Verify Logout', () => {
        cy.logout()
    })
})
