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

  verifyCoreUiStyling(): this {
    cy.contains("button", "Status Actions", { timeout: 20000 })
      .should("be.visible")
      .should(($button) => {
        const styles = window.getComputedStyle($button[0]);

        expect(styles.fontFamily.trim(), "button font-family").to.not.equal("");
        expect(
          parseFloat(styles.fontSize),
          "button font-size",
        ).to.be.greaterThan(10);
        expect(
          parseInt(styles.fontWeight, 10),
          "button font-weight",
        ).to.be.greaterThan(0);
        expect(styles.backgroundColor, "button background-color").to.not.equal(
          "rgba(0, 0, 0, 0)",
        );
      });

    cy.get("[role='tab']:visible", { timeout: 20000 })
      .first()
      .should(($tab) => {
        const styles = window.getComputedStyle($tab[0]);

        expect(styles.fontFamily.trim(), "tab font-family").to.not.equal("");
        expect(parseFloat(styles.fontSize), "tab font-size").to.be.greaterThan(
          10,
        );
        expect(styles.color, "tab text color").to.not.equal("rgba(0, 0, 0, 0)");
      });

    cy.get(this.selectors.statusMenu)
      .find("button, a")
      .first()
      .should("be.visible")
      .should(($action) => {
        const styles = window.getComputedStyle($action[0]);

        expect(
          styles.fontFamily.trim(),
          "status action font-family",
        ).to.not.equal("");
        expect(
          parseFloat(styles.fontSize),
          "status action font-size",
        ).to.be.greaterThan(10);
      });

    cy.get("body").then(($body) => {
      const iconCount = $body.find(
        "svg, i[class*='icon'], span[class*='icon'], .fa, .bi, .material-icons",
      ).length;

      expect(iconCount, "icons rendered in applicants view").to.be.greaterThan(
        0,
      );
    });

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
        cy.contains("th", "Submission #", { timeout: 20000 }).should("exist");
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
        cy.contains("th", "Submission #", { timeout: 20000 }).should("exist");
        cy.contains("th", "Category", { timeout: 20000 }).should("exist");
        cy.contains("th", "Status", { timeout: 20000 }).should("exist");
        break;
      case "History":
        cy.contains("Funding History", { timeout: 20000 }).should("exist");
        cy.contains("Issue Tracking", { timeout: 20000 }).should("exist");
        cy.contains("Audit History", { timeout: 20000 }).should("exist");
        break;
      case "Payments":
        cy.contains("Payment Summary", { timeout: 20000 }).should("exist");
        cy.contains("Payment List", { timeout: 20000 }).should("exist");
        cy.contains("th", "Payment ID", { timeout: 20000 }).should("exist");
        break;
    }

    this.verifyActiveTabInputValues(tabName);
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

    this.verifyActiveRightTabInputValues(tabName);
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

  private verifyActiveTabInputValues(tabName: LeftTabName): void {
    const inputSelector = [
      "input:not([type='hidden']):not([type='search']):not([type='checkbox']):not([type='radio']):not([type='file']):not([type='button']):not([type='submit'])",
      "textarea",
      "select",
    ].join(", ");

    cy.get(".tab-pane.active.show, .tab-pane.active", { timeout: 20000 })
      .first()
      .then(($panel) => {
        const $fields = $panel
          .find(inputSelector)
          .filter((_, element) => Cypress.$(element).is(":visible"));

        if ($fields.length === 0) {
          cy.log(`[${tabName}] no visible input fields found`);
          return;
        }

        cy.wrap($fields).each(($field, index) => {
          const $el = Cypress.$($field);
          const tagName = ($el.prop("tagName") as string).toLowerCase();
          const fieldName =
            $el.attr("name") ||
            $el.attr("id") ||
            $el.attr("placeholder") ||
            `${tagName}-${index}`;

          const rawValue =
            tagName === "select"
              ? String($el.find("option:selected").text() || "").trim()
              : String($el.val() ?? "").trim();

          expect(
            rawValue.toLowerCase(),
            `[${tabName}] ${fieldName} should not be undefined`,
          ).to.not.equal("undefined");
          expect(
            rawValue.toLowerCase(),
            `[${tabName}] ${fieldName} should not be null`,
          ).to.not.equal("null");

          const isRequiredField =
            $el.is("[required]") ||
            String($el.attr("aria-required") || "").toLowerCase() === "true";

          if (isRequiredField) {
            expect(
              rawValue,
              `[${tabName}] required field ${fieldName} should have a value`,
            ).to.not.equal("");
          }
        });
      });
  }

  private verifyActiveRightTabInputValues(tabName: RightTabName): void {
    const inputSelector = [
      "input:not([type='hidden']):not([type='search']):not([type='checkbox']):not([type='radio']):not([type='file']):not([type='button']):not([type='submit'])",
      "textarea",
      "select",
    ].join(", ");

    cy.get(".right-card .tab-pane.active.show, .right-card .tab-pane.active", {
      timeout: 20000,
    })
      .first()
      .then(($panel) => {
        const $fields = $panel
          .find(inputSelector)
          .filter((_, element) => Cypress.$(element).is(":visible"));

        if ($fields.length === 0) {
          cy.log(`[Right:${tabName}] no visible input fields found`);
          return;
        }

        cy.wrap($fields).each(($field, index) => {
          const $el = Cypress.$($field);
          const tagName = ($el.prop("tagName") as string).toLowerCase();
          const fieldName =
            $el.attr("name") ||
            $el.attr("id") ||
            $el.attr("placeholder") ||
            `${tagName}-${index}`;

          const rawValue =
            tagName === "select"
              ? String($el.find("option:selected").text() || "").trim()
              : String($el.val() ?? "").trim();

          expect(
            rawValue.toLowerCase(),
            `[Right:${tabName}] ${fieldName} should not be undefined`,
          ).to.not.equal("undefined");
          expect(
            rawValue.toLowerCase(),
            `[Right:${tabName}] ${fieldName} should not be null`,
          ).to.not.equal("null");

          const isRequiredField =
            $el.is("[required]") ||
            String($el.attr("aria-required") || "").toLowerCase() === "true";

          if (isRequiredField) {
            expect(
              rawValue,
              `[Right:${tabName}] required field ${fieldName} should have a value`,
            ).to.not.equal("");
          }
        });
      });
  }
}
