/// <reference types="cypress" />

import { BasePage } from "./BasePage";

type LeftTabName =
  | "Applicant Info"
  | "Contacts"
  | "Addresses"
  | "Submissions"
  | "History"
  | "Payments";

type RightTabName =
  | "Details"
  | "Email"
  | "Comments"
  | "Links"
  | "Attachments"
  | "History";

/**
 * ApplicantsViewPage - Page object for Applicants list + details view.
 */
export class ApplicantsViewPage extends BasePage {
  private readonly selectors = {
    search: "#search",
    statusActionsButton: "button:contains('Status Actions')",
    statusMenu:
      "#applicantActionBar .dropdown-menu, .action-bar .dropdown-menu",
    rightPaneTablist: ".right-card [role='tablist'], [role='tablist']",
  };

  private readonly rightTabSelectors: Partial<Record<RightTabName, string>> = {
    Details: "#details-tab",
    Email: "#emails-tab",
    Comments: "#comments-tab",
    Links: "#links-tab",
    Attachments: "#attachments-tab",
    History: "#history-tab",
  };

  private readonly rightTabFallbackIndex: Record<RightTabName, number> = {
    Details: 0,
    Email: 1,
    Comments: 2,
    Links: 3,
    Attachments: 4,
    History: 5,
  };

  waitForApplicantsList(): this {
    cy.location("pathname", { timeout: 20000 }).should(
      "include",
      "/GrantApplicants",
    );
    cy.get(this.selectors.search, { timeout: 20000 }).should("be.visible");
    return this;
  }

  visitApplicantsList(): this {
    const baseUrl = String(Cypress.env("webapp.url") || "");
    const normalizedBaseUrl = baseUrl.endsWith("/") ? baseUrl : `${baseUrl}/`;
    cy.visit(`${normalizedBaseUrl}GrantApplicants`);
    return this.waitForApplicantsList();
  }

  searchAndOpenApplicant(applicantName: string): this {
    this.typeText(this.selectors.search, applicantName);
    cy.get(this.selectors.search).type("{enter}");
    cy.contains("td a", new RegExp(applicantName, "i"), { timeout: 20000 })
      .first()
      .should("be.visible")
      .click({ force: true });
    cy.location("pathname", { timeout: 20000 }).should(
      "include",
      "/GrantApplicants/Details",
    );
    return this;
  }

  openStatusActions(): this {
    cy.contains("button", "Status Actions", { timeout: 20000 })
      .should("be.visible")
      .click({ force: true });
    cy.get(this.selectors.statusMenu, { timeout: 20000 }).should("be.visible");
    return this;
  }

  verifyStatusActionOptions(options: string[]): this {
    options.forEach((option) => {
      cy.get(this.selectors.statusMenu)
        .contains("button, a", new RegExp(`^${option}$`, "i"))
        .should("exist");
    });
    return this;
  }

  closeOpenMenus(): this {
    cy.get("body").click(0, 0, { force: true });
    return this;
  }

  openLeftTab(tabName: LeftTabName): this {
    cy.contains("[role='tab']:visible", new RegExp(`^${tabName}$`), {
      timeout: 20000,
    })
      .should("be.visible")
      .click({ force: true })
      .should(($tab) => {
        const isSelected =
          $tab.attr("aria-selected") === "true" || $tab.hasClass("active");
        expect(isSelected, `${tabName} tab selected`).to.equal(true);
      });

    // Wait for the selected tab panel to be rendered before tab-specific checks.
    cy.get(".tab-pane.active.show, .tab-pane.active", {
      timeout: 20000,
    }).should("exist");
    return this;
  }

  verifyLeftTabContent(tabName: LeftTabName): this {
    switch (tabName) {
      case "Applicant Info":
        cy.contains("Applicant Information", { timeout: 20000 }).should(
          "exist",
        );
        cy.contains("Organization Information", { timeout: 20000 }).should(
          "exist",
        );
        break;
      case "Contacts":
        cy.contains("Primary Contact", { timeout: 20000 }).should("exist");
        cy.contains("Contacts", { timeout: 20000 }).should("exist");
        cy.contains("th:visible", "Submission #", { timeout: 20000 }).should(
          "be.visible",
        );
        break;
      case "Addresses":
        cy.contains("Primary Physical Address", { timeout: 20000 }).should(
          "exist",
        );
        cy.contains("Primary Mailing Address", { timeout: 20000 }).should(
          "exist",
        );
        cy.contains("Addresses", { timeout: 20000 }).should("exist");
        break;
      case "Submissions":
        cy.contains("th:visible", "Submission #", { timeout: 20000 }).should(
          "be.visible",
        );
        cy.contains("th:visible", "Category", { timeout: 20000 }).should(
          "be.visible",
        );
        cy.contains("th:visible", "Status", { timeout: 20000 }).should(
          "be.visible",
        );
        break;
      case "History":
        cy.contains("Funding History", { timeout: 20000 }).should("exist");
        cy.contains("Issue Tracking", { timeout: 20000 }).should("exist");
        cy.contains("Audit History", { timeout: 20000 }).should("exist");
        break;
      case "Payments":
        cy.contains("Payment Summary", { timeout: 20000 }).should("exist");
        cy.contains("Payment List", { timeout: 20000 }).should("exist");
        cy.contains("th:visible", "Payment ID", { timeout: 20000 }).should(
          "be.visible",
        );
        break;
    }
    return this;
  }

  openRightTab(tabName: RightTabName): this {
    const selector = this.rightTabSelectors[tabName];

    if (selector) {
      cy.get("body").then(($body) => {
        if ($body.find(selector).length > 0) {
          cy.get(selector, { timeout: 20000 })
            .should("be.visible")
            .click({ force: true });
          return;
        }

        this.openRightTabByFallback(tabName);
      });
      return this;
    }

    this.openRightTabByFallback(tabName);
    return this;
  }

  verifyRightTabContent(tabName: RightTabName): this {
    switch (tabName) {
      case "Details":
        cy.contains("h6", "Applicant Details", { timeout: 20000 }).should(
          "exist",
        );
        cy.contains("h6", "Under Construction", { timeout: 20000 }).should(
          "exist",
        );
        break;
      case "Email":
        cy.contains("h6", "Emails", { timeout: 20000 }).should("exist");
        cy.contains("h6", "Under Construction", { timeout: 20000 }).should(
          "exist",
        );
        break;
      case "Comments":
        cy.contains("h6", "Comments", { timeout: 20000 }).should("exist");
        cy.get("textarea[placeholder='Add a comment']", {
          timeout: 20000,
        }).should("exist");
        break;
      case "Links":
        cy.contains("h6", "Links", { timeout: 20000 }).should("exist");
        cy.contains("h6", "Under Construction", { timeout: 20000 }).should(
          "exist",
        );
        break;
      case "Attachments":
        cy.contains("h6", "Applicant Attachments", { timeout: 20000 }).should(
          "exist",
        );
        cy.contains("button", "Add Attachments", { timeout: 20000 }).should(
          "exist",
        );
        cy.contains("th", "Document Name", { timeout: 20000 }).should("exist");
        break;
      case "History":
        cy.contains("h6", "History", { timeout: 20000 }).should("exist");
        cy.contains("h6", "Under Construction", { timeout: 20000 }).should(
          "exist",
        );
        break;
    }
    return this;
  }

  private openRightTabByFallback(tabName: RightTabName): void {
    cy.get("body").then(($body) => {
      const rightTablist = $body
        .find(this.selectors.rightPaneTablist)
        .filter((_, tabList) => {
          const text = (tabList.textContent || "").toLowerCase();
          return (
            text.includes("details") &&
            text.includes("email") &&
            text.includes("links")
          );
        });

      const targetList = rightTablist.first();
      expect(targetList.length, "right pane tab list exists").to.be.greaterThan(
        0,
      );

      cy.wrap(targetList)
        .find("[role='tab']")
        .eq(this.rightTabFallbackIndex[tabName])
        .should("be.visible")
        .click({ force: true });
    });
  }
}
