/// <reference types="cypress" />
describe('Unity Login and check data from CHEFS', () => {

    it('Verify Login', () => {
        cy.login()
    })
    // 19.) Verify that the info panel populates with mapped data
    it('Verify the UI is populated with valid data from CHEFS', () => {

        cy.getSubmissionDetail('confirmationID').then(id => { cy.log(`Confirmation ID: ${id}`); });

        cy.get('#search').should('exist').clear(); // Ensure the field exists and clear its contents
        cy.get('#search').click() // click the search field
        cy.getSubmissionDetail('confirmationID').then(id => cy.get('#search').type(id)); // Fetch the confirmation ID and type it into the search field
        cy.getSubmissionDetail('confirmationID').then(id => cy.contains('tr', id).find('.checkbox-select').click()); // Fetch the confirmation ID, find its row, and click the checkbox

        cy.get('#applicationLink').should('exist').click() // open the info panel
        // 19.) Verify that the info panel populates with mapped data
        // Category: AutoUI
        cy.get('label[for="Category"]').next('.display-input').should('include.text', 'AutoUI');
        // Organization Name: DOLPHIN ASPHALT
        cy.get('label.display-input-label[for="OrganizationName"]').next('div.display-input').should('contain.text', 'DOLPHIN ASPHALT')
        // Organization #: 
        cy.get('label.display-input-label[for="OrganizationNumber"]').next('div.display-input').should('contain.text', 'FM0162628')
        // Economic Region: Kootenay
        cy.get('label[for="EconomicRegion"]').next('.display-input').should('include.text', 'Kootenay')
        // Regional District: East Kootenay
        cy.get('label[for="RegionalDistrict"]').next('.display-input').should('include.text', 'East Kootenay')
        // Community: East Kootenay B
        cy.get('label[for="Community"]').next('.display-input').should('include.text', 'East Kootenay B')
        // Requested Amount: $89,000.00
        cy.get('label[for="RequestedAmount"]').next('.display-input').should('include.text', '$89,000.00')
        // Total Project Budget: $125,000.00
        cy.get('label[for="ProjectBudget"]').next('.display-input').should('include.text', '$125,000.00')
        // Sector: Other services (except public administration)
        cy.get('label[for="Sector"]').next('.display-input').should('include.text', 'Other services (except public administration)')
        cy.get('#closeSummaryCanvas').click()
        // 20.) Verify that the details panel populates with mapped data
        cy.get('#externalLink').should('exist').click() //open the application
        // Category: AutoUI
        cy.get('label[for="Category"]').next('.display-input').should('include.text', 'AutoUI')
        // Organization Name: DOLPHIN ASPHALT
        cy.get('label[for="OrganizationName"]').next('.display-input').should('include.text', 'DOLPHIN ASPHALT')
        // Organization #: 
        cy.get('label[for="OrganizationNumber"]').next('.display-input').should('include.text', 'FM0162628')
        // Economic Region: Kootenay
        cy.get('label[for="EconomicRegion"]').next('.display-input').should('include.text', 'Kootenay')
        // Regional District: East Kootenay
        cy.get('label[for="RegionalDistrict"]').next('.display-input').should('include.text', 'East Kootenay')
        // Community: East Kootenay B
        cy.get('label[for="Community"]').next('.display-input').should('include.text', 'East Kootenay B')
        // Requested Amount: $89,000.00
        cy.get('label[for="RequestedAmount"]').next('.display-input').should('include.text', '$89,000.00')
        // Total Project Budget: $125,000.00
        cy.get('label[for="ProjectBudget"]').next('.display-input').should('include.text', '$125,000.00')
        // Sector: Other services (except public administration)
        cy.get('label[for="Sector"]').next('.display-input').should('include.text', 'Other services (except public administration)')
        // 21.) Verify that the Review & Assessment tab populates with mapped data
        cy.get('#nav-review-and-assessment-tab').should('exist').click() // open the Review & Assessment tab
        // Requested Amount: $89,000.00
        cy.get('#RequestedAmountInputAR').should('have.value', '89,000.00')
        // Total Project Budget: $125,000.00
        cy.get('#TotalBudgetInputAR').should('have.value', '125,000.00')
        // 22.) Verify that the Project Info tab populates with mapped data
        cy.get('#nav-project-info-tab').should('exist').click() // open the Project Info tab
        // Project Name
        cy.get('#ProjectInfo_ProjectName').should('have.value', 'Hanbury Development Initiative - Phase 2')
        // Project Start Date: 2026-01-05
        cy.get('#startDate').should('have.value', '2026-01-05')
        // Project End Date: 2027-03-11
        cy.get('#ProjectInfo_ProjectEndDate').should('have.value', '2027-03-11')
        // Requested Amount: $89,000.00
        cy.get('#RequestedAmountInputPI').should('have.value', '89,000.00')
        // Total Project Budget: $125,000.00
        cy.get('#TotalBudgetInputPI').should('have.value', '125,000.00')
        // Acquisition: No
        cy.get('#ProjectInfo_Acquisition').should('have.value', 'NO')
        // Forestry/Non-Forestry: Forestry
        cy.get('#ProjectInfo_Forestry').should('have.value', 'FORESTRY')
        // Forestry Focus: Secondary/Value-Added/Not Mass Timber (value="SECONDARY")
        cy.get('#ProjectInfo_ForestryFocus').should('have.value', 'SECONDARY')
        // Economic Region: Kootenay
        cy.get('#economicRegions').should('have.value', 'Kootenay')
        // Regional District: East Kootenay
        cy.get('#regionalDistricts').should('have.value', 'East Kootenay')
        // Community: East Kootenay B
        cy.get('#communities').should('have.value', 'East Kootenay B')
        // Community Population: 38
        cy.get('#ProjectInfo_CommunityPopulation').should('have.value', '38')
        // Electoral District: Kootenay-Rockies
        cy.get('#ProjectInfo_ElectoralDistrict').should('have.value', 'Kootenay-Rockies')
        // Place: Hanbury
        cy.get('#ProjectInfo_Place').should('have.value', 'Hanbury')

        // 23.) open the Applicant Info tab
        it('23. Applicant Info tab shows the mapped data', () => {
            // 1. open the pane
            cy.contains('a.nav-link', 'Applicant Info').click()

            // 2. wait for the Applicant Info fieldset, then work inside it
            cy.get('fieldset[name$="Applicant_Summary"]', { timeout: 10_000 })
                .should('be.visible')
                .as('app')                       // alias for scoping

            // 3. simple value assertions
            const plainInputs: [string, string][] = [
                ['#ApplicantSummary_OrgName', 'DOLPHIN ASPHALT'],
                ['#ApplicantSummary_OrgNumber', 'FM0162628'],
                ['#ApplicantSummary_ContactFullName', 'Jeff Gordon'],
                ['#ApplicantSummary_ContactTitle', 'Sr. Analyst'],
                ['#ApplicantSummary_ContactEmail', 'Jeff.Gordon@Dolphin.ca'],
                ['#ApplicantSummary_ContactBusinessPhone', '(250) 621-3217'],
                ['#ApplicantSummary_ContactCellPhone', '(887) 362-1459'],
                ['#ApplicantSummary_PhysicalAddressStreet', '24th Avenue South'],
                ['#ApplicantSummary_PhysicalAddressStreet2', 'Room 409'],
                ['#ApplicantSummary_PhysicalAddressUnit', '19'],
                ['#ApplicantSummary_PhysicalAddressCity', 'Cranbrook'],
                ['#ApplicantSummary_PhysicalAddressProvince', 'British Columbia'],
                ['#ApplicantSummary_PhysicalAddressPostalCode', 'V1C 3H8'],
                ['#ApplicantInfo_MailingAddressStreet', '2567 Shaughnessy Street'],
                ['#ApplicantInfo_MailingAddressStreet2', 'PO Box 905'],
                ['#ApplicantInfo_MailingAddressUnit', '22'],
                ['#ApplicantInfo_MailingAddressCity', 'Hanbury'],
                ['#ApplicantInfo_MailingAddressProvince', 'British Columbia'],
                ['#ApplicantInfo_MailingAddressPostalCode', 'V1C 4T6'],
                ['#ApplicantInfo_SigningAuthorityFullName', 'Maximillion Cooper'],
                ['#ApplicantInfo_SigningAuthorityTitle', 'Consultant'],
                ['#ApplicantInfo_SigningAuthorityEmail', 'Maximillion.Cooper@Dolphin.ca'],
                ['#ApplicantInfo_SigningAuthorityBusinessPhone', '(250) 841-2511'],
                ['#ApplicantInfo_SigningAuthorityCellPhone', '(657) 456-5413']
            ]

            plainInputs.forEach(([selector, expected]) => {
                cy.get('@app').find(selector).should('have.value', expected)
            })

            // 4. textarea requires .invoke('val')
            cy.get('@app')
                .find('#ApplicantSummary_SectorSubSectorIndustryDesc')
                .invoke('val')
                .should('equal', 'Stone Aggregate Recycling')
        })

        // 24.) Sector and Sub-sector lists
        it('24. Sector and Sub-sector dropdowns behave', () => {
            // open Applicant Info
            cy.contains('a.nav-link', 'Applicant Info').click()

            // locate the Sector and Sub-sector <select>s via their labels
            cy.contains('label.form-label', /^Sector$/, { timeout: 10_000 })
                .siblings('select')
                .as('sector')

            cy.contains('label.form-label', /^Sub-sector$/)
                .siblings('select')
                .as('subSector')

            // pick “Manufacturing” in Sector
            cy.get('@sector').select('Manufacturing')

            // one-line array of expected Sub-sector options
            const subs = ['Apparel manufacturing', 'Beverage and tobacco product manufacturing', 'Chemical manufacturing', 'Computer and electronic product manufacturing', 'Electrical equipment, appliance, and component manufacturing', 'Fabricated metal product manufacturing', 'Food manufacturing', 'Furniture and related product manufacturing', 'Leather and allied product manufacturing', 'Machinery manufacturing', 'Miscellaneous manufacturing', 'Non-metallic mineral product manufacturing', 'Other', 'Paper manufacturing', 'Petroleum and coal product manufacturing', 'Plastics and rubber products manufacturing', 'Primary metal manufacturing', 'Printing and related support activities', 'Textile mills', 'Textile product mills', 'Transportation equipment manufacturing', 'Wood product manufacturing'];

            // verify each option is selectable
            subs.forEach(text => {
                cy.get('@subSector').select(text).should('have.value', text)
            })
        })


        // 25	Verify that the Payment Info tab populates with mapped data
        cy.get('#nav-payment-info-tab').should('exist').click() // open the Payment Info tab
        // Requested Amount: 89,000.00
        cy.get('#RequestedAmount').should('have.value', '89,000.00')
        // 26.) Verify that the Submission tab populates with all form data
        cy.get('#nav-summery-tab').should('exist').click() // open the Submission tab 
        const headers = ['1. INTRODUCTION', '2. ELIGIBILITY', '3. APPLICANT INFORMATION', '4. PROJECT INFORMATION', '5. PROJECT TIMELINES', '6. PROJECT BUDGET', '7. ATTESTATION'];
        headers.forEach(header => {
            cy.contains('h4', header)
                .should('exist')
                .click();
        });
    })
    it('Verify Logout', () => {
        cy.logout()
    })
})