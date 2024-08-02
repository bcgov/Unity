describe('Chefs Login and Logout', () => {

  it('Verify that Chefs is online.', () => {
    cy.chefsLogin();
	cy.contains("My Forms").should('exist').click();
    cy.chefsLogout();
  })
})