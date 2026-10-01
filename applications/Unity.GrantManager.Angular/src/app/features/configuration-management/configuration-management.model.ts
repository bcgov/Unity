// Mirrors Pages/ConfigurationManagement/Index.cshtml.cs's IndexModel visibility
// flags exactly - same server-side computation, exposed via
// ConfigurationManagementController.GetBootstrapAsync().
export interface ConfigurationManagementBootstrap {
  showNotifications: boolean;
  showPayments: boolean;
  showCustomFields: boolean;
  showScoresheets: boolean;
  showTags: boolean;
  showAI: boolean;
  showProgramDetails: boolean;
}

// One entry per side-menu item that is NOT yet migrated to Angular - clicking these
// sets Index.js's exact "active menu" localStorage key/value (see
// EmailsWidget/Default.js for the established precedent of this same pattern) and
// navigates back to the MVC page, landing directly on that section.
export interface MvcSideMenuItem {
  flag: keyof Omit<ConfigurationManagementBootstrap, 'showProgramDetails'>;
  label: string;
  elementId: string;
}

export const MVC_SIDE_MENU_ITEMS: MvcSideMenuItem[] = [
  { flag: 'showNotifications', label: 'Notifications', elementId: 'notifications-menu-item' },
  { flag: 'showPayments', label: 'Payments', elementId: 'payments-menu-item' },
  { flag: 'showCustomFields', label: 'Custom Fields', elementId: 'custom-fields-menu-item' },
  { flag: 'showScoresheets', label: 'Scoresheets', elementId: 'scoresheets-menu-item' },
  { flag: 'showTags', label: 'Tags', elementId: 'tags-menu-item' },
  { flag: 'showAI', label: 'AI', elementId: 'ai-menu-item' }
];
