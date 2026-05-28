describe("Chefs Login and Logout", () => {
  it("Verify that Chefs is online.", () => {
    cy.getChefsDetail("chefsBaseURL").then((baseURL) => {
      cy.visit(baseURL);

      cy.contains("button, a, [role='button']", /log\s*in|login/i)
        .should("exist")
        .click({ force: true });

      cy.location("pathname").should("include", "/app/login");
      cy.contains("button, a, [role='button']", /IDIR/i).should("exist");
      cy.contains("button, a, [role='button']", /BC Services Card/i).should(
        "exist",
      );
    });
  });
});
