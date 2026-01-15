/**
 * PageFactory - Factory pattern for creating page objects
 */

import { LoginPage } from "../pages/LoginPage";
import { NavigationPage } from "../pages/NavigationPage";
import { DashboardPage } from "../pages/DashboardPage";
import { ApplicationDetailsPage } from "../pages/ApplicationDetailsPage";
import { EmailsPage } from "../pages/EmailsPage";
import { ApplicationActionBarPage } from "../pages/ApplicationActionBarPage";
import {
  ApplicationsPage,
  RolesPage,
  UsersPage,
  IntakesPage,
  FormsPage,
  PaymentsPage,
} from "../pages/ListPages";

export class PageFactory {
  private static instances: Map<string, any> = new Map();

  /**
   * Get or create LoginPage instance
   */
  static getLoginPage(): LoginPage {
    return this.getInstance("LoginPage", () => new LoginPage());
  }

  /**
   * Get or create NavigationPage instance
   */
  static getNavigationPage(): NavigationPage {
    return this.getInstance("NavigationPage", () => new NavigationPage());
  }

  /**
   * Get or create DashboardPage instance
   */
  static getDashboardPage(): DashboardPage {
    return this.getInstance("DashboardPage", () => new DashboardPage());
  }

  /**
   * Get or create ApplicationsPage instance
   */
  static getApplicationsPage(): ApplicationsPage {
    return this.getInstance("ApplicationsPage", () => new ApplicationsPage());
  }

  /**
   * Get or create ApplicationDetailsPage instance
   */
  static getApplicationDetailsPage(): ApplicationDetailsPage {
    return this.getInstance(
      "ApplicationDetailsPage",
      () => new ApplicationDetailsPage()
    );
  }

  /**
   * Get or create RolesPage instance
   */
  static getRolesPage(): RolesPage {
    return this.getInstance("RolesPage", () => new RolesPage());
  }

  /**
   * Get or create UsersPage instance
   */
  static getUsersPage(): UsersPage {
    return this.getInstance("UsersPage", () => new UsersPage());
  }

  /**
   * Get or create IntakesPage instance
   */
  static getIntakesPage(): IntakesPage {
    return this.getInstance("IntakesPage", () => new IntakesPage());
  }

  /**
   * Get or create FormsPage instance
   */
  static getFormsPage(): FormsPage {
    return this.getInstance("FormsPage", () => new FormsPage());
  }

  /**
   * Get or create PaymentsPage instance
   */
  static getPaymentsPage(): PaymentsPage {
    return this.getInstance("PaymentsPage", () => new PaymentsPage());
  }

  /**
   * Get or create EmailsPage instance
   */
  static getEmailsPage(): EmailsPage {
    return this.getInstance("EmailsPage", () => new EmailsPage());
  }

  /**
   * Get or create ApplicationActionBarPage instance
   */
  static getApplicationActionBarPage(): ApplicationActionBarPage {
    return this.getInstance(
      "ApplicationActionBarPage",
      () => new ApplicationActionBarPage()
    );
  }

  /**
   * Generic getInstance with caching
   */
  private static getInstance<T>(key: string, factory: () => T): T {
    if (!this.instances.has(key)) {
      this.instances.set(key, factory());
    }
    return this.instances.get(key);
  }

  /**
   * Clear all cached instances (useful for test isolation)
   */
  static clearCache(): void {
    this.instances.clear();
  }

  /**
   * Clear specific page instance
   */
  static clearInstance(key: string): void {
    this.instances.delete(key);
  }
}

/**
 * Convenience exports for direct page access
 */
export const LoginPageInstance = () => PageFactory.getLoginPage();
export const NavigationPageInstance = () => PageFactory.getNavigationPage();
export const DashboardPageInstance = () => PageFactory.getDashboardPage();
export const ApplicationsPageInstance = () => PageFactory.getApplicationsPage();
export const ApplicationDetailsPageInstance = () =>
  PageFactory.getApplicationDetailsPage();
export const RolesPageInstance = () => PageFactory.getRolesPage();
export const UsersPageInstance = () => PageFactory.getUsersPage();
export const IntakesPageInstance = () => PageFactory.getIntakesPage();
export const FormsPageInstance = () => PageFactory.getFormsPage();
export const PaymentsPageInstance = () => PageFactory.getPaymentsPage();
export const EmailsPageInstance = () => PageFactory.getEmailsPage();
export const ApplicationActionBarPageInstance = () =>
  PageFactory.getApplicationActionBarPage();
