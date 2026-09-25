export interface ShellMenuItem {
  name: string;
  displayName: string;
  url: string | null;
  icon: string | null;
  items: ShellMenuItem[];
}

export interface ShellBranding {
  appName: string;
  logoUrl: string;
}

export interface ShellUser {
  badge: string | null;
  currentTenantName: string | null;
}

export interface ShellUserDropdown {
  showSwitchGrantPrograms: boolean;
  showApplicantPortalConfiguration: boolean;
  showConfigurationManagement: boolean;
  showUnityAdmin: boolean;
}

export interface ShellBootstrap {
  branding: ShellBranding;
  user: ShellUser;
  mainMenu: ShellMenuItem[];
  userDropdown: ShellUserDropdown;
}
