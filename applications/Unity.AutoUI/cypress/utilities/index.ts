/**
 * index.ts - Central export file for all page objects and utilities
 */

// Page Objects
export { BasePage } from "../pages/BasePage";
export { LoginPage } from "../pages/LoginPage";
export { NavigationPage } from "../pages/NavigationPage";
export { DashboardPage } from "../pages/DashboardPage";
export { ApplicationDetailsPage } from "../pages/ApplicationDetailsPage";
export {
  ListPage,
  ApplicationsPage,
  RolesPage,
  UsersPage,
  IntakesPage,
  FormsPage,
  PaymentsPage,
} from "../pages/ListPages";

// Utilities
export {
  PageFactory,
  LoginPageInstance,
  NavigationPageInstance,
  DashboardPageInstance,
  ApplicationsPageInstance,
  ApplicationDetailsPageInstance,
  RolesPageInstance,
  UsersPageInstance,
  IntakesPageInstance,
  FormsPageInstance,
  PaymentsPageInstance,
} from "./PageFactory";

export { TestDataHelper } from "./TestDataHelper";
