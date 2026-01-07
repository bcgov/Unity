import { BasePage } from "./BasePage";

export class NavigationPage extends BasePage {
  // Navigation menu items
  private readonly navItems = {
    applications: "Applications",
    roles: "Roles",
    users: "Users",
    intakes: "Intakes",
    forms: "Forms",
    dashboard: "Dashboard",
    payments: "Payments",
  };

  // User menu selectors
  private readonly userMenuButton = ".unity-user-initials";
  private readonly tenantDropdown = "#user-dropdown .btn-dropdown span";

  constructor() {
    super(Cypress.env("webapp.url"));
  }

  /**
   * Click on user menu to open dropdown
   */
  clickUserMenu(): void {
    cy.get(this.userMenuButton).should("exist").click();
  }

  /**
   * Verify current tenant name
   */
  verifyCurrentTenant(tenantName: string): void {
    cy.get(this.tenantDropdown).should("contain", tenantName);
  }

  /**
   * Switch to a specific tenant if available
   * @param tenantName - Name of the tenant to switch to (e.g., "Default Grants Program")
   */
  switchToTenantIfAvailable(tenantName: string): void {
    cy.get("body").then(($body) => {
      // Check if user initials menu exists
      const hasUserInitials = $body.find(this.userMenuButton).length > 0;

      if (!hasUserInitials) {
        cy.log("Skipping tenant switch: no user initials menu found");
        return;
      }

      // Open user dropdowns
      cy.get(this.userMenuButton).click();

      cy.get("body").then(($body2) => {
        // Look for "Switch Grant Programs" link in dropdown
        const switchLink = $body2
          .find("#user-dropdown a.dropdown-item")
          .filter((_, el) => {
            return (el.textContent || "").trim() === "Switch Grant Programs";
          });

        if (switchLink.length === 0) {
          cy.log(
            'Skipping tenant switch: "Switch Grant Programs" not present for this user/session'
          );
          // Close dropdown so it does not block clicks later
          cy.get("body").click(0, 0);
          return;
        }

        // Click "Switch Grant Programs"
        cy.wrap(switchLink.first()).click();

        // Wait for Grant Programs page
        cy.url({ timeout: 20000 }).should("include", "/GrantPrograms");

        // Search for the tenant
        cy.get("#search-grant-programs", { timeout: 20000 })
          .should("be.visible")
          .clear()
          .type(tenantName);

        // Select the tenant from the table
        cy.get("#UserGrantProgramsTable", { timeout: 20000 })
          .should("be.visible")
          .within(() => {
            cy.contains("tbody tr", tenantName, { timeout: 20000 })
              .should("exist")
              .within(() => {
                cy.contains("button", "Select").should("be.enabled").click();
              });
          });

        // Verify redirect to GrantApplications or auth page
        cy.location("pathname", { timeout: 20000 }).should((p) => {
          expect(
            p.indexOf("/GrantApplications") >= 0 || p.indexOf("/auth/") >= 0
          ).to.eq(true);
        });
      });
    });
  }

  /**
   * Navigate to Applications page
   */
  goToApplications(): void {
    this.clickByText(this.navItems.applications);
  }

  /**
   * Navigate to Roles page
   */
  goToRoles(): void {
    this.clickByText(this.navItems.roles);
  }

  /**
   * Navigate to Users page
   */
  goToUsers(): void {
    this.clickByText(this.navItems.users);
  }

  /**
   * Navigate to Intakes page
   */
  goToIntakes(): void {
    this.clickByText(this.navItems.intakes);
  }

  /**
   * Navigate to Forms page
   */
  goToForms(): void {
    this.clickByText(this.navItems.forms);
  }

  /**
   * Navigate to Dashboard page
   */
  goToDashboard(): void {
    this.clickByText(this.navItems.dashboard);
  }

  /**
   * Navigate to Payments page
   */
  goToPayments(): void {
    this.clickByText(this.navItems.payments);
  }

  /**
   * Verify all navigation items exist
   */
  verifyAllNavItemsExist(): void {
    Object.values(this.navItems).forEach((item) => {
      cy.contains(item).should("exist");
    });
  }

  /**
   * Navigate to a specific page by name
   */
  navigateTo(pageName: string): void {
    this.clickByText(pageName);
    this.wait(1000);
  }

  /**
   * Verify navigation item exists
   */
  verifyNavItemExists(itemName: string): void {
    cy.contains(itemName).should("exist");
  }
}
