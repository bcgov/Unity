describe('Grant Manager Login and Logout', () => {

  it('Verify Default Grant Program tenant is selected.', () => {
    cy.login()
    cy.get('.unity-user-initials').should('exist').click()
    cy.logout()
  })
})