/// <reference types="cypress" />
describe('Unity Login and check data from CHEFS', () => {
	
  it('Verify Login', () => {
    cy.login()
  })
  // 19.) Verify that the info panel populates with mapped data
  it('Verify the UI is populated with valid data from CHEFS', () => {
    
    cy.getSubmissionDetail('confirmationID').then(id => {cy.log(`Confirmation ID: ${id}`);});

	cy.get('#search').should('exist').clear(); // Ensure the field exists and clear its contents
	cy.get('#search').click() // click the search field
	cy.getSubmissionDetail('confirmationID').then(id => cy.get('#search').type(id)); // Fetch the confirmation ID and type it into the search field
    cy.getSubmissionDetail('confirmationID').then(id => cy.contains('tr', id).find('.checkbox-select').click()); // Fetch the confirmation ID, find its row, and click the checkbox

	cy.get('#applicationLink').should('exist').click() // open the info panel
    // 19.) Verify that the info panel populates with mapped data
    // Category: AutoUI
    cy.get('label[for="Category"]').next('.display-input').should('include.text', 'AutoUI');
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
    // 23.) Verify that the Applicant Info tab populates with mapped data
    cy.get('#nav-organization-info-tab').should('exist').click() // open the Applicant Info tab
    // Organization Name: DOLPHIN ASPHALT
    cy.get('#ApplicantInfo_OrgName').should('have.value', 'DOLPHIN ASPHALT')
    // Organization #: FM0162628
    cy.get('#ApplicantInfo_OrgNumber').should('have.value', 'FM0162628')
    // Other Sector/Sub/Industry Description: Stone Aggregate Recycling
    cy.get('#ApplicantInfo_SectorSubSectorIndustryDesc').should('have.value', 'Stone Aggregate Recycling')
    // Full Name: Jeff Gordon
    cy.get('#ApplicantInfo_ContactFullName').should('have.value', 'Jeff Gordon')
    // Title: Sr. Analyst
    cy.get('#ApplicantInfo_ContactTitle').should('have.value', 'Sr. Analyst')
    // Email: Jeff.Gordon@Dolphin.ca
    cy.get('#ApplicantInfo_ContactEmail').should('have.value', 'Jeff.Gordon@Dolphin.ca')
    // Business Phone: (250) 621-3217
    cy.get('#ApplicantInfo_ContactBusinessPhone').should('have.value', '(250) 621-3217')
    // Cell Phone: (887) 362-1459
    cy.get('#ApplicantInfo_ContactCellPhone').should('have.value', '(887) 362-1459')
    // (Physical Address)
    // Street: 24th Avenue South
    cy.get('#ApplicantInfo_PhysicalAddressStreet').should('have.value', '24th Avenue South')
    // Street 2: Room 409
    cy.get('#ApplicantInfo_PhysicalAddressStreet2').should('have.value', 'Room 409')
    // Unit: 19
    cy.get('#ApplicantInfo_PhysicalAddressUnit').should('have.value', '19')
    // City: Cranbrook
    cy.get('#ApplicantInfo_PhysicalAddressCity').should('have.value', 'Cranbrook')
    // Province: British Columbia
    cy.get('#ApplicantInfo_PhysicalAddressProvince').should('have.value', 'British Columbia')
    // Postal Code: V1C 3H8
    cy.get('#ApplicantInfo_PhysicalAddressPostalCode').should('have.value', 'V1C 3H8')
    // (Mailing Address)
    // Street: 2567 Shaughnessy Street
    cy.get('#ApplicantInfo_MailingAddressStreet').should('have.value', '2567 Shaughnessy Street')
    // Street 2: PO Box 905
    cy.get('#ApplicantInfo_MailingAddressStreet2').should('have.value', 'PO Box 905')
    // Unit: 22
    cy.get('#ApplicantInfo_MailingAddressUnit').should('have.value', '22')
    // City: Hanbury
    cy.get('#ApplicantInfo_MailingAddressCity').should('have.value', 'Hanbury')
    // Province: British Columbia
    cy.get('#ApplicantInfo_MailingAddressProvince').should('have.value', 'British Columbia')
    // Postal Code: V1C 4T6
    cy.get('#ApplicantInfo_MailingAddressPostalCode').should('have.value', 'V1C 4T6')
    // (Signing Authority)
    // Full Name: Maximillion Cooper
    cy.get('#ApplicantInfo_SigningAuthorityFullName').should('have.value', 'Maximillion Cooper')
    // Title: Consultant
    cy.get('#ApplicantInfo_SigningAuthorityTitle').should('have.value', 'Consultant')
    // Email: Maximillion.Cooper@Dolphin.ca
    cy.get('#ApplicantInfo_SigningAuthorityEmail').should('have.value', 'Maximillion.Cooper@Dolphin.ca')
    // Business Phone: (250) 841-2511
    cy.get('#ApplicantInfo_SigningAuthorityBusinessPhone').should('have.value', '(250) 841-2511')
    // Phone: (657) 456-5413
    cy.get('#ApplicantInfo_SigningAuthorityCellPhone').should('have.value', '(657) 456-5413')
    // 24.) Verify that the Sector and Subsector Select Lists have a valid list of values.
    // Check if the sector dropdown contains an option for "Manufacturing"
    cy.get('#orgSectorDropdown').should('contain', 'Manufacturing')
    // Select manufacturing
    cy.get('#orgSectorDropdown').select('Manufacturing')
      // Array of all expected options to check for in the Subsector list of values.
      const options = ['Apparel manufacturing', 'Beverage and tobacco product manufacturing', 'Chemical manufacturing', 'Computer and electronic product manufacturing', 'Electrical equipment, appliance, and component manufacturing', 'Fabricated metal product manufacturing', 'Food manufacturing', 'Furniture and related product manufacturing', 'Leather and allied product manufacturing', 'Machinery manufacturing', 'Miscellaneous manufacturing', 'Non-metallic mineral product manufacturing', 'Other', 'Paper manufacturing', 'Petroleum and coal product manufacturing', 'Plastics and rubber products manufacturing', 'Primary metal manufacturing', 'Printing and related support activities', 'Textile mills', 'Textile product mills', 'Transportation equipment manufacturing', 'Wood product manufacturing']
      // Check if the dropdown contains each expected option in the options array
      options.forEach(option => {cy.get('#orgSubSectorDropdown').select(option).should('have.value', option)
      })
    // 25	Verify that the Payment Info tab populates with mapped data
    cy.get('#nav-payment-info-tab').should('exist').click() // open the Payment Info tab
    // Requested Amount: 89,000.00
    cy.get('#RequestedAmount').should('have.value', '89,000.00')
    // 26.) Verify that the Submission tab populates with all form data
    cy.get('#nav-summery-tab').should('exist').click() // open the Submission tab 
    const headers = ['1. INTRODUCTION', '2. ELIGIBILITY', '3. APPLICANT INFORMATION','4. PROJECT INFORMATION', '5. PROJECT TIMELINES', '6. PROJECT BUDGET', '7. ATTESTATION'];
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