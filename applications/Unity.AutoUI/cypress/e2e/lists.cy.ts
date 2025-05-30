describe('Grant Manager Login and List Navigation', () => {

  it('Verify Login', () => {
    cy.login()
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
    cy.get('#applicationStatusChart > div > svg > g > text:nth-child(1)')
      .invoke('text')
      .then((text) => {
      const number = parseInt(text, 10)
      expect(number).to.be.gt(0)})
    cy.get('#economicRegionChart > div > svg > g > text:nth-child(1)')
      .invoke('text')
      .then((text) => {
      const number = parseInt(text, 10)
      expect(number).to.be.gt(0)})
    cy.get('#applicationAssigneeChart > div > svg > g > text:nth-child(1)')
      .invoke('text')
      .then((text) => {
      const number = parseInt(text, 10)
      expect(number).to.be.gt(0)})
    cy.get('#subsectorRequestedAmountChart > div > svg > g > text:nth-child(1)')
      .invoke('text') //gets the text of the element, which should be a string representation of a dollar amount
      .then((text) => {
      const amount = parseFloat(text.replace('$', '')) //the dollar sign is removed from the text and the result is converted to a number
      expect(amount).to.be.gt(0)}) //the amount is expected to be greater than zero
    // Return to top
    cy.visit(Cypress.env('webapp.url'))
  })
  it('Verify Logout', () => {
    cy.logout()
  })
})