describe('Metabase Login and Logout', () => {
  beforeEach(() => { 
    cy.clearCookies(); // Clear cookies before each test  
    cy.clearLocalStorage(); // Clear local storage before each test
    cy.clearSessionStorage(); // Clear session storage before each test
    cy.clearBrowserCache(); // Clear browser cache before each test
  });
	
  it('Verify that Metabase is online', () => {
    cy.metabaseLogin()
  })
})