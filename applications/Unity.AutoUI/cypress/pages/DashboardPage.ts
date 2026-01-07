import { BasePage } from "./BasePage";

export class DashboardPage extends BasePage {
  // Chart selectors
  private readonly chartSelectors = {
    applicationStatusChart:
      "#applicationStatusChart > div > svg > g > text:nth-child(1)",
    economicRegionChart:
      "#economicRegionChart > div > svg > g > text:nth-child(1)",
    applicationAssigneeChart:
      "#applicationAssigneeChart > div > svg > g > text:nth-child(1)",
    subsectorRequestedAmountChart:
      "#subsectorRequestedAmountChart > div > svg > g > text:nth-child(1)",
  };

  // Intake selector elements
  private readonly intakeButton = 'button[data-id="dashboardIntakeId"]';
  private readonly intakeListbox = '#bs-select-1[role="listbox"]';
  private readonly intakeSearch =
    'input[type="search"][aria-controls="bs-select-1"][aria-label="Search"]';

  constructor() {
    super();
  }

  /**
   * Set dashboard intake to a specific value if available
   * @param intakeName - Name of the intake to select (e.g., 'Test')
   */
  setIntakeIfAvailable(intakeName: string): void {
    cy.get("body").then(($body) => {
      if ($body.find(this.intakeButton).length === 0) {
        cy.log(`Skipping intake switch: dashboard intake selector not found`);
        return;
      }

      // Open the intake dropdown
      cy.get(this.intakeButton).first().click({ force: true });

      cy.get(this.intakeListbox, { timeout: 20000 }).should("exist");

      // Search for intake if search field exists
      cy.get("body").then(($b2) => {
        if ($b2.find(this.intakeSearch).length > 0) {
          cy.get(this.intakeSearch)
            .should("be.visible")
            .clear()
            .type(intakeName);
        }
      });

      // Find and click the intake option
      cy.get(this.intakeListbox).within(() => {
        cy.get('a.dropdown-item[role="option"]').then(($opts) => {
          const match = $opts.filter((_, el) => {
            const textSpan = el.querySelector("span.text");
            const label =
              (textSpan ? textSpan.textContent : el.textContent) || "";
            return label.trim() === intakeName;
          });

          if (match.length === 0) {
            cy.log(`Skipping intake switch: "${intakeName}" option not found`);
            return;
          }

          const domEl = match.get(0) as HTMLElement;

          const ariaSelected =
            (domEl.getAttribute("aria-selected") || "").trim() === "true";
          const classSelected =
            domEl.classList.contains("selected") ||
            (domEl.closest("li")?.classList.contains("selected") ?? false);

          if (!ariaSelected && !classSelected) {
            cy.wrap(domEl).scrollIntoView().click({ force: true });
          }
        });
      });

      // Verify selection state
      cy.get(this.intakeListbox).within(() => {
        cy.get('a.dropdown-item[role="option"]').then(($opts2) => {
          const match2 = $opts2.filter((_, el) => {
            const textSpan = el.querySelector("span.text");
            const label =
              (textSpan ? textSpan.textContent : el.textContent) || "";
            return label.trim() === intakeName;
          });

          expect(match2.length).to.be.greaterThan(0);

          const domEl2 = match2.get(0) as HTMLElement;

          const ariaSelected2 =
            (domEl2.getAttribute("aria-selected") || "").trim() === "true";
          const classSelected2 =
            domEl2.classList.contains("selected") ||
            (domEl2.closest("li")?.classList.contains("selected") ?? false);

          expect(ariaSelected2 || classSelected2).to.eq(true);
        });
      });

      // Close dropdown so it does not block chart interaction
      cy.get("body").click(0, 0);
    });
  }

  /**
   * Verify Application Status Chart has data
   */
  verifyApplicationStatusChartHasData(): void {
    this.getElement(this.chartSelectors.applicationStatusChart)
      .invoke("text")
      .then((text) => {
        const number = parseInt(text, 10);
        expect(number).to.be.gt(0);
      });
  }

  /**
   * Verify Economic Region Chart has data
   */
  verifyEconomicRegionChartHasData(): void {
    this.getElement(this.chartSelectors.economicRegionChart)
      .invoke("text")
      .then((text) => {
        const number = parseInt(text, 10);
        expect(number).to.be.gt(0);
      });
  }

  /**
   * Verify Application Assignee Chart has data
   */
  verifyApplicationAssigneeChartHasData(): void {
    this.getElement(this.chartSelectors.applicationAssigneeChart)
      .invoke("text")
      .then((text) => {
        const number = parseInt(text, 10);
        expect(number).to.be.gt(0);
      });
  }

  /**
   * Verify Subsector Requested Amount Chart has data
   */
  verifySubsectorRequestedAmountChartHasData(): void {
    this.getElement(this.chartSelectors.subsectorRequestedAmountChart)
      .invoke("text")
      .then((text) => {
        const amount = parseFloat(text.replace("$", ""));
        expect(amount).to.be.gt(0);
      });
  }

  /**
   * Verify all dashboard charts have data
   */
  verifyAllChartsHaveData(): void {
    this.verifyApplicationStatusChartHasData();
    this.verifyEconomicRegionChartHasData();
    this.verifyApplicationAssigneeChartHasData();
    this.verifySubsectorRequestedAmountChartHasData();
  }

  /**
   * Get chart value as number
   */
  getChartValue(chartSelector: string): Cypress.Chainable<number> {
    return this.getElement(chartSelector)
      .invoke("text")
      .then((text) => parseInt(text, 10));
  }

  /**
   * Get chart value as currency
   */
  getChartCurrencyValue(chartSelector: string): Cypress.Chainable<number> {
    return this.getElement(chartSelector)
      .invoke("text")
      .then((text) => parseFloat(text.replace("$", "")));
  }
}
