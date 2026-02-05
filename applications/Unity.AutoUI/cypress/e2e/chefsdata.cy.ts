/// <reference types="cypress" />

// cypress/e2e/chefsdata.cy.ts

describe('Unity Login and check data from CHEFS', () => {
    const STANDARD_TIMEOUT = 20000

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

    // Verify that the details panel populates with mapped data
    it('Verify the UI is populated with valid data from CHEFS', () => {
        cy.getSubmissionDetail('confirmationID').then((id) => {
            cy.log(`Confirmation ID: ${id}`)
        })

        // Ensure the search field exists
        cy.get('#search', { timeout: STANDARD_TIMEOUT }).should('exist')

        // Conditionally widen Submitted Date range if the control exists
        cy.get('body', { timeout: STANDARD_TIMEOUT }).then(($body) => {
            if ($body.find('input#submittedFromDate').length > 0) {
                cy.get('input#submittedFromDate', { timeout: STANDARD_TIMEOUT })
                    .should('exist')
                    .clear()
                    .type('2022-01-01')
            }
        })

        // Clear and focus search
        cy.get('#search', { timeout: STANDARD_TIMEOUT }).clear()
        cy.get('#search', { timeout: STANDARD_TIMEOUT }).click({ force: true })

        // Type confirmation ID
        cy.getSubmissionDetail('confirmationID').then((id) => {
            cy.get('#search', { timeout: STANDARD_TIMEOUT }).type(id)
        })

        // Select matching row if table rendering exists
        cy.getSubmissionDetail('confirmationID').then((id) => {
            cy.get('body', { timeout: STANDARD_TIMEOUT }).then(($body) => {
                if ($body.find(`tr:contains("${id}")`).length > 0) {
                    cy.contains('tr', id, { timeout: STANDARD_TIMEOUT })
                        .find('.checkbox-select')
                        .click({ force: true })
                }
            })
        })

        // Open the info panel if available
        cy.get('body', { timeout: STANDARD_TIMEOUT }).then(($body) => {
            if ($body.find('#applicationLink').length > 0) {
                cy.get('#applicationLink', { timeout: STANDARD_TIMEOUT }).click({ force: true })
            }
        })

        // Summary panel assertions
        cy.get('label[for="Category"]', { timeout: STANDARD_TIMEOUT }).next('.display-input').should('include.text', 'AutoUI')
        cy.get('label.display-input-label[for="OrganizationName"]', { timeout: STANDARD_TIMEOUT }).next('div.display-input').should('contain.text', 'DOLPHIN ASPHALT')
        cy.get('label.display-input-label[for="OrganizationNumber"]', { timeout: STANDARD_TIMEOUT }).next('div.display-input').should('contain.text', 'FM0162628')
        cy.get('label[for="EconomicRegion"]', { timeout: STANDARD_TIMEOUT }).next('.display-input').should('include.text', 'Kootenay')
        cy.get('label[for="RegionalDistrict"]', { timeout: STANDARD_TIMEOUT }).next('.display-input').should('include.text', 'East Kootenay')
        cy.get('label[for="Community"]', { timeout: STANDARD_TIMEOUT }).next('.display-input').should('include.text', 'East Kootenay B')
        cy.get('label[for="RequestedAmount"]', { timeout: STANDARD_TIMEOUT }).next('.display-input').should('include.text', '$89,000.00')
        cy.get('label[for="ProjectBudget"]', { timeout: STANDARD_TIMEOUT }).next('.display-input').should('include.text', '$125,000.00')
        cy.get('label[for="Sector"]', { timeout: STANDARD_TIMEOUT }).next('.display-input').should('include.text', 'Other services (except public administration)')

        cy.get('#closeSummaryCanvas', { timeout: STANDARD_TIMEOUT }).click({ force: true })

        // Open the application details
        cy.get('#externalLink', { timeout: STANDARD_TIMEOUT }).should('exist').click({ force: true })

        // Review & Assessment tab
        cy.get('#nav-review-and-assessment-tab', { timeout: STANDARD_TIMEOUT }).should('exist').click({ force: true })
        cy.get('#RequestedAmountInputAR', { timeout: STANDARD_TIMEOUT }).should('have.value', '89,000.00')
        cy.get('#TotalBudgetInputAR', { timeout: STANDARD_TIMEOUT }).should('have.value', '125,000.00')

        // Project Info tab
        cy.get('#nav-project-info-tab', { timeout: STANDARD_TIMEOUT }).should('exist').click({ force: true })
        cy.get('#ProjectInfo_ProjectName', { timeout: STANDARD_TIMEOUT }).should('have.value', 'Hanbury Development Initiative - Phase 2')
        cy.get('#startDate', { timeout: STANDARD_TIMEOUT }).should('have.value', '2026-01-05')
        cy.get('#ProjectInfo_ProjectEndDate', { timeout: STANDARD_TIMEOUT }).should('have.value', '2027-03-11')
        cy.get('#RequestedAmountInputPI', { timeout: STANDARD_TIMEOUT }).should('have.value', '89,000.00')
        cy.get('#TotalBudgetInputPI', { timeout: STANDARD_TIMEOUT }).should('have.value', '125,000.00')
        cy.get('#ProjectInfo_Acquisition', { timeout: STANDARD_TIMEOUT }).should('have.value', 'NO')
        cy.get('#ProjectInfo_Forestry', { timeout: STANDARD_TIMEOUT }).should('have.value', 'FORESTRY')
        cy.get('#ProjectInfo_ForestryFocus', { timeout: STANDARD_TIMEOUT }).should('have.value', 'SECONDARY')
        cy.get('#economicRegions', { timeout: STANDARD_TIMEOUT }).should('have.value', 'Kootenay')
        cy.get('#regionalDistricts', { timeout: STANDARD_TIMEOUT }).should('have.value', 'East Kootenay')
        cy.get('#communities', { timeout: STANDARD_TIMEOUT }).should('have.value', 'East Kootenay B')
        cy.get('#ProjectInfo_CommunityPopulation', { timeout: STANDARD_TIMEOUT }).should('have.value', '38')
        cy.get('#ProjectInfo_ElectoralDistrict', { timeout: STANDARD_TIMEOUT }).should('have.value', 'Kootenay-Rockies')
        cy.get('#ProjectInfo_Place', { timeout: STANDARD_TIMEOUT }).should('have.value', 'Hanbury')

        // Applicant Info tab
        cy.contains('a.nav-link', 'Applicant Info', { timeout: STANDARD_TIMEOUT }).should('exist').click({ force: true })

        // Applicant Summary fieldset
        cy.get('fieldset[name="Unity_GrantManager_ApplicationManagement_Applicant_Summary"]', { timeout: STANDARD_TIMEOUT })
            .should('exist')
            .as('appSummary')

        cy.get('@appSummary').find('#ApplicantSummary_ApplicantName', { timeout: STANDARD_TIMEOUT }).should('have.value', 'Dolphin Asphalt')

        cy.get('@appSummary').find('#ApplicantSummary_Sector', { timeout: STANDARD_TIMEOUT }).should('exist')
        cy.get('@appSummary').find('#ApplicantSummary_SubSector', { timeout: STANDARD_TIMEOUT }).should('exist')

        cy.get('@appSummary')
            .find('#ApplicantSummary_SectorSubSectorIndustryDesc', { timeout: STANDARD_TIMEOUT })
            .should('exist')
            .invoke('val')
            .should('equal', 'Stone Aggregate Recycling')

        // Contact Info fieldset (TEST uses ContactInfo_* ids) :contentReference[oaicite:2]{index=2}
        cy.get('fieldset[name="Unity_GrantManager_ApplicationManagement_Applicant_Contact"]', { timeout: STANDARD_TIMEOUT })
            .should('exist')
            .as('contactInfo')

        cy.get('@contactInfo').find('#ContactInfo_Name', { timeout: STANDARD_TIMEOUT }).should('have.value', 'Jeff Gordon')
        cy.get('@contactInfo').find('#ContactInfo_Title', { timeout: STANDARD_TIMEOUT }).should('have.value', 'Sr. Analyst')
        cy.get('@contactInfo').find('#ContactInfo_Email', { timeout: STANDARD_TIMEOUT }).should('have.value', 'Jeff.Gordon@Dolphin.ca')
        cy.get('@contactInfo').find('#ContactInfo_Phone', { timeout: STANDARD_TIMEOUT }).should('have.value', '(250) 621-3217')
        cy.get('@contactInfo').find('#ContactInfo_Phone2', { timeout: STANDARD_TIMEOUT }).should('have.value', '(887) 362-1459')

        // Sector/Sub-sector dropdown behavior
        cy.get('@appSummary').find('#ApplicantSummary_Sector', { timeout: STANDARD_TIMEOUT }).select('Manufacturing')

        const subs = [
            'Apparel manufacturing',
            'Beverage and tobacco product manufacturing',
            'Chemical manufacturing',
            'Computer and electronic product manufacturing',
            'Electrical equipment, appliance, and component manufacturing',
            'Fabricated metal product manufacturing',
            'Food manufacturing',
            'Furniture and related product manufacturing',
            'Leather and allied product manufacturing',
            'Machinery manufacturing',
            'Miscellaneous manufacturing',
            'Non-metallic mineral product manufacturing',
            'Other',
            'Paper manufacturing',
            'Petroleum and coal product manufacturing',
            'Plastics and rubber products manufacturing',
            'Primary metal manufacturing',
            'Printing and related support activities',
            'Textile mills',
            'Textile product mills',
            'Transportation equipment manufacturing',
            'Wood product manufacturing'
        ]

        subs.forEach((text) => {
            cy.get('@appSummary')
                .find('#ApplicantSummary_SubSector', { timeout: STANDARD_TIMEOUT })
                .select(text)
                .should('have.value', text)
        })

        // Payment Info tab
        cy.get('#nav-payment-info-tab', { timeout: STANDARD_TIMEOUT }).should('exist').click({ force: true })
        cy.get('#RequestedAmount', { timeout: STANDARD_TIMEOUT }).should('have.value', '89,000.00')

        // Submission tab
        cy.get('#nav-summery-tab', { timeout: STANDARD_TIMEOUT }).should('exist').click({ force: true })

        // In TEST, the section headers are inside shadow DOM and include the numeric prefix (e.g., "2. ELIGIBILITY").
        // Anchor to the actual tag and allow the number to vary.
        const sectionRegexes: RegExp[] = [
            /^\s*\d+\.\s*INTRODUCTION\s*$/i,
            /^\s*\d+\.\s*ELIGIBILITY\s*$/i,
            /^\s*\d+\.\s*APPLICANT INFORMATION\s*$/i,
            /^\s*\d+\.\s*PROJECT INFORMATION\s*$/i,
            /^\s*\d+\.\s*PROJECT TIMELINES\s*$/i,
            /^\s*\d+\.\s*PROJECT BUDGET\s*$/i,
            /^\s*\d+\.\s*ATTESTATION\s*$/i
        ]

        // Wait for one known header to render
        cy.contains('h4', /^\s*\d+\.\s*INTRODUCTION\s*$/i, { timeout: STANDARD_TIMEOUT }).should('exist')

        sectionRegexes.forEach((rx) => {
            cy.contains('h4', rx, { timeout: STANDARD_TIMEOUT })
                .should('exist')
                .click({ force: true })
        })
    })

    it('Verify Logout', () => {
        cy.logout()
    })
})
