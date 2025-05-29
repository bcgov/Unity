describe('Grant Manager Login and Top Navigation', () => {

  it('Verify Login', () => {
    cy.login()
  })
  it('Verify navigation options in the top banner', () => {
    // 3.) Verify Default Grant Program tenant is selected.
    cy.get('.unity-user-initials').should('exist').click()
    cy.get('#user-dropdown .btn-dropdown span').should('contain', 'Default Grants Program')
    // 4.) Ensure all expected headings are present.
    // 5.) Applications
    cy.contains("Applications").should('exist').click()
    cy.wait(1000)
    // 6.) Roles
    cy.contains("Roles").should('exist').click()
    // 7.) Users
    cy.contains("Users").should('exist').click()
    // 8.) Intakes
    cy.contains("Intakes").should('exist').click()
    // 9.) Forms
    cy.contains("Forms").should('exist').click()
    // 10.) Dashboard
    cy.contains("Dashboard").should('exist').click()
    // 11.) Payments
    cy.contains("Payments").should('exist').click()
    // Return to top
    cy.visit(Cypress.env('webapp.url'))
  })
  it('Verify Logout', () => {
    cy.logout()
  })
})