import { Component, OnInit, inject, signal } from '@angular/core';
import { RouterOutlet } from '@angular/router';

import { loadStylesheet } from '../../core/theme-assets';
import { ConfigurationManagementBootstrap, MVC_SIDE_MENU_ITEMS, MvcSideMenuItem } from './configuration-management.model';
import { ConfigurationManagementService } from './configuration-management.service';

const ACTIVE_MENU_STORAGE_KEY = 'ConfigurationManagement_ActiveMenu';

// Mirrors Pages/ConfigurationManagement/Index.cshtml's .config-page-layout: a
// sticky side menu (.side-menu) next to the section content (.config-content).
// Program Details is the only section actually migrated to Angular - every other
// visible item (permission/feature-gated the same way the Razor page's own
// side-menu is) navigates back to the MVC page's matching section instead of
// showing content inline here, same as the top-level shell menu does for
// everything outside ConfigurationManagement.
@Component({
  selector: 'app-configuration-management',
  standalone: true,
  imports: [RouterOutlet],
  templateUrl: './configuration-management.component.html',
  styleUrl: './configuration-management.component.scss'
})
export class ConfigurationManagementComponent implements OnInit {
  private readonly configurationManagementService = inject(ConfigurationManagementService);

  readonly bootstrap = signal<ConfigurationManagementBootstrap | null>(null);
  readonly mvcSideMenuItems: MvcSideMenuItem[] = MVC_SIDE_MENU_ITEMS;

  // Placeholder rows shown in the side menu while bootstrap() is still null - count
  // matches MVC_SIDE_MENU_ITEMS plus the Program Details item, i.e. the max number
  // of real rows that could appear once the bootstrap flags come back.
  readonly skeletonRows = Array.from({ length: MVC_SIDE_MENU_ITEMS.length + 1 });

  constructor() {
    // Feature-specific, not shell-global (core/theme-assets.ts) - matches how the
    // Razor page itself only pulls this in via its own @section styles block.
    loadStylesheet('/Pages/ConfigurationManagement/Index.css');
  }

  ngOnInit(): void {
    this.configurationManagementService.getBootstrap().subscribe((bootstrap) => {
      this.bootstrap.set(bootstrap);
    });
  }

  isVisible(item: MvcSideMenuItem): boolean {
    return !!this.bootstrap()?.[item.flag];
  }

  // Matches EmailsWidget/Default.js's established pattern for jumping directly to
  // a specific ConfigurationManagement section from elsewhere in the app - set the
  // same "active menu" key Index.js reads on load, then navigate there.
  navigateToMvcSection(elementId: string): void {
    localStorage.setItem(ACTIVE_MENU_STORAGE_KEY, elementId);
    window.location.href = '/ConfigurationManagement';
  }
}
