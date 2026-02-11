/// <reference types="cypress" />

// cypress/e2e/chefsdata.cy.ts

describe('Unity Login and check data from CHEFS', () => {
    const STANDARD_TIMEOUT = 20000

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


    // TEST renders the Submission tab inside an open shadow root (Form.io).
    // Enabling this makes cy.get / cy.contains pierce shadow DOM consistently across envs.
    before(() => {
        Cypress.config('includeShadowDom', true)
    })

    it('Verify Login', () => {
        // 1.) Always start from the base URL
        cy.visit(Cypress.env('webapp.url'))

        // 2.) Decide auth path based on visible UI
        cy.get('body', { timeout: STANDARD_TIMEOUT }).then(($body) => {
            // Already authenticated
            if ($body.find('button:contains("VIEW APPLICATIONS")').length > 0) {
                cy.contains('VIEW APPLICATIONS', { timeout: STANDARD_TIMEOUT }).click({ force: true })
                return
            }

            // Not authenticated
            if ($body.find('button:contains("LOGIN")').length > 0) {
                cy.contains('LOGIN', { timeout: STANDARD_TIMEOUT }).should('exist').click({ force: true })
                cy.contains('IDIR', { timeout: STANDARD_TIMEOUT }).should('exist').click({ force: true })

                cy.get('body', { timeout: STANDARD_TIMEOUT }).then(($loginBody) => {
                    // Perform IDIR login only if prompted
                    if ($loginBody.find('#user').length > 0) {
                        cy.get('#user', { timeout: STANDARD_TIMEOUT }).type(Cypress.env('test1username'))
                        cy.get('#password', { timeout: STANDARD_TIMEOUT }).type(Cypress.env('test1password'))
                        cy.contains('Continue', { timeout: STANDARD_TIMEOUT }).should('exist').click({ force: true })
                    } else {
                        cy.log('Already logged in')
                    }
                })

                return
            }

            // Fail loudly if neither state is detectable
            throw new Error('Unable to determine authentication state')
        })

        // 3.) Post-condition check
        cy.url({ timeout: STANDARD_TIMEOUT }).should('include', '/GrantApplications')
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

    it('Tests the existence and functionality of the Submitted Date From and Submitted Date To filters', () => {

        const pad2 = (n: number) => String(n).padStart(2, '0');

        const todayIsoLocal = () => {
            const d = new Date();
            return `${d.getFullYear()}-${pad2(d.getMonth() + 1)}-${pad2(d.getDate())}`;
        };

        const waitForRefresh = () => {
            // If the spinner shows, wait for it to finish. If it never shows, at least ensure it's not visible.
            cy.get('div.spinner-grow[role="status"]', { timeout: STANDARD_TIMEOUT })
                .then(($s) => {
                    const isHiddenNow = $s.attr('style') && $s.attr('style')!.includes('display: none');
                    if (!isHiddenNow) {
                        cy.wrap($s).should('have.attr', 'style').and('contain', 'display: none');
                    } else {
                        cy.wrap($s).should('have.attr', 'style').and('contain', 'display: none');
                    }
                });
        };

        // --- Submitted Date From ---
        cy.get('input#submittedFromDate', { timeout: STANDARD_TIMEOUT })
            .click({ force: true })
            .clear({ force: true })
            .type('2022-01-01', { force: true })
            .trigger('change', { force: true })
            .blur({ force: true })
            .should('have.value', '2022-01-01');

        waitForRefresh();

        // --- Submitted Date To ---
        const today = todayIsoLocal();

        cy.get('input#submittedToDate', { timeout: STANDARD_TIMEOUT })
            .click({ force: true })
            .clear({ force: true })
            .type(today, { force: true })
            .trigger('change', { force: true })
            .blur({ force: true })
            .should('have.value', today);

        waitForRefresh();

    });

    //  With no rows selected verify the visibility of Filter, Export, Save View, and Columns.
    it('Verify the action buttons are visible with no rows selected', () => {

    })

    //  With one row selected verify the visibility of Filter, Export, Save View, and Columns.
    it('Verify the action buttons are visible with one row selected', () => {

    })

    //  With two rows selected verify the visibility of Filter, Export, Save View, and Columns.
    it('Verify the action buttons are visible with two rows selected', () => {
        // Select first two applications (checkboxes are dynamic ids like row_874)
        cy.get('input.checkbox-select.chkbox[title="Select Application"]', { timeout: STANDARD_TIMEOUT })
            .should('have.length.greaterThan', 1)
            .then(($boxes) => {
                cy.wrap($boxes.eq(0)).check({ force: true })
                cy.wrap($boxes.eq(1)).check({ force: true })
            })

        // Assert the buttons directly
        cy.get('#assignApplication', { timeout: STANDARD_TIMEOUT })
            .should('exist')
            .and('be.visible')
            .and('contain.text', 'Assign')

        cy.get('#approveApplications', { timeout: STANDARD_TIMEOUT })
            .should('exist')
            .and('be.visible')
            .and('contain.text', 'Approve')

        cy.get('#tagApplication', { timeout: STANDARD_TIMEOUT })
            .should('exist')
            .and('be.visible')
            .and('contain.text', 'Tags')

        cy.get('#btn-toggle-filter', { timeout: STANDARD_TIMEOUT })
            .should('exist')
            .and('be.visible')
            .and('contain.text', 'Filter')

        cy.get('#dynamicButtonContainerId', { timeout: STANDARD_TIMEOUT }).should('exist')

        cy.contains('#dynamicButtonContainerId .dt-buttons button span', 'Export', { timeout: STANDARD_TIMEOUT })
            .should('exist')
            .and('be.visible')

        cy.contains('#dynamicButtonContainerId button.grp-savedStates', 'Save View', { timeout: STANDARD_TIMEOUT })
            .should('exist')
            .and('be.visible')

        cy.contains('#dynamicButtonContainerId .dt-buttons button span', 'Columns', { timeout: STANDARD_TIMEOUT })
            .should('exist')
            .and('be.visible')
    })

    // Walk the Columns menu and toggle each column on, verifying the column is visibile.
    it('Verify all columns in the menu are visible when and toggled on.', () => {
        const clickColumnsItem = (label: string) => {
            cy.contains('a.dropdown-item', label, { timeout: STANDARD_TIMEOUT })
                .should('exist')
                .scrollIntoView()
                .click({ force: true })
        }

        const getVisibleHeaderTitles = () => {
            return cy.get('.dt-scroll-head span.dt-column-title', { timeout: STANDARD_TIMEOUT }).then(($els) => {
                const titles = Cypress.$($els)
                    .toArray()
                    .map((el) => (el.textContent || '').replace(/\s+/g, ' ').trim())
                    .filter((t) => t.length > 0)
                return titles
            })
        }

        const assertVisibleHeadersInclude = (expected: string[]) => {
            getVisibleHeaderTitles().then((titles) => {
                expected.forEach((e) => {
                    expect(titles, `visible headers should include "${e}"`).to.include(e)
                })
            })
        }

        const scrollX = (x: number) => {
            cy.get('.dt-scroll-body', { timeout: STANDARD_TIMEOUT })
                .should('exist')
                .scrollTo(x, 0, { duration: 0, ensureScrollable: false })
        }

        // Open the "Save View" dropdown
        cy.get('button.grp-savedStates', { timeout: STANDARD_TIMEOUT })
            .should('be.visible')
            .and('contain.text', 'Save View')
            .click()

        // Click "Reset to Default View"
        cy.contains('a.dropdown-item', 'Reset to Default View', { timeout: STANDARD_TIMEOUT })
            .should('exist')
            .click({ force: true })

        // Open Columns menu
        cy.contains('span', 'Columns', { timeout: STANDARD_TIMEOUT })
            .should('be.visible')
            .click()

        clickColumnsItem('% of Total Project Budget')
        clickColumnsItem('Acquisition')
        clickColumnsItem('Applicant Electoral District')

        clickColumnsItem('Applicant Id')
        clickColumnsItem('Applicant Id')

        clickColumnsItem('Applicant Name')
        clickColumnsItem('Applicant Name')

        clickColumnsItem('Approved Amount')
        clickColumnsItem('Approved Amount')

        clickColumnsItem('Assessment Result')

        clickColumnsItem('Assignee')
        clickColumnsItem('Assignee')

        clickColumnsItem('Business Number')

        clickColumnsItem('Category')
        clickColumnsItem('Category')

        clickColumnsItem('City')

        clickColumnsItem('Community')
        clickColumnsItem('Community')

        clickColumnsItem('Community Population')
        clickColumnsItem('Contact Business Phone')
        clickColumnsItem('Contact Cell Phone')
        clickColumnsItem('Contact Email')
        clickColumnsItem('Contact Full Name')
        clickColumnsItem('Contact Title')
        clickColumnsItem('Decision Date')
        clickColumnsItem('Decline Rationale')
        clickColumnsItem('Due Date')
        clickColumnsItem('Due Diligence Status')
        clickColumnsItem('Economic Region')
        clickColumnsItem('Forestry Focus')
        clickColumnsItem('Forestry or Non-Forestry')
        clickColumnsItem('FYE Day')
        clickColumnsItem('FYE Month')
        clickColumnsItem('Indigenous')
        clickColumnsItem('Likelihood of Funding')
        clickColumnsItem('Non-Registered Organization Name')
        clickColumnsItem('Notes')
        clickColumnsItem('Org Book Status')
        clickColumnsItem('Organization Type')
        clickColumnsItem('Other Sector/Sub/Industry Description')
        clickColumnsItem('Owner')
        clickColumnsItem('Payout')
        clickColumnsItem('Place')
        clickColumnsItem('Project Electoral District')
        clickColumnsItem('Project End Date')

        clickColumnsItem('Project Name')
        clickColumnsItem('Project Name')

        clickColumnsItem('Project Start Date')
        clickColumnsItem('Project Summary')
        clickColumnsItem('Projected Funding Total')
        clickColumnsItem('Recommended Amount')
        clickColumnsItem('Red-Stop')
        clickColumnsItem('Regional District')
        clickColumnsItem('Registered Organization Name')
        clickColumnsItem('Registered Organization Number')

        clickColumnsItem('Requested Amount')
        clickColumnsItem('Requested Amount')

        clickColumnsItem('Risk Ranking')
        clickColumnsItem('Sector')
        clickColumnsItem('Signing Authority Business Phone')
        clickColumnsItem('Signing Authority Cell Phone')
        clickColumnsItem('Signing Authority Email')
        clickColumnsItem('Signing Authority Full Name')
        clickColumnsItem('Signing Authority Title')

        clickColumnsItem('Status')
        clickColumnsItem('Status')

        clickColumnsItem('Sub-Status')
        clickColumnsItem('Sub-Status')

        clickColumnsItem('Submission #')
        clickColumnsItem('Submission #')

        clickColumnsItem('Submission Date')
        clickColumnsItem('Submission Date')

        clickColumnsItem('SubSector')

        clickColumnsItem('Tags')
        clickColumnsItem('Tags')

        clickColumnsItem('Total Paid Amount $')
        clickColumnsItem('Total Project Budget')
        clickColumnsItem('Total Score')
        clickColumnsItem('Unity Application Id')

        // Close the menu and wait until the overlay is gone
        cy.get('div.dt-button-background', { timeout: STANDARD_TIMEOUT })
            .should('exist')
            .click({ force: true })

        cy.get('div.dt-button-background', { timeout: STANDARD_TIMEOUT }).should('not.exist')

        // Assertions by horizontal scroll segments (human-style scan)
        scrollX(0)
        assertVisibleHeadersInclude([
            'Applicant Name',
            'Category',
            'Submission #',
            'Submission Date',
            'Status',
            'Sub-Status',
            'Community',
            'Requested Amount',
            'Approved Amount',
            'Project Name',
            'Applicant Id',
        ])

        scrollX(1500)
        assertVisibleHeadersInclude([
            'Tags',
            'Assignee',
            'SubSector',
            'Economic Region',
            'Regional District',
            'Registered Organization Number',
            'Org Book Status',
        ])

        scrollX(3000)
        assertVisibleHeadersInclude([
            'Project Start Date',
            'Project End Date',
            'Projected Funding Total',
            'Total Paid Amount $',
            'Project Electoral District',
            'Applicant Electoral District',
        ])

        scrollX(4500)
        assertVisibleHeadersInclude([
            'Forestry or Non-Forestry',
            'Forestry Focus',
            'Acquisition',
            'City',
            'Community Population',
            'Likelihood of Funding',
            'Total Score',
        ])

        scrollX(6000)
        assertVisibleHeadersInclude([
            'Assessment Result',
            'Recommended Amount',
            'Due Date',
            'Owner',
            'Decision Date',
            'Project Summary',
            'Organization Type',
            'Business Number',
        ])

        scrollX(7500)
        assertVisibleHeadersInclude([
            'Due Diligence Status',
            'Decline Rationale',
            'Contact Full Name',
            'Contact Title',
            'Contact Email',
            'Contact Business Phone',
            'Contact Cell Phone',
        ])

        scrollX(9000)
        assertVisibleHeadersInclude([
            'Signing Authority Full Name',
            'Signing Authority Title',
            'Signing Authority Email',
            'Signing Authority Business Phone',
            'Signing Authority Cell Phone',
            'Place',
            'Risk Ranking',
            'Notes',
            'Red-Stop',
            'Indigenous',
            'FYE Day',
            'FYE Month',
            'Payout',
            'Unity Application Id',
        ])
    })


    it('Verify Logout', () => {
        cy.logout()
    })
})
